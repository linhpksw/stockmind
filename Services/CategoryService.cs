using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using stockmind.DTOs.Categories;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services;

public class CategoryService
{
    private readonly CategoryRepository _categoryRepository;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(CategoryRepository categoryRepository, ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<CategoryNodeDto>> ListHierarchyAsync(CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.ListAllAsync(includeDeleted: false, cancellationToken);
        var nodeLookup = categories.ToDictionary(
            c => c.CategoryId,
            c => new CategoryNodeDto
            {
                CategoryId = c.CategoryId,
                Code = c.Code,
                Name = c.Name,
                ParentCategoryId = c.ParentCategoryId,
            });

        var roots = new List<CategoryNodeDto>();
        foreach (var node in nodeLookup.Values)
        {
            if (node.ParentCategoryId.HasValue && nodeLookup.TryGetValue(node.ParentCategoryId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        SortNodesByName(roots);
        return roots;
    }

    private static void SortNodesByName(List<CategoryNodeDto> nodes)
    {
        nodes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        foreach (var node in nodes)
        {
            if (node.Children.Count > 0)
            {
                SortNodesByName(node.Children);
            }
        }
    }

    public async Task<ImportCategoriesResponseDto> ImportCategoriesAsync(
        ImportCategoriesRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var response = new ImportCategoriesResponseDto();
        if (request.Rows == null || request.Rows.Count == 0)
        {
            return response;
        }

        var existingCategories = await _categoryRepository.ListAllTrackedAsync(includeDeleted: true, cancellationToken);
        var categoriesByCode = existingCategories
            .GroupBy(c => c.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var categoriesById = existingCategories.ToDictionary(c => c.CategoryId);

        var now = DateTime.UtcNow;
        var newCategoriesAutoIds = new List<Category>();
        var newCategoriesExplicitIds = new List<Category>();

        var pendingRows = request.Rows.Where(r => r != null).ToList();
        var nullRows = request.Rows.Count - pendingRows.Count;
        if (nullRows > 0)
        {
            response.SkippedInvalid += nullRows;
        }

        while (pendingRows.Count > 0)
        {
            var remainingRows = new List<ImportCategoryRowDto>();
            var processedInPass = 0;

            foreach (var row in pendingRows)
            {
                var result = TryProcessImportRow(
                    row,
                    now,
                    categoriesByCode,
                    categoriesById,
                    newCategoriesAutoIds,
                    newCategoriesExplicitIds,
                    response);

                switch (result)
                {
                    case ImportRowProcessResult.Processed:
                        processedInPass += 1;
                        break;
                    case ImportRowProcessResult.MissingParent:
                        remainingRows.Add(row);
                        break;
                    case ImportRowProcessResult.Invalid:
                        break;
                }
            }

            if (processedInPass == 0)
            {
                response.SkippedMissingParent += remainingRows.Count;
                break;
            }

            pendingRows = remainingRows;
        }

        if (newCategoriesAutoIds.Count > 0)
        {
            await _categoryRepository.AddRangeAsync(newCategoriesAutoIds, cancellationToken);
        }

        await _categoryRepository.SaveChangesAsync(cancellationToken);

        if (newCategoriesExplicitIds.Count > 0)
        {
            await _categoryRepository.AddRangeWithExplicitIdsAsync(newCategoriesExplicitIds, cancellationToken);
        }

        _logger.LogInformation(
            "Category import completed: {Created} created, {Updated} updated, {Skipped} skipped.",
            response.Created,
            response.Updated,
            response.SkippedInvalid + response.SkippedMissingParent);
        return response;
    }

    private ImportRowProcessResult TryProcessImportRow(
        ImportCategoryRowDto row,
        DateTime timestamp,
        IDictionary<string, Category> categoriesByCode,
        IDictionary<long, Category> categoriesById,
        IList<Category> newCategoriesAutoIds,
        IList<Category> newCategoriesExplicitIds,
        ImportCategoriesResponseDto response)
    {
        var normalizedCode = row.Code?.Trim();
        var normalizedName = row.Name?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedCode) || string.IsNullOrWhiteSpace(normalizedName))
        {
            response.SkippedInvalid += 1;
            return ImportRowProcessResult.Invalid;
        }

        var parentResolution = ResolveParent(row, categoriesByCode, categoriesById);
        if (parentResolution.Missing)
        {
            return ImportRowProcessResult.MissingParent;
        }

        var requestedCategoryId = row.CategoryId.HasValue && row.CategoryId.Value > 0
            ? row.CategoryId.Value
            : (long?)null;

        var existing = FindExistingCategory(normalizedCode, requestedCategoryId, categoriesByCode, categoriesById);
        if (existing != null)
        {
            var desiredParentId = parentResolution.Parent != null && parentResolution.Parent.CategoryId > 0
                ? parentResolution.Parent.CategoryId
                : (long?)null;

            var changed = false;

            if (!string.Equals(existing.Name, normalizedName, StringComparison.Ordinal))
            {
                existing.Name = normalizedName;
                changed = true;
            }

            if (existing.ParentCategoryId != desiredParentId)
            {
                existing.ParentCategoryId = desiredParentId;
                if (parentResolution.Parent != null && parentResolution.Parent.CategoryId == 0)
                {
                    existing.ParentCategory = parentResolution.Parent;
                }
                changed = true;
            }

            if (existing.Deleted)
            {
                existing.Deleted = false;
                changed = true;
            }

            if (changed)
            {
                existing.LastModifiedAt = timestamp;
                response.Updated += 1;
            }

            return ImportRowProcessResult.Processed;
        }

        var category = new Category
        {
            Code = normalizedCode,
            Name = normalizedName,
            CreatedAt = timestamp,
            LastModifiedAt = timestamp,
            Deleted = false,
            ParentCategoryId = parentResolution.Parent != null && parentResolution.Parent.CategoryId > 0
                ? parentResolution.Parent.CategoryId
                : null,
        };

        if (parentResolution.Parent != null && parentResolution.Parent.CategoryId == 0)
        {
            category.ParentCategory = parentResolution.Parent;
        }

        if (requestedCategoryId.HasValue)
        {
            category.CategoryId = requestedCategoryId.Value;
            newCategoriesExplicitIds.Add(category);
            categoriesById[requestedCategoryId.Value] = category;
        }
        else
        {
            newCategoriesAutoIds.Add(category);
        }

        categoriesByCode[normalizedCode] = category;
        response.Created += 1;
        return ImportRowProcessResult.Processed;
    }

    private static Category? FindExistingCategory(
        string normalizedCode,
        long? requestedCategoryId,
        IDictionary<string, Category> categoriesByCode,
        IDictionary<long, Category> categoriesById)
    {
        if (requestedCategoryId.HasValue &&
            categoriesById.TryGetValue(requestedCategoryId.Value, out var existingById))
        {
            return existingById;
        }

        if (categoriesByCode.TryGetValue(normalizedCode, out var existingByCode))
        {
            return existingByCode;
        }

        return null;
    }

    private enum ImportRowProcessResult
    {
        Processed,
        MissingParent,
        Invalid
    }

    private static ParentResolutionResult ResolveParent(
        ImportCategoryRowDto row,
        IDictionary<string, Category> categoriesByCode,
        IDictionary<long, Category> categoriesById)
    {
        if (row.ParentCategoryId.HasValue &&
            categoriesById.TryGetValue(row.ParentCategoryId.Value, out var parentById))
        {
            return ParentResolutionResult.Success(parentById);
        }

        if (!string.IsNullOrWhiteSpace(row.ParentCode))
        {
            var parentCode = row.ParentCode.Trim();
            if (categoriesByCode.TryGetValue(parentCode, out var parentByCode))
            {
                return ParentResolutionResult.Success(parentByCode);
            }

            if (long.TryParse(parentCode, out var numericParent) &&
                categoriesById.TryGetValue(numericParent, out var parentByNumericCode))
            {
                return ParentResolutionResult.Success(parentByNumericCode);
            }

            return ParentResolutionResult.MissingParent;
        }

        if (row.ParentCategoryId.HasValue)
        {
            return ParentResolutionResult.MissingParent;
        }

        return ParentResolutionResult.Success(null);
    }

    private sealed class ParentResolutionResult
    {
        private ParentResolutionResult(Category? parent, bool missing)
        {
            Parent = parent;
            Missing = missing;
        }

        public Category? Parent { get; }
        public bool Missing { get; }

        public static ParentResolutionResult Success(Category? parent) =>
            new(parent, false);

        public static ParentResolutionResult MissingParent => new(null, true);
    }
}
