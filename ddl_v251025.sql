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
    role_id       BIGINT IDENTITY(1,1) PRIMARY KEY,
    code          VARCHAR(40)   NOT NULL UNIQUE,
    name          NVARCHAR(100) NOT NULL
);

CREATE TABLE dbo.UserAccount (
    user_id       BIGINT IDENTITY(1,1) PRIMARY KEY,
    username      NVARCHAR(100) NOT NULL UNIQUE,
    full_name     NVARCHAR(200) NOT NULL,
    email         NVARCHAR(256) NULL UNIQUE,
    phone_number  NVARCHAR(10)  NULL UNIQUE,
    password_hash NVARCHAR(255) NULL,      -- or managed externally (IdP/SSO)
    is_active     BIT NOT NULL DEFAULT(1),
    created_at    DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
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

CREATE TABLE dbo.Category (
    category_id  BIGINT IDENTITY(1,1) PRIMARY KEY,
    code         NVARCHAR(50) NOT NULL UNIQUE,
    name         NVARCHAR(200) NOT NULL,
    parent_category_id  BIGINT NULL,
    FOREIGN KEY (parent_category_id) REFERENCES dbo.Category(category_id)
);

CREATE TABLE dbo.Supplier (
    supplier_id   BIGINT IDENTITY(1,1) PRIMARY KEY,
    name          NVARCHAR(200) NOT NULL,
    contact       NVARCHAR(100) NULL,
    leadTimeDays  INT NOT NULL DEFAULT(0) CHECK (leadTimeDays >= 0)
);

/* ==========================================================
   PRODUCT & INVENTORY CORE
   ========================================================== */

CREATE TABLE dbo.Product (
    product_id     BIGINT IDENTITY(1,1) PRIMARY KEY,
    skuCode        NVARCHAR(64) NOT NULL UNIQUE,
    name           NVARCHAR(255) NOT NULL,
    category_id    BIGINT NULL,
    isPerishable   BIT NOT NULL,                      -- drives FEFO and lot+expiry behaviors
    shelfLifeDays  INT NULL,                          -- required when isPerishable=1
    uom            NVARCHAR(16) NOT NULL DEFAULT N'unit',
    price          DECIMAL(19,4) NOT NULL DEFAULT(0),
    minStock       INT NOT NULL DEFAULT(0) CHECK (minStock >= 0),
    leadTimeDays   INT NOT NULL DEFAULT(0) CHECK (leadTimeDays >= 0),
    supplier_id    BIGINT NULL,
    createdAt      DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    updatedAt      DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Product_Category  FOREIGN KEY (category_id) REFERENCES dbo.Category(category_id),
    CONSTRAINT FK_Product_Supplier  FOREIGN KEY (supplier_id) REFERENCES dbo.Supplier(supplier_id),
    CONSTRAINT CK_Product_PerishableShelfLife
        CHECK ((isPerishable = 1 AND shelfLifeDays IS NOT NULL AND shelfLifeDays > 0)
            OR (isPerishable = 0))
);

CREATE TABLE dbo.Inventory (
    inventory_id  BIGINT IDENTITY(1,1) PRIMARY KEY,
    product_id    BIGINT NOT NULL,
    onHand        DECIMAL(19,4) NOT NULL DEFAULT(0),
    updatedAt     DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Inv_Product  FOREIGN KEY (product_id)  REFERENCES dbo.Product(product_id)
);

CREATE TABLE dbo.Lot (
    lot_id      BIGINT IDENTITY(1,1) PRIMARY KEY,
    product_id  BIGINT NOT NULL,
    lotCode     NVARCHAR(64) NOT NULL,
    receivedAt  DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    expiryDate  DATE NULL,
    qtyOnHand   DECIMAL(19,4) NOT NULL DEFAULT(0),
    CONSTRAINT UQ_Lot_Product_LotCode UNIQUE (product_id, lotCode),
    CONSTRAINT FK_Lot_Product   FOREIGN KEY (product_id)  REFERENCES dbo.Product(product_id),
    CONSTRAINT CK_Lot_Positive  CHECK (qtyOnHand >= 0)
);

/* ==========================================================
   PURCHASING (PO) & RECEIVING (GRN)
   ========================================================== */

CREATE TABLE dbo.PO (
    po_id       BIGINT IDENTITY(1,1) PRIMARY KEY,
    supplier_id BIGINT NOT NULL,
    status      VARCHAR(16) NOT NULL DEFAULT 'OPEN' 
                 CHECK (status IN ('OPEN','RECEIVED','CANCELLED')),
    createdAt   DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_PO_Supplier FOREIGN KEY (supplier_id) REFERENCES dbo.Supplier(supplier_id)
);

CREATE TABLE dbo.POItem (
    po_item_id   BIGINT IDENTITY(1,1) PRIMARY KEY,
    po_id        BIGINT NOT NULL,
    product_id   BIGINT NOT NULL,
    qtyOrdered   DECIMAL(19,4) NOT NULL CHECK (qtyOrdered > 0),
    expectedDate DATE NULL,
    unitCost     DECIMAL(19,4) NOT NULL CHECK (unitCost >= 0),
    CONSTRAINT FK_POItem_PO      FOREIGN KEY (po_id)      REFERENCES dbo.PO(po_id),
    CONSTRAINT FK_POItem_Product FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id)
);

CREATE TABLE dbo.GRN (
    grn_id      BIGINT IDENTITY(1,1) PRIMARY KEY,
    po_id       BIGINT NULL,
    receivedAt  DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    receiverId  BIGINT NULL,  -- UserAccount
    CONSTRAINT FK_GRN_PO       FOREIGN KEY (po_id)      REFERENCES dbo.PO(po_id),
    CONSTRAINT FK_GRN_Receiver FOREIGN KEY (receiverId) REFERENCES dbo.UserAccount(user_id)
);

