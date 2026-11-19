BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118193740_AddDescriptionToBebidas'
)
BEGIN
    ALTER TABLE [Bebidas] ADD [Description] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118193740_AddDescriptionToBebidas'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261118193740_AddDescriptionToBebidas', N'8.0.17');
END;
GO

COMMIT;
GO

