using Microsoft.EntityFrameworkCore;

namespace stockmind.Models;

public partial class StockMindDbContext
{
    public virtual DbSet<ProductAuditLog> ProductAuditLogs { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        ConfigureProductAuditLogs(modelBuilder);
    }

    private static void ConfigureProductAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductAuditLog>(entity =>
        {
            entity.HasKey(e => e.ProductAuditLogId).HasName("PK_ProductAuditLog");

            entity.ToTable("ProductAuditLog");

            entity.Property(e => e.ProductAuditLogId).HasColumnName("product_audit_log_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Action)
                .HasMaxLength(64)
                .HasColumnName("action");
            entity.Property(e => e.Actor)
                .HasMaxLength(128)
                .HasColumnName("actor");
            entity.Property(e => e.Payload)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("payload");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Product)
                .WithMany(p => p.ProductAuditLogs)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProductAuditLog_Product");
        });
    }
}
