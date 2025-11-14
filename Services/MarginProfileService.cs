using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.DTOs.Margins;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services
{
    public class MarginProfileService
    {
        private readonly MarginProfileRepository _marginProfileRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly ILogger<MarginProfileService> _logger;

        public MarginProfileService(
            MarginProfileRepository marginProfileRepository,
            CategoryRepository categoryRepository,
            ILogger<MarginProfileService> logger)
        {
            _marginProfileRepository = marginProfileRepository;
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<MarginProfileDto>> ListAsync(CancellationToken cancellationToken)
        {
            var profiles = await _marginProfileRepository.ListAsync(cancellationToken);
            return profiles.Select(MapToDto).ToList();
        }

        public async Task<MarginProfileDto> UpdateAsync(
            long marginProfileId,
            UpdateMarginProfileRequestDto request,
            CancellationToken cancellationToken)
        {
            ValidateMarginInputs(request.MinMarginPct, request.TargetMarginPct, request.MaxMarginPct);

            var profile = await _marginProfileRepository.GetByIdAsync(marginProfileId, cancellationToken)
                          ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"marginProfileId={marginProfileId}" });

            profile.MinMarginPct = request.MinMarginPct;
            profile.TargetMarginPct = request.TargetMarginPct;
            profile.MaxMarginPct = request.MaxMarginPct;
            profile.LastModifiedAt = DateTime.UtcNow;

            await _marginProfileRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated margin profile {MarginProfileId}", profile.MarginProfileId);

            return MapToDto(profile);
        }

        public async Task<ImportMarginProfilesResponseDto> ImportAsync(
            ImportMarginProfilesRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var response = new ImportMarginProfilesResponseDto();
            if (request.Rows == null || request.Rows.Count == 0)
            {
                return response;
            }

            var categories = await _categoryRepository.ListAllTrackedAsync(includeDeleted: false, cancellationToken);
            var categoriesById = categories.ToDictionary(c => c.CategoryId);
            var categoriesByName = categories
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .ToDictionary(c => c.Name.Trim(), c => c, StringComparer.OrdinalIgnoreCase);

            var existingProfiles = await _marginProfileRepository.ListAsync(cancellationToken);
            var profilesByParentCategoryId = existingProfiles.ToDictionary(p => p.ParentCategoryId);

            var utcNow = DateTime.UtcNow;
            var newProfiles = new List<MarginProfile>();

            foreach (var row in request.Rows)
            {
                if (row is null ||
                    string.IsNullOrWhiteSpace(row.MarginProfile) ||
                    string.IsNullOrWhiteSpace(row.PriceSensitivity) ||
                    !row.MinMarginPct.HasValue ||
                    !row.TargetMarginPct.HasValue ||
                    !row.MaxMarginPct.HasValue)
                {
                    response.SkippedInvalid += 1;
                    continue;
                }

                var min = row.MinMarginPct.Value;
                var target = row.TargetMarginPct.Value;
                var max = row.MaxMarginPct.Value;

                if (!IsValidMarginSet(min, target, max))
                {
                    response.SkippedInvalid += 1;
                    continue;
                }

                Category? category = null;
                if (row.ParentCategoryId.HasValue &&
                    categoriesById.TryGetValue(row.ParentCategoryId.Value, out var categoryById))
                {
                    category = categoryById;
                }
                else if (!string.IsNullOrWhiteSpace(row.ParentCategoryName) &&
                         categoriesByName.TryGetValue(row.ParentCategoryName.Trim(), out var categoryByName))
                {
                    category = categoryByName;
                }

                if (category is null)
                {
                    response.SkippedMissingCategory += 1;
                    continue;
                }

                var normalizedProfile = row.MarginProfile.Trim();
                var normalizedSensitivity = row.PriceSensitivity.Trim();
                var notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim();

                if (profilesByParentCategoryId.TryGetValue(category.CategoryId, out var existing))
                {
                    existing.ParentCategoryName = category.Name;
                    existing.MarginProfileName = normalizedProfile;
                    existing.PriceSensitivity = normalizedSensitivity;
                    existing.MinMarginPct = min;
                    existing.TargetMarginPct = target;
                    existing.MaxMarginPct = max;
                    existing.Notes = notes;
                    existing.LastModifiedAt = utcNow;
                    response.Updated += 1;
                    continue;
                }

                var profile = new MarginProfile
                {
                    ParentCategoryId = category.CategoryId,
                    ParentCategoryName = category.Name,
                    MarginProfileName = normalizedProfile,
                    PriceSensitivity = normalizedSensitivity,
                    MinMarginPct = min,
                    TargetMarginPct = target,
                    MaxMarginPct = max,
                    Notes = notes,
                    CreatedAt = utcNow,
                    LastModifiedAt = utcNow,
                    Deleted = false
                };

                newProfiles.Add(profile);
                profilesByParentCategoryId[category.CategoryId] = profile;
                response.Created += 1;
            }

            if (newProfiles.Count > 0)
            {
                await _marginProfileRepository.AddRangeAsync(newProfiles, cancellationToken);
            }

            await _marginProfileRepository.SaveChangesAsync(cancellationToken);

            return response;
        }

        private static void ValidateMarginInputs(decimal min, decimal target, decimal max)
        {
            if (!IsValidMarginSet(min, target, max))
            {
                throw new BizException(
                    ErrorCode4xx.InvalidInput,
                    new[] { "Margin percentages must satisfy min <= target <= max and be non-negative." });
            }
        }

        private static bool IsValidMarginSet(decimal min, decimal target, decimal max)
        {
            if (min < 0 || target < 0 || max < 0)
            {
                return false;
            }

            return min <= target && target <= max;
        }

        private static MarginProfileDto MapToDto(MarginProfile profile)
        {
            return new MarginProfileDto
            {
                Id = profile.MarginProfileId,
                ParentCategoryId = profile.ParentCategoryId,
                ParentCategoryName = profile.ParentCategoryName,
                MarginProfile = profile.MarginProfileName,
                PriceSensitivity = profile.PriceSensitivity,
                MinMarginPct = profile.MinMarginPct,
                TargetMarginPct = profile.TargetMarginPct,
                MaxMarginPct = profile.MaxMarginPct,
                Notes = profile.Notes,
                CreatedAt = profile.CreatedAt,
                LastModifiedAt = profile.LastModifiedAt
            };
        }
    }
}
