/* ==========================================================
   DROP & RECREATE DATABASE (DESTRUCTIVE)
   ========================================================== */
USE [master];
GO

IF DB_ID(N'StockMindDB') IS NOT NULL
BEGIN
    PRINT 'Dropping existing database StockMindDB...';
    ALTER DATABASE [StockMindDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [StockMindDB];
END
GO

PRINT 'Creating database StockMindDB...';
CREATE DATABASE [StockMindDB];
GO
USE [StockMindDB];
GO

/* Optional ANSI settings */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ==========================================================
   BASE LOOKUPS & ACCESS
   ========================================================== */

CREATE TABLE dbo.Role (
    role_id          BIGINT IDENTITY(1,1) PRIMARY KEY,
    code             VARCHAR(40)   NOT NULL UNIQUE,
    name             NVARCHAR(100) NOT NULL,
    created_at       DATETIME2(0)  NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0)  NOT NULL DEFAULT SYSDATETIME(),
    deleted          BIT           NOT NULL DEFAULT(0)
);

CREATE TABLE dbo.UserAccount (
    user_id       BIGINT IDENTITY(1,1) PRIMARY KEY,
    username      NVARCHAR(100) NOT NULL UNIQUE,
    full_name     NVARCHAR(200) NOT NULL,
    email         NVARCHAR(256) NULL UNIQUE,
    phone_number  NVARCHAR(10)  NULL UNIQUE,
    password_hash NVARCHAR(255) NULL,      -- or managed externally (IdP/SSO)
    is_active     BIT NOT NULL DEFAULT(1),
    created_at    DATETIME2(0)  NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted       BIT NOT NULL DEFAULT(0),
    CONSTRAINT CK_UserAccount_Phone_VN
        CHECK (
            phone_number IS NULL
            OR (
                LEN(phone_number) = 10
                AND LEFT(phone_number,1) = '0'
                AND phone_number NOT LIKE '%[^0-9]%'
            )
        )
);

CREATE TABLE dbo.UserRole (
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    PRIMARY KEY (user_id, role_id),
    FOREIGN KEY (user_id) REFERENCES dbo.UserAccount(user_id),
    FOREIGN KEY (role_id) REFERENCES dbo.Role(role_id)
);

CREATE TABLE dbo.Customer (
    customer_id      BIGINT IDENTITY(1,1) PRIMARY KEY,
    loyalty_code     NVARCHAR(64) NULL UNIQUE,
    full_name        NVARCHAR(200) NOT NULL,
    phone_number     NVARCHAR(15) NOT NULL,
    email            NVARCHAR(256) NULL,
    loyalty_points   INT NOT NULL DEFAULT(0),
    notes            NVARCHAR(500) NULL,
    created_at       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted          BIT NOT NULL DEFAULT(0),
    CONSTRAINT CK_Customer_Phone
        CHECK (
            phone_number IS NOT NULL
            AND LEN(phone_number) BETWEEN 9 AND 15
            AND phone_number NOT LIKE '%[^0-9+]%'
        )
);

CREATE UNIQUE INDEX UX_Customer_Phone
    ON dbo.Customer(phone_number)
    WHERE deleted = 0;

CREATE TABLE dbo.Category (
    category_id        BIGINT IDENTITY(1,1) PRIMARY KEY,
    code               NVARCHAR(50) NOT NULL UNIQUE,
    name               NVARCHAR(200) NOT NULL,
    parent_category_id BIGINT NULL,
    created_at         DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at   DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted            BIT NOT NULL DEFAULT(0),
    FOREIGN KEY (parent_category_id) REFERENCES dbo.Category(category_id)
);

CREATE TABLE dbo.Supplier (
    supplier_id      BIGINT IDENTITY(1,1) PRIMARY KEY,
    name             NVARCHAR(200) NOT NULL,
    contact          NVARCHAR(100) NULL,
    lead_time_days     INT NOT NULL DEFAULT(0) CHECK (lead_time_days >= 0),
    created_at       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted          BIT NOT NULL DEFAULT(0),
    deleted_at       DATETIME2(0) NULL
);

