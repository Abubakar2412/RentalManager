-- Reference DDL only. The application uses the C# EF migration as its authoritative migration.
-- Execute within Chwaya_rentalDb only; creates normalized rental tables.
CREATE TABLE [Properties] (
 [Kind] nvarchar(10) NOT NULL,
 [FloorArea] decimal(18,2) NOT NULL,
 [BusinessType] nvarchar(150) NOT NULL,
 [RowVersion] rowversion NOT NULL,
 [Id] int IDENTITY(1,1) NOT NULL,
 [Name] nvarchar(120) NOT NULL,
 [Address] nvarchar(250) NOT NULL,
 [Bedrooms] int NOT NULL,
 [Bathrooms] int NOT NULL,
 [MonthlyRent] decimal(18,2) NOT NULL,
 [Amenities] nvarchar(3000) NOT NULL,
 [Available] bit NOT NULL,
 CONSTRAINT [PK_Properties] PRIMARY KEY ([Id]),
 CONSTRAINT [CK_Properties_Kind] CHECK ([Kind] IN ('House','Shop')),
 CONSTRAINT [CK_Properties_Rent] CHECK ([MonthlyRent] > 0)
);
CREATE TABLE [Tenants] (
 [RowVersion] rowversion NOT NULL,
 [Id] int IDENTITY(1,1) NOT NULL,
 [FullName] nvarchar(120) NOT NULL,
 [NationalId] nvarchar(80) NOT NULL,
 [Phone] nvarchar(40) NOT NULL,
 [Email] nvarchar(150) NULL,
 [Address] nvarchar(250) NOT NULL,
 [Occupation] nvarchar(150) NOT NULL,
 [EmergencyName] nvarchar(120) NOT NULL,
 [EmergencyPhone] nvarchar(40) NOT NULL,
 [BankName] nvarchar(100) NOT NULL,
 [AccountName] nvarchar(120) NOT NULL,
 [AccountNumber] nvarchar(80) NOT NULL,
 CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
);
CREATE TABLE [Leases] (
 [RowVersion] rowversion NOT NULL,
 [Id] int IDENTITY(1,1) NOT NULL,
 [PropertyId] int NOT NULL,
 [TenantId] int NOT NULL,
 [StartDate] date NOT NULL,
 [EndDate] date NOT NULL,
 [MonthlyRent] decimal(18,2) NOT NULL,
 [Deposit] decimal(18,2) NOT NULL,
 [DueDay] int NOT NULL,
 [Occupants] int NOT NULL,
 [NoticeDays] int NOT NULL,
 [Status] nvarchar(max) NOT NULL,
 [SpecialTerms] nvarchar(10000) NOT NULL,
 [WitnessName] nvarchar(120) NOT NULL,
 [WitnessPhone] nvarchar(40) NOT NULL,
 CONSTRAINT [PK_Leases] PRIMARY KEY ([Id]),
 CONSTRAINT [CK_Leases_Dates] CHECK ([EndDate] >= [StartDate]),
 CONSTRAINT [CK_Leases_Status] CHECK ([Status] IN ('Active','Ended','Cancelled')),
 CONSTRAINT [CK_Leases_Amounts] CHECK ([MonthlyRent]>0 AND [Deposit]>=0 AND [DueDay] BETWEEN 1 AND 28),
 CONSTRAINT [FK_Leases_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]),
 CONSTRAINT [FK_Leases_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id])
);
CREATE TABLE [Payments] (
 [RowVersion] rowversion NOT NULL,
 [Id] int IDENTITY(1,1) NOT NULL,
 [LeaseId] int NOT NULL,
 [PaidOn] date NOT NULL,
 [Period] date NOT NULL,
 [Amount] decimal(18,2) NOT NULL,
 [Kind] nvarchar(max) NOT NULL,
 [Method] nvarchar(max) NOT NULL,
 [Reference] nvarchar(120) NOT NULL,
 [Notes] nvarchar(1000) NOT NULL,
 CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
 CONSTRAINT [CK_Payments_Amount] CHECK ([Amount]>0),
 CONSTRAINT [CK_Payments_Kind] CHECK ([Kind] IN ('Rent','Deposit','Other')),
 CONSTRAINT [FK_Payments_Leases_LeaseId] FOREIGN KEY ([LeaseId]) REFERENCES [Leases] ([Id])
);
CREATE TABLE [Landlords] (
 [RowVersion] rowversion NOT NULL,
 [Id] int NOT NULL,
 [Name] nvarchar(150) NOT NULL,
 [Address] nvarchar(250) NOT NULL,
 [Phone] nvarchar(40) NOT NULL,
 [Email] nvarchar(150) NULL,
 [NationalId] nvarchar(80) NOT NULL,
 [BankName] nvarchar(100) NOT NULL,
 [AccountName] nvarchar(120) NOT NULL,
 [AccountNumber] nvarchar(80) NOT NULL,
 [Currency] nvarchar(10) NOT NULL,
 [ContractTerms] nvarchar(10000) NOT NULL,
 CONSTRAINT [PK_Landlords] PRIMARY KEY ([Id]),
 CONSTRAINT [CK_Landlords_Singleton] CHECK ([Id]=1)
);
CREATE TABLE [Admins] (
 [SecurityStamp] nvarchar(32) NOT NULL,
 [Id] int NOT NULL,
 [Username] nvarchar(80) NOT NULL,
 [PasswordHash] nvarchar(1000) NOT NULL,
 CONSTRAINT [PK_Admins] PRIMARY KEY ([Id]),
 CONSTRAINT [CK_Admins_Singleton] CHECK ([Id]=1)
);
CREATE TABLE [Contracts] (
 [Id] int IDENTITY(1,1) NOT NULL,
 [LeaseId] int NOT NULL,
 [CreatedAt] datetimeoffset NOT NULL,
 [Html] nvarchar(max) NOT NULL,
 CONSTRAINT [PK_Contracts] PRIMARY KEY ([Id]),
 CONSTRAINT [FK_Contracts_Leases_LeaseId] FOREIGN KEY ([LeaseId]) REFERENCES [Leases] ([Id])
);
CREATE TABLE [Audit] (
 [Id] int IDENTITY(1,1) NOT NULL,
 [At] datetimeoffset NOT NULL,
 [Actor] nvarchar(80) NOT NULL,
 [Action] nvarchar(30) NOT NULL,
 [Entity] nvarchar(40) NOT NULL,
 [EntityId] int NOT NULL,
 CONSTRAINT [PK_Audit] PRIMARY KEY ([Id])
);
CREATE UNIQUE INDEX [IX_Tenants_NationalId] ON [Tenants] ([NationalId]);
CREATE UNIQUE INDEX [IX_Admins_Username] ON [Admins] ([Username]);
CREATE INDEX [IX_Leases_PropertyId_StartDate_EndDate] ON [Leases] ([PropertyId],[StartDate],[EndDate]);
CREATE INDEX [IX_Leases_TenantId] ON [Leases] ([TenantId]);
CREATE INDEX [IX_Payments_LeaseId] ON [Payments] ([LeaseId]);
CREATE INDEX [IX_Contracts_LeaseId] ON [Contracts] ([LeaseId]);
GO
CREATE OR ALTER TRIGGER [TR_Leases_NoOverlap] ON [Leases] AFTER INSERT,UPDATE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS (
  SELECT 1 FROM inserted i JOIN [Leases] l WITH (UPDLOCK,HOLDLOCK)
  ON l.PropertyId=i.PropertyId AND l.Id<>i.Id
  AND l.Status='Active' AND i.Status='Active'
  AND l.StartDate<=i.EndDate AND l.EndDate>=i.StartDate
 )
 BEGIN
  THROW 51001, 'Overlapping active rental lease for this house or shop.', 1;
 END
END
GO