CREATE TABLE dbo.GRNItem (
    grn_item_id  BIGINT IDENTITY(1,1) PRIMARY KEY,
    grn_id       BIGINT NOT NULL,
    product_id   BIGINT NOT NULL,
    lot_id       BIGINT NULL,     -- may be assigned after creating the Lot row
    qtyReceived  DECIMAL(19,4) NOT NULL CHECK (qtyReceived > 0),
    unitCost     DECIMAL(19,4) NOT NULL CHECK (unitCost >= 0),
    lotCode      NVARCHAR(64) NULL,
    expiryDate   DATE NULL,       -- REQUIRED for perishables (validated via trigger)
    CONSTRAINT FK_GRNItem_GRN     FOREIGN KEY (grn_id)     REFERENCES dbo.GRN(grn_id),
    CONSTRAINT FK_GRNItem_Product FOREIGN KEY (product_id)  REFERENCES dbo.Product(product_id),
    CONSTRAINT FK_GRNItem_Lot     FOREIGN KEY (lot_id)      REFERENCES dbo.Lot(lot_id)
);

/* ==========================================================
   SALES ORDER & ITEMS
   ========================================================== */

CREATE TABLE dbo.SalesOrder (
    order_id   BIGINT IDENTITY(1,1) PRIMARY KEY,
    createdAt  DATETIME2(0) NOT NULL DEFAULT SYSDATETIME()
);

CREATE TABLE dbo.SalesOrderItem (
    order_item_id        BIGINT IDENTITY(1,1) PRIMARY KEY,
    order_id             BIGINT NOT NULL,
    product_id           BIGINT NOT NULL,
    qty                  DECIMAL(19,4) NOT NULL CHECK (qty > 0),
    unitPrice            DECIMAL(19,4) NOT NULL CHECK (unitPrice >= 0),
    appliedMarkdownPercent   DECIMAL(5,2) NULL CHECK (appliedMarkdownPercent BETWEEN 0 AND 1),
    CONSTRAINT FK_SOI_Order   FOREIGN KEY (order_id)  REFERENCES dbo.SalesOrder(order_id),
    CONSTRAINT FK_SOI_Product FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id)
);

/* ==========================================================
   STOCK MOVEMENT LEDGER (APPEND-ONLY)
   ========================================================== */

CREATE TABLE dbo.StockMovement (
    movement_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    product_id  BIGINT NOT NULL,
    lot_id      BIGINT NULL,
    qty         DECIMAL(19,4) NOT NULL CHECK (qty <> 0),   -- +IN / -OUT
    type        VARCHAR(24) NOT NULL
                 CHECK (type IN ('IN_RECEIPT','OUT_SALE','ADJUSTMENT','OUT_WASTE','IN_ADJUSTMENT')),
    refType     VARCHAR(24) NULL,     -- e.g., 'GRN','ORDER','ADJUSTMENT','PO','WASTE'
    refId       BIGINT NULL,          -- points to the ref record id
    actorId     BIGINT NULL,          -- UserAccount who caused it
	reason      NVARCHAR(200) NULL,
    createdAt   DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_SM_Product FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id),
    CONSTRAINT FK_SM_Lot     FOREIGN KEY (lot_id)     REFERENCES dbo.Lot(lot_id),
    CONSTRAINT FK_SM_Actor   FOREIGN KEY (actorId)    REFERENCES dbo.UserAccount(user_id)
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
    daysToExpiry     INT NOT NULL CHECK (daysToExpiry >= 0),
    discountPercent      DECIMAL(5,2) NOT NULL CHECK (discountPercent BETWEEN 0 AND 1),
    floorPercentOfCost   DECIMAL(5,2) NOT NULL CHECK (floorPercentOfCost BETWEEN 0 AND 1),
    CONSTRAINT FK_MR_Category FOREIGN KEY (category_id) REFERENCES dbo.Category(category_id)
);

CREATE TABLE dbo.ReplenishmentSuggestion (
    repl_id      BIGINT IDENTITY(1,1) PRIMARY KEY,
    product_id   BIGINT NOT NULL,
    onHand       DECIMAL(19,4) NOT NULL,
    onOrder      DECIMAL(19,4) NOT NULL,
    avgDaily     DECIMAL(19,4) NULL,
    sigmaDaily   DECIMAL(19,4) NULL,
    leadTimeDays INT NOT NULL,
    safetyStock  DECIMAL(19,4) NOT NULL,
    rop          DECIMAL(19,4) NOT NULL,
    suggestedQty DECIMAL(19,4) NOT NULL,
    computedAt   DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
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
        WHERE p.isPerishable = 1 AND (i.expiryDate IS NULL OR i.lotCode IS NULL)
    )
    BEGIN
        RAISERROR('Perishable products require lotCode and expiryDate on GRN item.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

/* ==========================================================
   INDEXES FOR HOT PATHS (FEFO/FIFO, REPORTS)
   ========================================================== */

CREATE INDEX IX_Lot_Product_Expiry    ON dbo.Lot(product_id, expiryDate);
CREATE INDEX IX_Lot_Product_Received  ON dbo.Lot(product_id, receivedAt);
CREATE INDEX IX_Inv_Product           ON dbo.Inventory(product_id);
CREATE INDEX IX_SM_Product_Created    ON dbo.StockMovement(product_id, createdAt DESC);
CREATE INDEX IX_SM_Ref                ON dbo.StockMovement(refType, refId);
CREATE INDEX IX_POItem_PO             ON dbo.POItem(po_id);
CREATE INDEX IX_SalesOrderItem_Order  ON dbo.SalesOrderItem(order_id);

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
