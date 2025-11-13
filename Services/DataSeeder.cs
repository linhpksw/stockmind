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

    private sealed record RoleSeed(string Code, string Name);

    private sealed record UserSeed(string Username, string FullName, string? Email, string? PhoneNumber, IReadOnlyCollection<string> Roles);
}