CREATE TABLE dbo.MarginProfile (
    margin_profile_id   BIGINT IDENTITY(1,1) PRIMARY KEY,
    parent_category_id  BIGINT NOT NULL,
    parent_category_name NVARCHAR(200) NOT NULL,
    profile      NVARCHAR(100) NOT NULL,
    price_sensitivity   NVARCHAR(150) NOT NULL,
    min_margin_pct      DECIMAL(5,2) NOT NULL CHECK (min_margin_pct >= 0),
    target_margin_pct   DECIMAL(5,2) NOT NULL CHECK (target_margin_pct >= 0),
    max_margin_pct      DECIMAL(5,2) NOT NULL CHECK (max_margin_pct >= 0),
    notes               NVARCHAR(500) NULL,
    created_at          DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at    DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted             BIT NOT NULL DEFAULT(0),
    CONSTRAINT FK_MarginProfile_Category FOREIGN KEY (parent_category_id) REFERENCES dbo.Category(category_id),
    CONSTRAINT CK_MarginProfile_Order CHECK (min_margin_pct <= target_margin_pct AND target_margin_pct <= max_margin_pct)
);

CREATE UNIQUE INDEX UX_MarginProfile_Category
    ON dbo.MarginProfile(parent_category_id)
    WHERE deleted = 0;

/* ==========================================================
   PRODUCT & INVENTORY CORE
   ========================================================== */

CREATE TABLE dbo.Product (
    product_id     BIGINT IDENTITY(1,1) PRIMARY KEY,
    sku_code        NVARCHAR(64) NOT NULL UNIQUE,
    name           NVARCHAR(255) NOT NULL,
    category_id    BIGINT NULL,
    is_perishable   BIT NOT NULL,                      -- drives FEFO and lot+expiry behaviors
    shelf_life_days  INT NULL,                          -- required when is_perishable=1
    uom            NVARCHAR(16) NOT NULL DEFAULT N'unit',
    price          DECIMAL(19,4) NOT NULL DEFAULT(0),
    media_url      NVARCHAR(1024) NULL,
    min_stock       INT NOT NULL DEFAULT(0) CHECK (min_stock >= 0),
    supplier_id    BIGINT NULL,
    created_at       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted          BIT NOT NULL DEFAULT(0),
    CONSTRAINT FK_Product_Category  FOREIGN KEY (category_id) REFERENCES dbo.Category(category_id),
    CONSTRAINT FK_Product_Supplier  FOREIGN KEY (supplier_id) REFERENCES dbo.Supplier(supplier_id),
    CONSTRAINT CK_Product_PerishableShelfLife
        CHECK ((is_perishable = 1 AND shelf_life_days IS NOT NULL AND shelf_life_days > 0)
            OR (is_perishable = 0))
);

CREATE TABLE dbo.Inventory (
    inventory_id     BIGINT IDENTITY(1,1) PRIMARY KEY,
    product_id       BIGINT NOT NULL,
    on_hand           DECIMAL(19,4) NOT NULL DEFAULT(0),
    created_at       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted          BIT NOT NULL DEFAULT(0),
    CONSTRAINT FK_Inv_Product  FOREIGN KEY (product_id)  REFERENCES dbo.Product(product_id)
);

CREATE TABLE dbo.Lot (
    lot_id      BIGINT IDENTITY(1,1) PRIMARY KEY,
    product_id  BIGINT NOT NULL,
    lot_code     NVARCHAR(64) NOT NULL,
    received_at  DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    expiry_date  DATE NULL,
    qty_on_hand   DECIMAL(19,4) NOT NULL DEFAULT(0),
    created_at       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted          BIT NOT NULL DEFAULT(0),
    CONSTRAINT UQ_Lot_Product_LotCode UNIQUE (product_id, lot_code),
    CONSTRAINT FK_Lot_Product   FOREIGN KEY (product_id)  REFERENCES dbo.Product(product_id),
    CONSTRAINT CK_Lot_Positive  CHECK (qty_on_hand >= 0)
);

