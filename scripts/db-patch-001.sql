/* =====================================================================
   Idempotent schema patch for new features (UserWarehouses, Purchase
   Requisitions, Supplier Quotes, smart-shelf columns on Locations).
   Safe to run multiple times.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    /* ---------- 1) Locations: smart-shelf columns ------------------- */
    IF COL_LENGTH('dbo.Locations', 'MaxCapacity') IS NULL
    BEGIN
        PRINT 'Adding Locations.MaxCapacity';
        ALTER TABLE dbo.Locations ADD MaxCapacity INT NULL;
    END

    IF COL_LENGTH('dbo.Locations', 'PickSortOrder') IS NULL
    BEGIN
        PRINT 'Adding Locations.PickSortOrder';
        ALTER TABLE dbo.Locations ADD PickSortOrder INT NULL;
    END

    /* ---------- 2) UserWarehouses ----------------------------------- */
    IF OBJECT_ID('dbo.UserWarehouses', 'U') IS NULL
    BEGIN
        PRINT 'Creating dbo.UserWarehouses';
        CREATE TABLE dbo.UserWarehouses
        (
            Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserWarehouses PRIMARY KEY,
            UserId      UNIQUEIDENTIFIER NOT NULL,
            WarehouseId UNIQUEIDENTIFIER NOT NULL,
            CONSTRAINT FK_UserWarehouses_Users
                FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
            CONSTRAINT FK_UserWarehouses_Warehouses
                FOREIGN KEY (WarehouseId) REFERENCES dbo.Warehouses(Id) ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IX_UserWarehouses_UserId_WarehouseId
            ON dbo.UserWarehouses(UserId, WarehouseId);
    END

    /* ---------- 3) PurchaseRequisitions ----------------------------- */
    IF OBJECT_ID('dbo.PurchaseRequisitions', 'U') IS NULL
    BEGIN
        PRINT 'Creating dbo.PurchaseRequisitions';
        CREATE TABLE dbo.PurchaseRequisitions
        (
            Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PurchaseRequisitions PRIMARY KEY,
            RequestNumber       NVARCHAR(450)    NOT NULL,
            Title               NVARCHAR(MAX)    NULL,
            Status              INT              NOT NULL,
            WarehouseId         UNIQUEIDENTIFIER NOT NULL,
            RequestedByUserId   UNIQUEIDENTIFIER NOT NULL,
            ApprovedByUserId    UNIQUEIDENTIFIER NULL,
            ApprovedDate        DATETIME2        NULL,
            Notes               NVARCHAR(MAX)    NULL,
            CreatedDate         DATETIME2        NOT NULL,
            UpdatedDate         DATETIME2        NULL,
            CreatedBy           UNIQUEIDENTIFIER NULL,
            UpdatedBy           UNIQUEIDENTIFIER NULL,
            CONSTRAINT FK_PurchaseRequisitions_Warehouses
                FOREIGN KEY (WarehouseId) REFERENCES dbo.Warehouses(Id) ON DELETE NO ACTION,
            CONSTRAINT FK_PurchaseRequisitions_Users_RequestedBy
                FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,
            CONSTRAINT FK_PurchaseRequisitions_Users_ApprovedBy
                FOREIGN KEY (ApprovedByUserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION
        );
        CREATE UNIQUE INDEX IX_PurchaseRequisitions_RequestNumber
            ON dbo.PurchaseRequisitions(RequestNumber);
        CREATE INDEX IX_PurchaseRequisitions_WarehouseId
            ON dbo.PurchaseRequisitions(WarehouseId);
        CREATE INDEX IX_PurchaseRequisitions_RequestedByUserId
            ON dbo.PurchaseRequisitions(RequestedByUserId);
        CREATE INDEX IX_PurchaseRequisitions_ApprovedByUserId
            ON dbo.PurchaseRequisitions(ApprovedByUserId);
    END

    /* ---------- 4) PurchaseRequisitionLines ------------------------- */
    IF OBJECT_ID('dbo.PurchaseRequisitionLines', 'U') IS NULL
    BEGIN
        PRINT 'Creating dbo.PurchaseRequisitionLines';
        CREATE TABLE dbo.PurchaseRequisitionLines
        (
            Id                      UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PurchaseRequisitionLines PRIMARY KEY,
            PurchaseRequisitionId   UNIQUEIDENTIFIER NOT NULL,
            ProductId               UNIQUEIDENTIFIER NOT NULL,
            Quantity                INT              NOT NULL,
            Notes                   NVARCHAR(MAX)    NULL,
            CreatedDate             DATETIME2        NOT NULL,
            UpdatedDate             DATETIME2        NULL,
            CreatedBy               UNIQUEIDENTIFIER NULL,
            UpdatedBy               UNIQUEIDENTIFIER NULL,
            CONSTRAINT FK_PurchaseRequisitionLines_Requisitions
                FOREIGN KEY (PurchaseRequisitionId)
                REFERENCES dbo.PurchaseRequisitions(Id) ON DELETE CASCADE,
            CONSTRAINT FK_PurchaseRequisitionLines_Products
                FOREIGN KEY (ProductId)
                REFERENCES dbo.Products(Id) ON DELETE NO ACTION
        );
        CREATE INDEX IX_PurchaseRequisitionLines_PurchaseRequisitionId
            ON dbo.PurchaseRequisitionLines(PurchaseRequisitionId);
        CREATE INDEX IX_PurchaseRequisitionLines_ProductId
            ON dbo.PurchaseRequisitionLines(ProductId);
    END

    /* ---------- 5) SupplierQuotes ----------------------------------- */
    IF OBJECT_ID('dbo.SupplierQuotes', 'U') IS NULL
    BEGIN
        PRINT 'Creating dbo.SupplierQuotes';
        CREATE TABLE dbo.SupplierQuotes
        (
            Id                      UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SupplierQuotes PRIMARY KEY,
            PurchaseRequisitionId   UNIQUEIDENTIFIER NOT NULL,
            SupplierId              UNIQUEIDENTIFIER NOT NULL,
            TotalAmount             DECIMAL(18, 2)   NOT NULL,
            Currency                NVARCHAR(8)      NOT NULL,
            Status                  INT              NOT NULL,
            Notes                   NVARCHAR(MAX)    NULL,
            CreatedDate             DATETIME2        NOT NULL,
            UpdatedDate             DATETIME2        NULL,
            CreatedBy               UNIQUEIDENTIFIER NULL,
            UpdatedBy               UNIQUEIDENTIFIER NULL,
            CONSTRAINT FK_SupplierQuotes_Requisitions
                FOREIGN KEY (PurchaseRequisitionId)
                REFERENCES dbo.PurchaseRequisitions(Id) ON DELETE CASCADE,
            CONSTRAINT FK_SupplierQuotes_Suppliers
                FOREIGN KEY (SupplierId)
                REFERENCES dbo.Suppliers(Id) ON DELETE NO ACTION
        );
        CREATE INDEX IX_SupplierQuotes_PurchaseRequisitionId
            ON dbo.SupplierQuotes(PurchaseRequisitionId);
        CREATE INDEX IX_SupplierQuotes_SupplierId
            ON dbo.SupplierQuotes(SupplierId);
    END

    COMMIT TRANSACTION;
    PRINT 'Schema patch completed successfully.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    PRINT 'Schema patch FAILED: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
