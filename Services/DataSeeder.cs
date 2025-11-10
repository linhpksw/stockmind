using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using stockmind.Models;
using BCryptNet = BCrypt.Net.BCrypt;

namespace stockmind.Services;

public class DataSeeder
{
    private static readonly IReadOnlyList<RoleSeed> RoleSeeds =
    [
        new RoleSeed("ADMIN", "Admin"),
        new RoleSeed("INVENTORY_MANAGER", "Inventory Manager"),
        new RoleSeed("BUYER", "Buyer"),
        new RoleSeed("STORE_STAFF", "Store Staff"),
        new RoleSeed("CASHIER", "Cashier (Mock)")
    ];

    private static readonly IReadOnlyList<UserSeed> UserSeeds =
    [
        new UserSeed(
            Username: "admin",
            FullName: "Administrator",
            Email: "admin@stockmind.local",
            PhoneNumber: "0900000000",
            Roles: new[] { "ADMIN" }),
        new UserSeed(
            Username: "inventory",
            FullName: "Inventory Manager",
            Email: "inventory.manager@stockmind.local",
            PhoneNumber: "0900000001",
            Roles: new[] { "INVENTORY_MANAGER" }),
        new UserSeed(
            Username: "buyer",
            FullName: "Buyer User",
            Email: "buyer@stockmind.local",
            PhoneNumber: "0900000002",
            Roles: new[] { "BUYER" }),
        new UserSeed(
            Username: "store",
            FullName: "Store Staff",
            Email: "store.staff@stockmind.local",
            PhoneNumber: "0900000003",
            Roles: new[] { "STORE_STAFF" }),
        new UserSeed(
            Username: "cashier",
            FullName: "Cashier",
            Email: "cashier@stockmind.local",
            PhoneNumber: "0900000004",
            Roles: new[] { "CASHIER" })
    ];

    private const string DefaultPassword = "123";

    private static readonly CategorySeed PerishableCategorySeed =
        new("PERISH", "Perishable Goods (Demo)");

    private static readonly ProductSeed MarkdownDemoProductSeed = new(
        SkuCode: "PROD-001",
        Name: "Demo Yogurt Cup",
        Uom: "PCS",
        Price: 4.50m,
        MinStock: 10,
        LeadTimeDays: 3,
        ShelfLifeDays: 7,
        InitialOnHand: 24m);

    private const string DemoLotCode = "LOT-A-DEMO";
    private const decimal DemoUnitCost = 2.10m;
    private const decimal DemoLotQuantity = 12m;
    private const int DemoLotDaysToExpiry = 2;

    private static readonly IReadOnlyList<MarkdownRuleStepSeed> MarkdownDemoRuleSeeds =
    [
        new MarkdownRuleStepSeed(DaysToExpiry: 2, DiscountPercent: 0.20m, FloorPercentOfCost: 0.30m),
        new MarkdownRuleStepSeed(DaysToExpiry: 1, DiscountPercent: 0.40m, FloorPercentOfCost: 0.30m),
        new MarkdownRuleStepSeed(DaysToExpiry: 0, DiscountPercent: 0.60m, FloorPercentOfCost: 0.30m)
    ];

    private readonly StockMindDbContext _dbContext;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(StockMindDbContext dbContext, ILogger<DataSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.Database.CanConnectAsync(cancellationToken))
        {
            _logger.LogWarning("Cannot connect to the database. Skipping data seeding.");
            return;
        }

        await SeedRolesAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
        await SeedMarkdownDemoDataAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await _dbContext.Roles
            .Select(role => role.Code)
            .ToListAsync(cancellationToken);
        var existingCodeSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        var missingRoles = RoleSeeds
            .Where(seed => !existingCodeSet.Contains(seed.Code))
            .Select(seed => new Role
            {
                Code = seed.Code,
                Name = seed.Name
            })
            .ToList();

        if (missingRoles.Count == 0)
        {
            return;
        }

        await _dbContext.Roles.AddRangeAsync(missingRoles, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var role in missingRoles)
        {
            _logger.LogInformation("Seeded role {RoleCode}", role.Code);
        }
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        var existingUsernames = await _dbContext.UserAccounts
            .Select(user => user.Username)
            .ToListAsync(cancellationToken);
        var existingUsernameSet = new HashSet<string>(existingUsernames, StringComparer.OrdinalIgnoreCase);