/* ==========================================================
   PURCHASING (PO) & RECEIVING (GRN)
   ========================================================== */

CREATE TABLE dbo.PO (
    po_id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    supplier_id      BIGINT NOT NULL,
    status           VARCHAR(16) NOT NULL DEFAULT 'OPEN' CHECK (status IN ('OPEN','RECEIVED','CANCELLED')),
    created_at       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted          BIT NOT NULL DEFAULT(0),
    CONSTRAINT FK_PO_Supplier FOREIGN KEY (supplier_id) REFERENCES dbo.Supplier(supplier_id)
);

CREATE TABLE dbo.POItem (
    po_item_id   BIGINT IDENTITY(1,1) PRIMARY KEY,
    po_id        BIGINT NOT NULL,
    product_id   BIGINT NOT NULL,
    qty_ordered   DECIMAL(19,4) NOT NULL CHECK (qty_ordered > 0),
    expected_date DATE NULL,
    unit_cost     DECIMAL(19,4) NOT NULL CHECK (unit_cost >= 0),
    created_at       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted          BIT NOT NULL DEFAULT(0),
    CONSTRAINT FK_POItem_PO      FOREIGN KEY (po_id)      REFERENCES dbo.PO(po_id),
    CONSTRAINT FK_POItem_Product FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id)
);

CREATE TABLE dbo.GRN (
    grn_id      BIGINT IDENTITY(1,1) PRIMARY KEY,
    po_id       BIGINT NULL,
    received_at  DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    receiver_id  BIGINT NULL,  -- UserAccount
    created_at       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted          BIT NOT NULL DEFAULT(0),
    CONSTRAINT FK_GRN_PO       FOREIGN KEY (po_id)      REFERENCES dbo.PO(po_id),
    CONSTRAINT FK_GRN_Receiver FOREIGN KEY (receiver_id) REFERENCES dbo.UserAccount(user_id)
);

CREATE TABLE dbo.GRNItem (
    grn_item_id  BIGINT IDENTITY(1,1) PRIMARY KEY,
    grn_id       BIGINT NOT NULL,
    product_id   BIGINT NOT NULL,
    lot_id       BIGINT NULL,     -- may be assigned after creating the Lot row
    qty_received  DECIMAL(19,4) NOT NULL CHECK (qty_received > 0),
    unit_cost     DECIMAL(19,4) NOT NULL CHECK (unit_cost >= 0),
    lot_code      NVARCHAR(64) NULL,
    expiry_date   DATE NULL,       -- REQUIRED for perishables (validated via trigger)
    created_at       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted          BIT NOT NULL DEFAULT(0),
    CONSTRAINT FK_GRNItem_GRN     FOREIGN KEY (grn_id)     REFERENCES dbo.GRN(grn_id),
    CONSTRAINT FK_GRNItem_Product FOREIGN KEY (product_id)  REFERENCES dbo.Product(product_id),
    CONSTRAINT FK_GRNItem_Lot     FOREIGN KEY (lot_id)      REFERENCES dbo.Lot(lot_id)
);

/* ==========================================================
   SALES ORDER & ITEMS
   ========================================================== */

