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

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Grn> Grns { get; set; }

    public virtual DbSet<Grnitem> Grnitems { get; set; }

    public virtual DbSet<Lot> Lots { get; set; }

    public virtual DbSet<LotSaleDecision> LotSaleDecisions { get; set; }

    public virtual DbSet<MarginProfile> MarginProfiles { get; set; }

    public virtual DbSet<MarkdownRule> MarkdownRules { get; set; }

    public virtual DbSet<Po> Pos { get; set; }

    public virtual DbSet<Poitem> Poitems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ReplenishmentSuggestion> ReplenishmentSuggestions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalesOrder> SalesOrders { get; set; }

    public virtual DbSet<SalesOrderItem> SalesOrderItems { get; set; }

    public virtual DbSet<SalesOrderPending> SalesOrderPendings { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<UserAccount> UserAccounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__D54EE9B4D1AF5F69");

            entity.ToTable("Category");

            entity.HasIndex(e => e.Code, "UQ__Category__357D4CF9E3D7D9F1").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.ParentCategoryId).HasColumnName("parent_category_id");

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .HasForeignKey(d => d.ParentCategoryId)
                .HasConstraintName("FK__Category__parent__5629CD9C");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__CD65CB8527485CF1");

            entity.ToTable("Customer");

            entity.HasIndex(e => e.LoyaltyCode, "UQ__Customer__675C0E5ADFCBA531").IsUnique();

            entity.HasIndex(e => e.PhoneNumber, "UX_Customer_Phone")
                .IsUnique()
                .HasFilter("([deleted]=(0))");

            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.LoyaltyCode)
                .HasMaxLength(64)
                .HasColumnName("loyalty_code");
            entity.Property(e => e.LoyaltyPoints).HasColumnName("loyalty_points");
            entity.Property(e => e.Notes)
                .HasMaxLength(500)
                .HasColumnName("notes");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .HasColumnName("phone_number");
        });

        modelBuilder.Entity<Grn>(entity =>
        {
            entity.HasKey(e => e.GrnId).HasName("PK__GRN__39D8A22A9485F401");

            entity.ToTable("GRN");

            entity.Property(e => e.GrnId).HasColumnName("grn_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.PoId).HasColumnName("po_id");
            entity.Property(e => e.ReceivedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("received_at");
            entity.Property(e => e.ReceiverId).HasColumnName("receiver_id");

            entity.HasOne(d => d.Po).WithMany(p => p.Grns)
                .HasForeignKey(d => d.PoId)
                .HasConstraintName("FK_GRN_PO");

            entity.HasOne(d => d.Receiver).WithMany(p => p.Grns)
                .HasForeignKey(d => d.ReceiverId)
                .HasConstraintName("FK_GRN_Receiver");
        });

        modelBuilder.Entity<Grnitem>(entity =>
        {
            entity.HasKey(e => e.GrnItemId).HasName("PK__GRNItem__92DEE4E6293F9332");

            entity.ToTable("GRNItem", tb => tb.HasTrigger("TR_GRNItem_RequireExpiry_ForPerishable"));

            entity.Property(e => e.GrnItemId).HasColumnName("grn_item_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.GrnId).HasColumnName("grn_id");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.LotCode)
                .HasMaxLength(64)
                .HasColumnName("lot_code");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.QtyReceived)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("qty_received");
            entity.Property(e => e.UnitCost)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("unit_cost");

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

        modelBuilder.Entity<Lot>(entity =>
        {
            entity.HasKey(e => e.LotId).HasName("PK__Lot__38CAA92B975BDCC2");

            entity.ToTable("Lot");

            entity.HasIndex(e => new { e.ProductId, e.ExpiryDate }, "IX_Lot_Product_Expiry");

            entity.HasIndex(e => new { e.ProductId, e.ReceivedAt }, "IX_Lot_Product_Received");

            entity.HasIndex(e => new { e.ProductId, e.LotCode }, "UQ_Lot_Product_LotCode").IsUnique();

            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.LotCode)
                .HasMaxLength(64)
                .HasColumnName("lot_code");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.QtyOnHand)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("qty_on_hand");
            entity.Property(e => e.ReceivedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("received_at");

            entity.HasOne(d => d.Product).WithMany(p => p.Lots)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lot_Product");
        });

        modelBuilder.Entity<LotSaleDecision>(entity =>
        {
            entity.HasKey(e => e.LotSaleDecisionId).HasName("PK__LotSaleD__6CC211DB268B111C");

            entity.ToTable("LotSaleDecision");

            entity.Property(e => e.LotSaleDecisionId).HasColumnName("lot_sale_decision_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.DiscountPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("discount_percent");
            entity.Property(e => e.IsApplied).HasColumnName("is_applied");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.LotId).HasColumnName("lot_id");

            entity.HasOne(d => d.Lot).WithMany(p => p.LotSaleDecisions)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LSD_Lot");
        });

        modelBuilder.Entity<MarginProfile>(entity =>
        {
            entity.HasKey(e => e.MarginProfileId).HasName("PK__MarginPr__7F927B691867AC60");

            entity.ToTable("MarginProfile");

            entity.HasIndex(e => e.ParentCategoryId, "UX_MarginProfile_Category")
                .IsUnique()
                .HasFilter("([deleted]=(0))");

            entity.Property(e => e.MarginProfileId).HasColumnName("margin_profile_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.MaxMarginPct)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("max_margin_pct");
            entity.Property(e => e.MinMarginPct)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("min_margin_pct");
            entity.Property(e => e.Notes)
                .HasMaxLength(500)
                .HasColumnName("notes");
            entity.Property(e => e.ParentCategoryId).HasColumnName("parent_category_id");
            entity.Property(e => e.ParentCategoryName)
                .HasMaxLength(200)
                .HasColumnName("parent_category_name");
            entity.Property(e => e.PriceSensitivity)
                .HasMaxLength(150)
                .HasColumnName("price_sensitivity");
            entity.Property(e => e.Profile)
                .HasMaxLength(100)
                .HasColumnName("profile");
            entity.Property(e => e.TargetMarginPct)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("target_margin_pct");

            entity.HasOne(d => d.ParentCategory).WithOne(p => p.MarginProfile)
                .HasForeignKey<MarginProfile>(d => d.ParentCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MarginProfile_Category");
        });

        modelBuilder.Entity<MarkdownRule>(entity =>
        {
            entity.HasKey(e => e.MarkdownRuleId).HasName("PK__Markdown__6A111CE14A713308");

            entity.ToTable("MarkdownRule");

            entity.Property(e => e.MarkdownRuleId).HasColumnName("markdown_rule_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.DaysToExpiry).HasColumnName("days_to_expiry");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.DiscountPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("discount_percent");
            entity.Property(e => e.FloorPercentOfCost)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("floor_percent_of_cost");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");

            entity.HasOne(d => d.Category).WithMany(p => p.MarkdownRules)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_MR_Category");
        });

        modelBuilder.Entity<Po>(entity =>
        {
            entity.HasKey(e => e.PoId).HasName("PK__PO__368DA7F04CE3561E");

            entity.ToTable("PO");

            entity.Property(e => e.PoId).HasColumnName("po_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
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
            entity.HasKey(e => e.PoItemId).HasName("PK__POItem__E2A583053A799E9F");

            entity.ToTable("POItem");

            entity.HasIndex(e => e.PoId, "IX_POItem_PO");

            entity.Property(e => e.PoItemId).HasColumnName("po_item_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.ExpectedDate).HasColumnName("expected_date");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.PoId).HasColumnName("po_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.QtyOrdered)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("qty_ordered");
            entity.Property(e => e.UnitCost)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("unit_cost");

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
            entity.HasKey(e => e.ProductId).HasName("PK__Product__47027DF5AA8D1090");

            entity.ToTable("Product");

            entity.HasIndex(e => e.SkuCode, "UQ__Product__843F428FD0913412").IsUnique();

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.IsPerishable).HasColumnName("is_perishable");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.MediaUrl)
                .HasMaxLength(1024)
                .HasColumnName("media_url");
            entity.Property(e => e.MinStock).HasColumnName("min_stock");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("price");
            entity.Property(e => e.ShelfLifeDays).HasColumnName("shelf_life_days");
            entity.Property(e => e.SkuCode)
                .HasMaxLength(64)
                .HasColumnName("sku_code");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.Uom)
                .HasMaxLength(16)
                .HasDefaultValue("unit")
                .HasColumnName("uom");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Product_Category");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Products)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_Product_Supplier");
        });

        modelBuilder.Entity<ReplenishmentSuggestion>(entity =>
        {
            entity.HasKey(e => e.ReplId).HasName("PK__Replenis__0FA1E662483F8ECE");

            entity.ToTable("ReplenishmentSuggestion");

            entity.Property(e => e.ReplId).HasColumnName("repl_id");
            entity.Property(e => e.AvgDaily)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("avg_daily");
            entity.Property(e => e.ComputedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("computed_at");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.LeadTimeDays).HasColumnName("lead_time_days");
            entity.Property(e => e.OnHand)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("on_hand");
            entity.Property(e => e.OnOrder)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("on_order");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Rop)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("rop");
            entity.Property(e => e.SafetyStock)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("safety_stock");
            entity.Property(e => e.SigmaDaily)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("sigma_daily");
            entity.Property(e => e.SuggestedQty)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("suggested_qty");

            entity.HasOne(d => d.Product).WithMany(p => p.ReplenishmentSuggestions)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Repl_Product");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__760965CCFACA904B");

            entity.ToTable("Role");

            entity.HasIndex(e => e.Code, "UQ__Role__357D4CF9CD30BFC9").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Code)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__SalesOrd__46596229AE99D0DA");

            entity.ToTable("SalesOrder");

            entity.HasIndex(e => e.OrderCode, "IX_SalesOrder_OrderCode").IsUnique();

            entity.HasIndex(e => e.OrderCode, "UQ_SalesOrder_Code").IsUnique();

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.CashierId).HasColumnName("cashier_id");
            entity.Property(e => e.CashierNotes)
                .HasMaxLength(500)
                .HasColumnName("cashier_notes");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.DiscountTotal)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("discount_total");
            entity.Property(e => e.ItemsCount).HasColumnName("items_count");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.LoyaltyAmountRedeemed)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("loyalty_amount_redeemed");
            entity.Property(e => e.LoyaltyPointsEarned).HasColumnName("loyalty_points_earned");
            entity.Property(e => e.LoyaltyPointsRedeemed).HasColumnName("loyalty_points_redeemed");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(32)
                .HasColumnName("order_code");
            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("subtotal");
            entity.Property(e => e.Total)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("total");

            entity.HasOne(d => d.Cashier).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.CashierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrder_Cashier");

            entity.HasOne(d => d.Customer).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_SalesOrder_Customer");
        });

        modelBuilder.Entity<SalesOrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__SalesOrd__3764B6BC364987AD");

            entity.ToTable("SalesOrderItem");

            entity.HasIndex(e => e.LotId, "IX_SalesOrderItem_Lot").HasFilter("([lot_id] IS NOT NULL)");

            entity.HasIndex(e => e.OrderId, "IX_SalesOrderItem_Order");

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.AppliedMarkdownPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("applied_markdown_percent");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.IsWeightBased).HasColumnName("is_weight_based");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.LineSubtotal)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("line_subtotal");
            entity.Property(e => e.LineTotal)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("line_total");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PriceOverrideReason)
                .HasMaxLength(200)
                .HasColumnName("price_override_reason");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Qty)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("qty");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Lot).WithMany(p => p.SalesOrderItems)
                .HasForeignKey(d => d.LotId)
                .HasConstraintName("FK_SOI_Lot");

            entity.HasOne(d => d.Order).WithMany(p => p.SalesOrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SOI_Order");

            entity.HasOne(d => d.Product).WithMany(p => p.SalesOrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SOI_Product");
        });

        modelBuilder.Entity<SalesOrderPending>(entity =>
        {
            entity.HasKey(e => e.PendingId).HasName("PK__SalesOrd__6FF04F4B6885B62F");

            entity.ToTable("SalesOrderPending");

            entity.HasIndex(e => e.ConfirmationToken, "UX_SalesOrderPending_Token").IsUnique();

            entity.Property(e => e.PendingId).HasColumnName("pending_id");
            entity.Property(e => e.CashierId).HasColumnName("cashier_id");
            entity.Property(e => e.ConfirmationToken).HasColumnName("confirmation_token");
            entity.Property(e => e.ConfirmedAt)
                .HasPrecision(0)
                .HasColumnName("confirmed_at");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.ExpiresAt)
                .HasPrecision(0)
                .HasColumnName("expires_at");
            entity.Property(e => e.PayloadJson).HasColumnName("payload_json");
            entity.Property(e => e.Status)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasDefaultValue("PENDING")
                .HasColumnName("status");

            entity.HasOne(d => d.Cashier).WithMany(p => p.SalesOrderPendings)
                .HasForeignKey(d => d.CashierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SOP_Cashier");

            entity.HasOne(d => d.Customer).WithMany(p => p.SalesOrderPendings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SOP_Customer");
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.MovementId).HasName("PK__StockMov__AB1D10225619A3CE");

            entity.ToTable("StockMovement", tb => tb.HasTrigger("TR_StockMovement_NoUpdateDelete"));

            entity.HasIndex(e => new { e.ProductId, e.CreatedAt }, "IX_SM_Product_Created").IsDescending(false, true);

            entity.HasIndex(e => new { e.RefType, e.RefId }, "IX_SM_Ref");

            entity.Property(e => e.MovementId).HasColumnName("movement_id");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Qty)
                .HasColumnType("decimal(19, 4)")
                .HasColumnName("qty");
            entity.Property(e => e.Reason)
                .HasMaxLength(200)
                .HasColumnName("reason");
            entity.Property(e => e.RefId).HasColumnName("ref_id");
            entity.Property(e => e.RefType)
                .HasMaxLength(24)
                .IsUnicode(false)
                .HasColumnName("ref_type");
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
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__6EE594E88AC5513F");

            entity.ToTable("Supplier");

            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.Contact)
                .HasMaxLength(100)
                .HasColumnName("contact");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.DeletedAt)
                .HasPrecision(0)
                .HasColumnName("deleted_at");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
            entity.Property(e => e.LeadTimeDays).HasColumnName("lead_time_days");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__UserAcco__B9BE370F6C52C546");

            entity.ToTable("UserAccount");

            entity.HasIndex(e => e.PhoneNumber, "UQ__UserAcco__A1936A6B91319C27").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__UserAcco__AB6E6164ACBC8905").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__UserAcco__F3DBC572C5CBC466").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastModifiedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("last_modified_at");
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
                        .HasConstraintName("FK__UserRole__role_i__47DBAE45"),
                    l => l.HasOne<UserAccount>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRole__user_i__46E78A0C"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK__UserRole__6EDEA153D7856177");
                        j.ToTable("UserRole");
                        j.IndexerProperty<long>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<long>("RoleId").HasColumnName("role_id");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
