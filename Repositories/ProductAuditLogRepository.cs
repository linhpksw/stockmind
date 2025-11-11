using System.Threading;
using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories
{
    public class ProductAuditLogRepository
    {
        private static bool _tableVerified;
        private static readonly SemaphoreSlim TableSemaphore = new(1, 1);

        private readonly StockMindDbContext _dbContext;

        public ProductAuditLogRepository(StockMindDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ProductAuditLog> AddAsync(ProductAuditLog entry, CancellationToken cancellationToken)
        {
            await EnsureAuditTableExistsAsync(cancellationToken);

            await _dbContext.ProductAuditLogs.AddAsync(entry, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return entry;
        }

        private async Task EnsureAuditTableExistsAsync(CancellationToken cancellationToken)
        {
            if (_tableVerified)
            {
                return;
            }

            await TableSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_tableVerified)
                {
                    return;
                }

                const string ddl = """
IF OBJECT_ID(N'[dbo].[ProductAuditLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductAuditLog]
    (
        [product_audit_log_id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [product_id] BIGINT NOT NULL,
        [action] NVARCHAR(64) NOT NULL,
        [actor] NVARCHAR(128) NOT NULL,
        [payload] NVARCHAR(MAX) NULL,
        [created_at] DATETIME2(0) NOT NULL CONSTRAINT DF_ProductAuditLog_CreatedAt DEFAULT (sysdatetime())
    );

    ALTER TABLE [dbo].[ProductAuditLog]
    ADD CONSTRAINT FK_ProductAuditLog_Product
        FOREIGN KEY ([product_id]) REFERENCES [dbo].[Product]([product_id]) ON DELETE CASCADE;
END
""";

                await _dbContext.Database.ExecuteSqlRawAsync(ddl, cancellationToken);
                _tableVerified = true;
            }
            finally
            {
                TableSemaphore.Release();
            }
        }
    }
}
