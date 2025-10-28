using System;
using AspectCore.DynamicProxy;
using stockmind.Models;

namespace stockmind.Commons.Attributes {
    [AttributeUsage(AttributeTargets.Method)]
    public class TransactionalAttribute : AbstractInterceptorAttribute {
        public override async Task Invoke(AspectContext context, AspectDelegate next) {
            var dbContext = context.ServiceProvider.GetService<StockMindDbContext>();
            if (dbContext == null) {
                await next(context);
                return;
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try {
                await next(context);
                await transaction.CommitAsync();
            } catch {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
