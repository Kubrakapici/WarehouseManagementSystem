/* =====================================================================
   Idempotent schema patch #002: Customers table.
   Safe to run multiple times.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.Customers', 'U') IS NULL
    BEGIN
        PRINT 'Creating dbo.Customers';
        CREATE TABLE dbo.Customers
        (
            Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
            Name        NVARCHAR(256)    NOT NULL,
            CompanyName NVARCHAR(256)    NULL,
            TaxNumber   NVARCHAR(64)     NULL,
            Phone       NVARCHAR(64)     NULL,
            Email       NVARCHAR(256)    NULL,
            Address     NVARCHAR(MAX)    NULL,
            City        NVARCHAR(128)    NULL,
            Notes       NVARCHAR(MAX)    NULL,
            IsActive    BIT              NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT(1),
            CreatedDate DATETIME2        NOT NULL,
            UpdatedDate DATETIME2        NULL,
            CreatedBy   UNIQUEIDENTIFIER NULL,
            UpdatedBy   UNIQUEIDENTIFIER NULL
        );
        CREATE INDEX IX_Customers_Name  ON dbo.Customers(Name);
        CREATE INDEX IX_Customers_Email ON dbo.Customers(Email);
    END

    COMMIT TRANSACTION;
    PRINT 'Schema patch #002 completed successfully.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    PRINT 'Schema patch #002 FAILED: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