        var rolesByCode = await _dbContext.Roles
            .ToDictionaryAsync(role => role.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var passwordHash = BCryptNet.HashPassword(DefaultPassword);

        var newUsers = new List<UserAccount>();

        foreach (var seed in UserSeeds)
        {
            if (existingUsernameSet.Contains(seed.Username))
            {
                continue;
            }

            var user = new UserAccount
            {
                Username = seed.Username,
                FullName = seed.FullName,
                Email = seed.Email,
                PhoneNumber = seed.PhoneNumber,
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var roleCode in seed.Roles)
            {
                if (rolesByCode.TryGetValue(roleCode, out var role))
                {
                    user.Roles.Add(role);
                }
                else
                {
                    _logger.LogWarning("Role {RoleCode} not found while seeding user {Username}", roleCode, seed.Username);
                }
            }

            newUsers.Add(user);
        }

        if (newUsers.Count == 0)
        {
            return;
        }

        await _dbContext.UserAccounts.AddRangeAsync(newUsers, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var user in newUsers)
        {
            _logger.LogInformation("Seeded user {Username} with default password.", user.Username);
        }
    }

    private async Task SeedMarkdownDemoDataAsync(CancellationToken cancellationToken)
    {
        var category = await EnsureCategoryAsync(PerishableCategorySeed, cancellationToken);
        var product = await EnsureProductAsync(MarkdownDemoProductSeed, category.CategoryId, cancellationToken);
        await EnsureInventoryAsync(product.ProductId, MarkdownDemoProductSeed.InitialOnHand, cancellationToken);
        var lot = await EnsureLotAsync(product.ProductId, DemoLotCode, DemoLotQuantity, DemoLotDaysToExpiry, cancellationToken);
        await EnsureGrnHistoryAsync(product.ProductId, lot.LotId, DemoLotQuantity, DemoUnitCost, lot.ExpiryDate, cancellationToken);
        await EnsureMarkdownRulesAsync(category.CategoryId, MarkdownDemoRuleSeeds, cancellationToken);
        await EnsureDemoSalesOrderAsync(product, cancellationToken);
    }