CREATE TABLE dbo.SalesOrder (
    order_id         BIGINT IDENTITY(1,1) PRIMARY KEY,
    order_code       NVARCHAR(32) NOT NULL,
    cashier_id       BIGINT NOT NULL,
    customer_id      BIGINT NULL,
    cashier_notes    NVARCHAR(500) NULL,
    items_count      INT NOT NULL DEFAULT(0),
    subtotal         DECIMAL(19,4) NOT NULL DEFAULT(0),
    discount_total   DECIMAL(19,4) NOT NULL DEFAULT(0),
    total            DECIMAL(19,4) NOT NULL DEFAULT(0),
    created_at       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted          BIT NOT NULL DEFAULT(0),
    CONSTRAINT UQ_SalesOrder_Code UNIQUE(order_code),
    CONSTRAINT FK_SalesOrder_Cashier FOREIGN KEY (cashier_id) REFERENCES dbo.UserAccount(user_id),
    CONSTRAINT FK_SalesOrder_Customer FOREIGN KEY (customer_id) REFERENCES dbo.Customer(customer_id)
);

CREATE TABLE dbo.SalesOrderItem (
    order_item_id        BIGINT IDENTITY(1,1) PRIMARY KEY,
    order_id             BIGINT NOT NULL,
    product_id           BIGINT NOT NULL,
    lot_id               BIGINT NULL,
    qty                  DECIMAL(19,4) NOT NULL CHECK (qty > 0),
    unit_price            DECIMAL(19,4) NOT NULL CHECK (unit_price >= 0),
    applied_markdown_percent   DECIMAL(5,2) NULL CHECK (applied_markdown_percent BETWEEN 0 AND 1),
    price_override_reason NVARCHAR(200) NULL,
    line_subtotal        DECIMAL(19,4) NOT NULL DEFAULT(0),
    line_total           DECIMAL(19,4) NOT NULL DEFAULT(0),
    is_weight_based      BIT NOT NULL DEFAULT(0),
    created_at           DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at     DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted              BIT NOT NULL DEFAULT(0),
    CONSTRAINT FK_SOI_Order   FOREIGN KEY (order_id)  REFERENCES dbo.SalesOrder(order_id),
    CONSTRAINT FK_SOI_Product FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id),
    CONSTRAINT FK_SOI_Lot      FOREIGN KEY (lot_id)     REFERENCES dbo.Lot(lot_id));

/* ==========================================================
   STOCK MOVEMENT LEDGER (APPEND-ONLY)
   ========================================================== */

CREATE TABLE dbo.StockMovement (
    movement_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    product_id  BIGINT NOT NULL,
    lot_id      BIGINT NULL,
    qty         DECIMAL(19,4) NOT NULL CHECK (qty <> 0),   -- +IN / -OUT
    type        VARCHAR(24) NOT NULL
                 CHECK (type IN ('IN_RECEIPT','OUT_SALE','OUT_ADJUSTMENT','OUT_WASTE','IN_ADJUSTMENT')),
    ref_type     VARCHAR(24) NULL,     -- e.g., 'GRN','ORDER','ADJUSTMENT','PO','WASTE'
    ref_id       BIGINT NULL,          -- points to the ref record id
    actor_id     BIGINT NULL,          -- UserAccount who caused it
	reason      NVARCHAR(200) NULL,
    created_at   DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_SM_Product FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id),
    CONSTRAINT FK_SM_Lot     FOREIGN KEY (lot_id)     REFERENCES dbo.Lot(lot_id),
    CONSTRAINT FK_SM_Actor   FOREIGN KEY (actor_id)    REFERENCES dbo.UserAccount(user_id)
);
GO

-- Append-only guard
CREATE OR ALTER TRIGGER dbo.TR_StockMovement_NoUpdateDelete
ON dbo.StockMovement
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    RAISERROR('StockMovement is append-only; UPDATE/DELETE is not allowed.', 16, 1);
END;
GO

/* ==========================================================
   PRICING / MARKDOWNS & REPLENISHMENT
   ========================================================== */

