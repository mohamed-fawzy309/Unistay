-- Add new columns to Payment table (migration: AddPaymentPaidAtVerificationTokenMonthYear)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Payment') AND name = 'PaidAt')
    ALTER TABLE [Payment] ADD [PaidAt] datetime2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Payment') AND name = 'VerificationToken')
    ALTER TABLE [Payment] ADD [VerificationToken] varchar(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Payment') AND name = 'MonthYear')
    ALTER TABLE [Payment] ADD [MonthYear] nvarchar(10) NULL;

-- Mark migration as applied so EF doesn't try again
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260626210149_AddPaymentPaidAtVerificationTokenMonthYear')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260626210149_AddPaymentPaidAtVerificationTokenMonthYear', '9.0.0');
