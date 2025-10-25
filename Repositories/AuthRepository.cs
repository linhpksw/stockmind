using System.Threading;
using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories;

public class AuthRepository
{
    private readonly StockMindDbContext _dbContext;

    public AuthRepository(StockMindDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserAccount?> GetActiveUserWithRolesAsync(string username, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserAccounts
            .Include(user => user.Roles)
            .AsNoTracking()
            .Where(user => user.IsActive && user.Username == username)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