CREATE TABLE dbo.MarkdownRule (
    markdown_rule_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    category_id      BIGINT NULL,
    days_to_expiry     INT NOT NULL CHECK (days_to_expiry >= 0),
    discount_percent      DECIMAL(5,2) NOT NULL CHECK (discount_percent BETWEEN 0 AND 1),
    floor_percent_of_cost   DECIMAL(5,2) NOT NULL CHECK (floor_percent_of_cost BETWEEN 0 AND 1),
    created_at       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted          BIT NOT NULL DEFAULT(0),
    CONSTRAINT FK_MR_Category FOREIGN KEY (category_id) REFERENCES dbo.Category(category_id)
);

CREATE TABLE dbo.LotSaleDecision (
    lot_sale_decision_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    lot_id               BIGINT NOT NULL,
    discount_percent     DECIMAL(5,2) NOT NULL CHECK (discount_percent BETWEEN 0 AND 1),
    is_applied           BIT NOT NULL DEFAULT(0),
    created_at           DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at     DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted              BIT NOT NULL DEFAULT(0),
    CONSTRAINT FK_LSD_Lot FOREIGN KEY (lot_id) REFERENCES dbo.Lot(lot_id)
);

CREATE TABLE dbo.ReplenishmentSuggestion (
    repl_id      BIGINT IDENTITY(1,1) PRIMARY KEY,
    product_id   BIGINT NOT NULL,
    on_hand       DECIMAL(19,4) NOT NULL,
    on_order      DECIMAL(19,4) NOT NULL,
    avg_daily     DECIMAL(19,4) NULL,
    sigma_daily   DECIMAL(19,4) NULL,
    lead_time_days INT NOT NULL,
    safety_stock  DECIMAL(19,4) NOT NULL,
    rop          DECIMAL(19,4) NOT NULL,
    suggested_qty DECIMAL(19,4) NOT NULL,
    computed_at   DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    created_at       DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    last_modified_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    deleted          BIT NOT NULL DEFAULT(0),
    CONSTRAINT FK_Repl_Product FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id)
);

/* ==========================================================
   BUSINESS RULE TRIGGERS
   ========================================================== */
GO
CREATE OR ALTER TRIGGER dbo.TR_GRNItem_RequireExpiry_ForPerishable
ON dbo.GRNItem
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Product p ON p.product_id = i.product_id
        WHERE p.is_perishable = 1 AND (i.expiry_date IS NULL OR i.lot_code IS NULL)
    )
    BEGIN
        RAISERROR('Perishable products require lot_code and expiry_date on GRN item.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

/* ==========================================================
   INDEXES FOR HOT PATHS (FEFO/FIFO, REPORTS)
   ========================================================== */

CREATE INDEX IX_Lot_Product_Expiry    ON dbo.Lot(product_id, expiry_date);
CREATE INDEX IX_Lot_Product_Received  ON dbo.Lot(product_id, received_at);
CREATE INDEX IX_Inv_Product           ON dbo.Inventory(product_id);
CREATE INDEX IX_SM_Product_Created    ON dbo.StockMovement(product_id, created_at DESC);
CREATE INDEX IX_SM_Ref                ON dbo.StockMovement(ref_type, ref_id);
CREATE INDEX IX_POItem_PO             ON dbo.POItem(po_id);
CREATE UNIQUE INDEX IX_SalesOrder_OrderCode ON dbo.SalesOrder(order_code);
CREATE INDEX IX_SalesOrderItem_Order  ON dbo.SalesOrderItem(order_id);
CREATE INDEX IX_SalesOrderItem_Lot    ON dbo.SalesOrderItem(lot_id) WHERE lot_id IS NOT NULL;

/* ==========================================================
   SEED DATA
   ========================================================== */
PRINT 'Seeding reference roles...';
INSERT INTO dbo.Role(code, name) VALUES
('ADMIN','Admin'),
('INVENTORY_MANAGER','Inventory Manager'),
('BUYER','Buyer'),
('STORE_STAFF','Store Staff'),
('CASHIER','Cashier (Mock)');
GO

PRINT 'StockMindDB recreated successfully.';
