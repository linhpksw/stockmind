using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories;

public class MarginProfileRepository
{
    private readonly StockMindDbContext _dbContext;

    public MarginProfileRepository(StockMindDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<MarginProfile>> ListAsync(CancellationToken cancellationToken)
    {
        return _dbContext.MarginProfiles
            .Include(p => p.ParentCategory)
            .Where(p => !p.Deleted)
            .OrderBy(p => p.ParentCategoryName)
            .ToListAsync(cancellationToken);
    }

    public Task<MarginProfile?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.MarginProfiles
            .Include(p => p.ParentCategory)
            .FirstOrDefaultAsync(p => p.MarginProfileId == id && !p.Deleted, cancellationToken);
    }

    public Task<MarginProfile?> GetByParentCategoryIdAsync(long parentCategoryId, CancellationToken cancellationToken)
    {
        return _dbContext.MarginProfiles
            .FirstOrDefaultAsync(p => p.ParentCategoryId == parentCategoryId && !p.Deleted, cancellationToken);
    }

    public async Task AddAsync(MarginProfile profile, CancellationToken cancellationToken)
    {
        await _dbContext.MarginProfiles.AddAsync(profile, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task AddRangeAsync(IEnumerable<MarginProfile> profiles, CancellationToken cancellationToken)
    {
        return _dbContext.MarginProfiles.AddRangeAsync(profiles, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
