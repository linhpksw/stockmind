using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace stockmind.Models;

public partial class StockMindDbContext : DbContext
{
    public StockMindDbContext(DbContextOptions<StockMindDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Grn> Grns { get; set; }

    public virtual DbSet<Grnitem> Grnitems { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Lot> Lots { get; set; }

    public virtual DbSet<MarkdownRule> MarkdownRules { get; set; }

    public virtual DbSet<Po> Pos { get; set; }

    public virtual DbSet<Poitem> Poitems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ReplenishmentSuggestion> ReplenishmentSuggestions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalesOrder> SalesOrders { get; set; }

    public virtual DbSet<SalesOrderItem> SalesOrderItems { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<UserAccount> UserAccounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__D54EE9B407F04540");

            entity.ToTable("Category");

            entity.HasIndex(e => e.Code, "UQ__Category__357D4CF9F2D1A4D6").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.ParentCategoryId).HasColumnName("parent_category_id");

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .HasForeignKey(d => d.ParentCategoryId)
                .HasConstraintName("FK__Category__parent__46E78A0C");
        });

        modelBuilder.Entity<Grn>(entity =>
        {
            entity.HasKey(e => e.GrnId).HasName("PK__GRN__39D8A22A649C3847");

            entity.ToTable("GRN");

            entity.Property(e => e.GrnId).HasColumnName("grn_id");
            entity.Property(e => e.PoId).HasColumnName("po_id");
            entity.Property(e => e.ReceivedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("receivedAt");
            entity.Property(e => e.ReceiverId).HasColumnName("receiverId");

            entity.HasOne(d => d.Po).WithMany(p => p.Grns)
                .HasForeignKey(d => d.PoId)
                .HasConstraintName("FK_GRN_PO");

            entity.HasOne(d => d.Receiver).WithMany(p => p.Grns)
                .HasForeignKey(d => d.ReceiverId)
                .HasConstraintName("FK_GRN_Receiver");
        });

        modelBuilder.Entity<Grnitem>(entity =>
        {
            entity.HasKey(e => e.GrnItemId).HasName("PK__GRNItem__92DEE4E65C3A9D32");

            entity.ToTable("GRNItem", tb => tb.HasTrigger("TR_GRNItem_RequireExpiry_ForPerishable"));

            entity.Property(e => e.GrnItemId).HasColumnName("grn_item_id");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiryDate");
            entity.Property(e => e.GrnId).HasColumnName("grn_id");
            entity.Property(e => e.LotCode)
                .HasMaxLength(64)
                .HasColumnName("lotCode");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.QtyReceived)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("qtyReceived");
            entity.Property(e => e.UnitCost)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("unitCost");

            entity.HasOne(d => d.Grn).WithMany(p => p.Grnitems)
                .HasForeignKey(d => d.GrnId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GRNItem_GRN");

            entity.HasOne(d => d.Lot).WithMany(p => p.Grnitems)
                .HasForeignKey(d => d.LotId)
                .HasConstraintName("FK_GRNItem_Lot");

            entity.HasOne(d => d.Product).WithMany(p => p.Grnitems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GRNItem_Product");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("PK__Inventor__B59ACC496D15BAB8");

            entity.ToTable("Inventory");

            entity.HasIndex(e => e.ProductId, "IX_Inv_Product");

            entity.Property(e => e.InventoryId).HasColumnName("inventory_id");
            entity.Property(e => e.OnHand)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("onHand");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("updatedAt");

            entity.HasOne(d => d.Product).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inv_Product");
        });

        modelBuilder.Entity<Lot>(entity =>
        {
            entity.HasKey(e => e.LotId).HasName("PK__Lot__38CAA92BF05D22E9");

            entity.ToTable("Lot");

            entity.HasIndex(e => new { e.ProductId, e.ExpiryDate }, "IX_Lot_Product_Expiry");

            entity.HasIndex(e => new { e.ProductId, e.ReceivedAt }, "IX_Lot_Product_Received");

            entity.HasIndex(e => new { e.ProductId, e.LotCode }, "UQ_Lot_Product_LotCode").IsUnique();

            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiryDate");
            entity.Property(e => e.LotCode)
                .HasMaxLength(64)
                .HasColumnName("lotCode");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.QtyOnHand)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("qtyOnHand");
            entity.Property(e => e.ReceivedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("receivedAt");

            entity.HasOne(d => d.Product).WithMany(p => p.Lots)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lot_Product");
        });

        modelBuilder.Entity<MarkdownRule>(entity =>
        {
            entity.HasKey(e => e.MarkdownRuleId).HasName("PK__Markdown__6A111CE19EB702DE");

            entity.ToTable("MarkdownRule");

            entity.Property(e => e.MarkdownRuleId).HasColumnName("markdown_rule_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.DaysToExpiry).HasColumnName("daysToExpiry");
            entity.Property(e => e.DiscountPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("discountPercent");
            entity.Property(e => e.FloorPercentOfCost)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("floorPercentOfCost");

            entity.HasOne(d => d.Category).WithMany(p => p.MarkdownRules)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_MR_Category");
        });

        modelBuilder.Entity<Po>(entity =>
        {
            entity.HasKey(e => e.PoId).HasName("PK__PO__368DA7F0B4D27146");

            entity.ToTable("PO");

            entity.Property(e => e.PoId).HasColumnName("po_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("createdAt");
            entity.Property(e => e.Status)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasDefaultValue("OPEN")
                .HasColumnName("status");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Pos)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PO_Supplier");
        });

        modelBuilder.Entity<Poitem>(entity =>
        {
            entity.HasKey(e => e.PoItemId).HasName("PK__POItem__E2A58305F8A2D573");

            entity.ToTable("POItem");

            entity.HasIndex(e => e.PoId, "IX_POItem_PO");

            entity.Property(e => e.PoItemId).HasColumnName("po_item_id");
            entity.Property(e => e.ExpectedDate).HasColumnName("expectedDate");
            entity.Property(e => e.PoId).HasColumnName("po_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.QtyOrdered)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("qtyOrdered");
            entity.Property(e => e.UnitCost)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("unitCost");

            entity.HasOne(d => d.Po).WithMany(p => p.Poitems)
                .HasForeignKey(d => d.PoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_POItem_PO");

            entity.HasOne(d => d.Product).WithMany(p => p.Poitems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_POItem_Product");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Product__47027DF5B5B1AC89");

            entity.ToTable("Product");

            entity.HasIndex(e => e.SkuCode, "UQ__Product__CE589F319A2937CA").IsUnique();

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("createdAt");
            entity.Property(e => e.IsPerishable).HasColumnName("isPerishable");
            entity.Property(e => e.LeadTimeDays).HasColumnName("leadTimeDays");
            entity.Property(e => e.MinStock).HasColumnName("minStock");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("price");
            entity.Property(e => e.ShelfLifeDays).HasColumnName("shelfLifeDays");
            entity.Property(e => e.SkuCode)
                .HasMaxLength(64)
                .HasColumnName("skuCode");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.Uom)
                .HasMaxLength(16)
                .HasDefaultValue("unit")
                .HasColumnName("uom");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("updatedAt");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Product_Category");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Products)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_Product_Supplier");
        });

        modelBuilder.Entity<ReplenishmentSuggestion>(entity =>
        {
            entity.HasKey(e => e.ReplId).HasName("PK__Replenis__0FA1E662D3EBDD2B");

            entity.ToTable("ReplenishmentSuggestion");

            entity.Property(e => e.ReplId).HasColumnName("repl_id");
            entity.Property(e => e.AvgDaily)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("avgDaily");
            entity.Property(e => e.ComputedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("computedAt");
            entity.Property(e => e.LeadTimeDays).HasColumnName("leadTimeDays");
            entity.Property(e => e.OnHand)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("onHand");
            entity.Property(e => e.OnOrder)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("onOrder");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Rop)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("rop");
            entity.Property(e => e.SafetyStock)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("safetyStock");
            entity.Property(e => e.SigmaDaily)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("sigmaDaily");
            entity.Property(e => e.SuggestedQty)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("suggestedQty");

            entity.HasOne(d => d.Product).WithMany(p => p.ReplenishmentSuggestions)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Repl_Product");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__760965CC8CD03932");

            entity.ToTable("Role");

            entity.HasIndex(e => e.Code, "UQ__Role__357D4CF9F4454F64").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Code)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__SalesOrd__465962296CD68BFA");

            entity.ToTable("SalesOrder");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("createdAt");
        });

        modelBuilder.Entity<SalesOrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__SalesOrd__3764B6BCBC2E6D4A");

            entity.ToTable("SalesOrderItem");

            entity.HasIndex(e => e.OrderId, "IX_SalesOrderItem_Order");

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.AppliedMarkdownPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("appliedMarkdownPercent");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Qty)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("qty");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("unitPrice");

            entity.HasOne(d => d.Order).WithMany(p => p.SalesOrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SOI_Order");

            entity.HasOne(d => d.Product).WithMany(p => p.SalesOrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SOI_Product");
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.MovementId).HasName("PK__StockMov__AB1D102216E002CE");

            entity.ToTable("StockMovement", tb => tb.HasTrigger("TR_StockMovement_NoUpdateDelete"));

            entity.HasIndex(e => new { e.ProductId, e.CreatedAt }, "IX_SM_Product_Created").IsDescending(false, true);

            entity.HasIndex(e => new { e.RefType, e.RefId }, "IX_SM_Ref");

            entity.Property(e => e.MovementId).HasColumnName("movement_id");
            entity.Property(e => e.ActorId).HasColumnName("actorId");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("createdAt");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Qty)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("qty");
            entity.Property(e => e.Reason)
                .HasMaxLength(200)
                .HasColumnName("reason");
            entity.Property(e => e.RefId).HasColumnName("refId");
            entity.Property(e => e.RefType)
                .HasMaxLength(24)
                .IsUnicode(false)
                .HasColumnName("refType");
            entity.Property(e => e.Type)
                .HasMaxLength(24)
                .IsUnicode(false)
                .HasColumnName("type");

            entity.HasOne(d => d.Actor).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.ActorId)
                .HasConstraintName("FK_SM_Actor");

            entity.HasOne(d => d.Lot).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.LotId)
                .HasConstraintName("FK_SM_Lot");

            entity.HasOne(d => d.Product).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SM_Product");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__6EE594E86AD995FB");

            entity.ToTable("Supplier");

            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.Contact)
                .HasMaxLength(100)
                .HasColumnName("contact");
            entity.Property(e => e.LeadTimeDays).HasColumnName("leadTimeDays");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__UserAcco__B9BE370F6315347F");

            entity.ToTable("UserAccount");

            entity.HasIndex(e => e.PhoneNumber, "UQ__UserAcco__A1936A6B25F9E02E").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__UserAcco__AB6E616424A1BB8D").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__UserAcco__F3DBC572618B6E45").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(10)
                .HasColumnName("phone_number");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRole__role_i__4316F928"),
                    l => l.HasOne<UserAccount>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRole__user_i__4222D4EF"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK__UserRole__6EDEA153A91D1CCD");
                        j.ToTable("UserRole");
                        j.IndexerProperty<long>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<long>("RoleId").HasColumnName("role_id");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
