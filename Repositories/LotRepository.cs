using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories
{
    public class LotRepository
    {
        private readonly StockMindDbContext _context;

        public LotRepository(StockMindDbContext context)
        {
            _context = context;
        }

        public async Task<Lot?> FindByProductIdAndLotCodeAsync(long productId, string lotCode, CancellationToken cancellationToken)
        {
            return await _context.Lots
                .FirstOrDefaultAsync(
                    l => l.ProductId == productId &&
                         l.LotCode == lotCode &&
                         !l.Deleted,
                    cancellationToken
                );
        }

        public async Task AddAsync(Lot lot, CancellationToken cancellationToken)
        {
            await _context.Lots.AddAsync(lot, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Lot lot, CancellationToken cancellationToken)
        {
            _context.Lots.Update(lot);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