    private async Task<Category> EnsureCategoryAsync(CategorySeed seed, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories.FirstOrDefaultAsync(
            c => c.Code == seed.Code,
            cancellationToken);

        if (category != null)
        {
            if (category.Deleted)
            {
                category.Deleted = false;
                category.LastModifiedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return category;
        }

        category = new Category
        {
            Code = seed.Code,
            Name = seed.Name,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            Deleted = false
        };

        await _dbContext.Categories.AddAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded category {CategoryCode}", seed.Code);

        return category;
    }

    private async Task<Product> EnsureProductAsync(ProductSeed seed, long categoryId, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(
            p => p.SkuCode == seed.SkuCode,
            cancellationToken);

        if (product == null)
        {
            product = new Product
            {
                SkuCode = seed.SkuCode,
                Name = seed.Name,
                Uom = seed.Uom,
                Price = seed.Price,
                MinStock = seed.MinStock,
                LeadTimeDays = seed.LeadTimeDays,
                ShelfLifeDays = seed.ShelfLifeDays,
                IsPerishable = true,
                CategoryId = categoryId,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                Deleted = false
            };

            await _dbContext.Products.AddAsync(product, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded product {SkuCode}", product.SkuCode);
        }
        else
        {
            product.CategoryId = categoryId;
            product.IsPerishable = true;
            product.Price = seed.Price;
            product.ShelfLifeDays = seed.ShelfLifeDays;
            product.LastModifiedAt = DateTime.UtcNow;
            product.Deleted = false;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return product;
    }

    private async Task EnsureInventoryAsync(long productId, decimal onHand, CancellationToken cancellationToken)
    {
        var inventory = await _dbContext.Inventories.FirstOrDefaultAsync(
            i => i.ProductId == productId,
            cancellationToken);

        if (inventory == null)
        {
            inventory = new Inventory
            {
                ProductId = productId,
                OnHand = onHand,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                Deleted = false
            };

            await _dbContext.Inventories.AddAsync(inventory, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        inventory.OnHand = onHand;
        inventory.Deleted = false;
        inventory.LastModifiedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Lot> EnsureLotAsync(long productId, string lotCode, decimal qtyOnHand, int daysToExpiry, CancellationToken cancellationToken)
    {
        var deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysToExpiry));
        var lot = await _dbContext.Lots.FirstOrDefaultAsync(
            l => l.ProductId == productId && l.LotCode == lotCode,
            cancellationToken);

        if (lot == null)
        {
            lot = new Lot
            {
                ProductId = productId,
                LotCode = lotCode,
                QtyOnHand = qtyOnHand,
                ReceivedAt = DateTime.UtcNow.AddDays(-3),
                ExpiryDate = deadline,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                Deleted = false
            };

            await _dbContext.Lots.AddAsync(lot, cancellationToken);
        }
        else
        {
            lot.QtyOnHand = qtyOnHand;
            lot.ExpiryDate = deadline;
            lot.Deleted = false;
            lot.LastModifiedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return lot;
    }

    private async Task EnsureGrnHistoryAsync(long productId, long lotId, decimal qtyReceived, decimal unitCost, DateOnly? expiry, CancellationToken cancellationToken)
    {
        var grnItem = await _dbContext.Grnitems.FirstOrDefaultAsync(
            item => item.LotId == lotId && item.ProductId == productId && !item.Deleted,
            cancellationToken);

        if (grnItem != null)
        {
            grnItem.UnitCost = unitCost;
            grnItem.QtyReceived = qtyReceived;
            grnItem.ExpiryDate = expiry;
            grnItem.LastModifiedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var grn = new Grn
        {
            ReceivedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            Deleted = false
        };

        await _dbContext.Grns.AddAsync(grn, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        grnItem = new Grnitem
        {
            GrnId = grn.GrnId,
            ProductId = productId,
            LotId = lotId,
            QtyReceived = qtyReceived,
            UnitCost = unitCost,
            LotCode = DemoLotCode,
            ExpiryDate = expiry,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            Deleted = false
        };

        await _dbContext.Grnitems.AddAsync(grnItem, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureMarkdownRulesAsync(long categoryId, IEnumerable<MarkdownRuleStepSeed> seeds, CancellationToken cancellationToken)
    {
        var existingRules = await _dbContext.MarkdownRules
            .Where(rule => rule.CategoryId == categoryId)
            .ToListAsync(cancellationToken);

        foreach (var seed in seeds)
        {
            var rule = existingRules.FirstOrDefault(r => r.DaysToExpiry == seed.DaysToExpiry);
            if (rule == null)
            {
                rule = new MarkdownRule
                {
                    CategoryId = categoryId,
                    DaysToExpiry = seed.DaysToExpiry,
                    DiscountPercent = seed.DiscountPercent,
                    FloorPercentOfCost = seed.FloorPercentOfCost,
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow,
                    Deleted = false
                };
                await _dbContext.MarkdownRules.AddAsync(rule, cancellationToken);
                continue;
            }

            rule.DiscountPercent = seed.DiscountPercent;
            rule.FloorPercentOfCost = seed.FloorPercentOfCost;
            rule.Deleted = false;
            rule.LastModifiedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDemoSalesOrderAsync(Product product, CancellationToken cancellationToken)
    {
        var order = await _dbContext.SalesOrders
            .Include(o => o.SalesOrderItems.Where(i => !i.Deleted))
            .FirstOrDefaultAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;

        if (order == null)
        {
            order = new SalesOrder
            {
                CreatedAt = utcNow,
                LastModifiedAt = utcNow,
                Deleted = false
            };

            order.SalesOrderItems.Add(new SalesOrderItem
            {
                ProductId = product.ProductId,
                Qty = 5,
                UnitPrice = product.Price,
                AppliedMarkdownPercent = null,
                CreatedAt = utcNow,
                LastModifiedAt = utcNow,
                Deleted = false
            });

            await _dbContext.SalesOrders.AddAsync(order, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded demo sales order for product {Product}", product.SkuCode);
            return;
        }

        if (order.SalesOrderItems.Any(item => item.ProductId == product.ProductId && !item.Deleted))
        {
            return;
        }

        order.SalesOrderItems.Add(new SalesOrderItem
        {
            ProductId = product.ProductId,
            Qty = 5,
            UnitPrice = product.Price,
            AppliedMarkdownPercent = null,
            CreatedAt = utcNow,
            LastModifiedAt = utcNow,
            Deleted = false
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Appended demo sales order item for product {Product}", product.SkuCode);
    }

    private sealed record RoleSeed(string Code, string Name);

    private sealed record UserSeed(string Username, string FullName, string? Email, string? PhoneNumber, IReadOnlyCollection<string> Roles);

    private sealed record CategorySeed(string Code, string Name);

    private sealed record ProductSeed(
        string SkuCode,
        string Name,
        string Uom,
        decimal Price,
        int MinStock,
        int LeadTimeDays,
        int? ShelfLifeDays,
        decimal InitialOnHand);

    private sealed record MarkdownRuleStepSeed(int DaysToExpiry, decimal DiscountPercent, decimal FloorPercentOfCost);
}
