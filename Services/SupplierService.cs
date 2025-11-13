using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Helpers;
using stockmind.Commons.Responses;
using stockmind.DTOs.Suppliers;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services;

public class SupplierService
{
    private const int MaxPageSize = 100;
    private static readonly string[] SortableFields = { "name", "createdAt", "lastModifiedAt", "leadTimeDays" };

    private readonly SupplierRepository _supplierRepository;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(SupplierRepository supplierRepository, ILogger<SupplierService> logger)
    {
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    #region Create

    public async Task<SupplierResponseDto> CreateSupplierAsync(CreateSupplierRequestDto request, CancellationToken cancellationToken)
    {
        ValidateCreate(request);

        var normalizedName = request.Name.Trim();

        if (await _supplierRepository.ExistsByNameAsync(normalizedName, null, cancellationToken))
        {
            throw new BizDataAlreadyExistsException(ErrorCode4xx.DataAlreadyExists, new[] { normalizedName });
        }

        var utcNow = DateTime.UtcNow;
        var supplier = new Supplier
        {
            Name = normalizedName,
            Contact = request.Contact?.Trim(),
            LeadTimeDays = request.LeadTimeDays,
            CreatedAt = utcNow,
            LastModifiedAt = utcNow,
            Deleted = false,
            DeletedAt = null
        };

        await _supplierRepository.AddAsync(supplier, cancellationToken);
        _logger.LogInformation("Supplier {SupplierName} created with id {SupplierId}", supplier.Name, supplier.SupplierId);

        return MapToResponse(supplier);
    }

    #endregion

    #region Get by id

    public async Task<SupplierResponseDto> GetSupplierByIdAsync(string publicId, bool includeDeleted, CancellationToken cancellationToken)
    {
        var supplierId = SupplierCodeHelper.FromPublicId(publicId);

        var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken)
                         ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });

        if (supplier.Deleted && !includeDeleted)
        {
            throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });
        }

        return MapToResponse(supplier);
    }

    #endregion

    #region List

    public async Task<PageResponseModel<SupplierResponseDto>> ListSuppliersAsync(ListSuppliersQueryDto query, CancellationToken cancellationToken)
    {
        var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, MaxPageSize);

        var suppliersQuery = BuildSuppliersQuery(query);

        var pageResult = await _supplierRepository.ListAsync(suppliersQuery, pageNum, pageSize, cancellationToken);

        var items = pageResult.Items.Select(MapToResponse).ToList();
        var totalCount = pageResult.Total > int.MaxValue ? int.MaxValue : (int)pageResult.Total;

        return new PageResponseModel<SupplierResponseDto>(pageSize, pageNum, totalCount, items);
    }

    private IQueryable<Supplier> BuildSuppliersQuery(ListSuppliersQueryDto query)
    {
        var suppliers = _supplierRepository
            .Query()
            .AsNoTracking();

        suppliers = ApplyDeletionFilter(suppliers, query.IncludeDeleted, query.DeletedOnly);
        suppliers = ApplySearchFilter(suppliers, query.Query);
        suppliers = ApplySorting(suppliers, query.Sort);

        return suppliers;
    }

    private static IQueryable<Supplier> ApplyDeletionFilter(IQueryable<Supplier> query, bool includeDeleted, bool deletedOnly)
    {
        if (deletedOnly)
        {
            return query.Where(s => s.Deleted);
        }

        if (!includeDeleted)
        {
            return query.Where(s => !s.Deleted);
        }

        return query;
    }

    private static IQueryable<Supplier> ApplySearchFilter(IQueryable<Supplier> query, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return query;
        }

        var term = searchText.Trim();
        return query.Where(s =>
            EF.Functions.Like(s.Name, $"%{term}%") ||
            (s.Contact != null && EF.Functions.Like(s.Contact, $"%{term}%")));
    }

    private static IQueryable<Supplier> ApplySorting(IQueryable<Supplier> query, string? sort)
    {
        var sortField = "createdat";
        var sortDirection = "desc";

        if (!string.IsNullOrWhiteSpace(sort))
        {
            var segments = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 2)
            {
                var candidateField = segments[0];
                var candidateDirection = segments[1];

                if (SortableFields.Contains(candidateField, StringComparer.OrdinalIgnoreCase) &&
                    (string.Equals(candidateDirection, "asc", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(candidateDirection, "desc", StringComparison.OrdinalIgnoreCase)))
                {
                    sortField = candidateField.ToLowerInvariant();
                    sortDirection = candidateDirection.ToLowerInvariant();
                }
            }
            else if (segments.Length == 1)
            {
                var candidateDirection = segments[0];
                if (string.Equals(candidateDirection, "asc", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(candidateDirection, "desc", StringComparison.OrdinalIgnoreCase))
                {
                    sortDirection = candidateDirection.ToLowerInvariant();
                }
            }
        }

        return (sortField, sortDirection) switch
        {
            ("name", "asc") => query.OrderBy(s => s.Name),
            ("name", _) => query.OrderByDescending(s => s.Name),
            ("leadtimedays", "asc") => query.OrderBy(s => s.LeadTimeDays),
            ("leadtimedays", _) => query.OrderByDescending(s => s.LeadTimeDays),
            ("lastmodifiedat", "asc") => query.OrderBy(s => s.LastModifiedAt),
            ("lastmodifiedat", _) => query.OrderByDescending(s => s.LastModifiedAt),
            ("createdat", "asc") => query.OrderBy(s => s.CreatedAt),
            _ => query.OrderByDescending(s => s.CreatedAt)
        };
    }

    #endregion

    #region Import

    public async Task<ImportSuppliersResponseDto> ImportSuppliersAsync(
        ImportSuppliersRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var response = new ImportSuppliersResponseDto();
        if (request.Rows == null || request.Rows.Count == 0)
        {
            return response;
        }

        var trackedSuppliers = await _supplierRepository.ListAllTrackedAsync(includeDeleted: true, cancellationToken);
        var suppliersById = trackedSuppliers.ToDictionary(s => s.SupplierId);
        var suppliersByName = trackedSuppliers
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .ToDictionary(s => s.Name.Trim(), s => s, StringComparer.OrdinalIgnoreCase);

        var utcNow = DateTime.UtcNow;
        var newSuppliers = new List<Supplier>();

        foreach (var row in request.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedName = row?.Name?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                response.SkippedInvalid += 1;
                continue;
            }

            Supplier? supplier = null;
            if (!string.IsNullOrWhiteSpace(row?.SupplierId))
            {
                try
                {
                    var internalId = SupplierCodeHelper.FromPublicId(row.SupplierId.Trim());
                    suppliersById.TryGetValue(internalId, out supplier);
                }
                catch
                {
                    // Ignore invalid public IDs and fall back to name matching.
                }
            }

            if (supplier is null && suppliersByName.TryGetValue(normalizedName, out var byName))
            {
                supplier = byName;
            }

            var contact = row?.Contact?.Trim();
            var leadTime = Math.Max(0, row?.LeadTimeDays ?? supplier?.LeadTimeDays ?? 0);

            if (supplier is null)
            {
                var newSupplier = new Supplier
                {
                    Name = normalizedName,
                    Contact = contact,
                    LeadTimeDays = leadTime,
                    CreatedAt = utcNow,
                    LastModifiedAt = utcNow,
                    Deleted = false,
                    DeletedAt = null
                };

                newSuppliers.Add(newSupplier);
                suppliersByName[normalizedName] = newSupplier;
                response.Created += 1;
                continue;
            }

            var changed = false;
            if (!string.Equals(supplier.Name, normalizedName, StringComparison.Ordinal))
            {
                supplier.Name = normalizedName;
                suppliersByName[normalizedName] = supplier;
                changed = true;
            }

            if (!string.Equals(supplier.Contact, contact, StringComparison.Ordinal))
            {
                supplier.Contact = contact;
                changed = true;
            }

            if (supplier.LeadTimeDays != leadTime)
            {
                supplier.LeadTimeDays = leadTime;
                changed = true;
            }

            if (supplier.Deleted)
            {
                supplier.Deleted = false;
                supplier.DeletedAt = null;
                changed = true;
            }

            if (changed)
            {
                supplier.LastModifiedAt = utcNow;
                response.Updated += 1;
            }
        }

        if (newSuppliers.Count > 0)
        {
            await _supplierRepository.AddRangeAsync(newSuppliers, cancellationToken);
        }

        await _supplierRepository.SaveChangesAsync(cancellationToken);

        response.Total = request.Rows.Count;

        _logger.LogInformation(
            "Supplier import completed: {Created} created, {Updated} updated, {Skipped} invalid.",
            response.Created,
            response.Updated,
            response.SkippedInvalid);

        return response;
    }

    #endregion

    #region Update

    public async Task<SupplierResponseDto> UpdateSupplierAsync(string publicId, UpdateSupplierRequestDto request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var supplierId = SupplierCodeHelper.FromPublicId(publicId);
        var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken)
                        ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });

        if (supplier.Deleted)
        {
            throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });
        }

        await ValidateUpdateAsync(request, supplier.SupplierId, cancellationToken);

        var updated = ApplyUpdates(supplier, request);
        await _supplierRepository.UpdateAsync(updated, cancellationToken);

        return MapToResponse(updated);
    }

    private async Task ValidateUpdateAsync(UpdateSupplierRequestDto request, long supplierId, CancellationToken cancellationToken)
    {
        if (request.LeadTimeDays.HasValue && request.LeadTimeDays.Value < 0)
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "leadTimeDays" });
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var normalizedName = request.Name.Trim();
            if (await _supplierRepository.ExistsByNameAsync(normalizedName, supplierId, cancellationToken))
            {
                throw new BizDataAlreadyExistsException(ErrorCode4xx.DataAlreadyExists, new[] { normalizedName });
            }
        }
    }

    private static Supplier ApplyUpdates(Supplier supplier, UpdateSupplierRequestDto request)
    {
        var utcNow = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            supplier.Name = request.Name.Trim();
        }

        if (request.Contact != null)
        {
            supplier.Contact = request.Contact.Trim();
        }

        if (request.LeadTimeDays.HasValue)
        {
            supplier.LeadTimeDays = request.LeadTimeDays.Value;
        }

        supplier.LastModifiedAt = utcNow;
        return supplier;
    }

    #endregion

    #region Soft delete

    public async Task<SupplierResponseDto> SoftDeleteSupplierAsync(string publicId, CancellationToken cancellationToken)
    {
        var supplierId = SupplierCodeHelper.FromPublicId(publicId);

        var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken)
                        ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });

        if (!supplier.Deleted)
        {
            var utcNow = DateTime.UtcNow;
            supplier.Deleted = true;
            supplier.DeletedAt = utcNow;
            supplier.LastModifiedAt = utcNow;

            await _supplierRepository.UpdateAsync(supplier, cancellationToken);
        }

        return MapToResponse(supplier);
    }

    #endregion

    #region Helpers

    private static void ValidateCreate(CreateSupplierRequestDto request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "name" });
        }

        if (request.LeadTimeDays < 0)
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "leadTimeDays" });
        }
    }

    private static SupplierResponseDto MapToResponse(Supplier supplier)
    {
        return new SupplierResponseDto
        {
            Id = SupplierCodeHelper.ToPublicId(supplier.SupplierId),
            Name = supplier.Name,
            Contact = supplier.Contact,
            LeadTimeDays = supplier.LeadTimeDays,
            CreatedAt = supplier.CreatedAt,
            LastModifiedAt = supplier.LastModifiedAt,
            Deleted = supplier.Deleted,
            DeletedAt = supplier.DeletedAt
        };
    }

    #endregion
}
