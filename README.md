# Aplikacija za upravljanje partnerima

Ovo je web aplikacija razvijena za upravljanje partnerima osiguravajućeg društva i njihovim policama, izrađena prema zadanoj specifikaciji.

## Tehnologije

* **Framework:** .NET 10
* **Baza podataka:** MS SQL Server
* **ORM:** Dapper Micro ORM
* **UI:** Bootstrap 4

## Postavljanje baze podataka

Za inicijalizaciju baze, pokrenite SQL skriptu u vašem SQL Server Management Studiu. Skripta automatski kreira tablice, postavlja relacije i popunjava šifrarnike.

```sql
-- 1. Kreiranje pomoćnih tablica
CREATE TABLE Genders (Id INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL);
CREATE TABLE PartnerTypes (Id INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL);
CREATE TABLE UserRoles (Id INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL);

-- 2. Punjenje pomoćnih tablica
INSERT INTO Genders (Id, Name) VALUES (1, 'M'), (2, 'F'), (3, 'N');
INSERT INTO PartnerTypes (Id, Name) VALUES (1, 'Personal'), (2, 'Legal');
INSERT INTO UserRoles (Id, Name) VALUES (1, 'Admin'), (2, 'User');

-- 3. Kreiranje tablice Users
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Email NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    PasswordHash NVARCHAR(MAX) NOT NULL,
    RoleId INT,
    CreatedAtUtc DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CreatedByUserId INT NULL,
    ModifiedAtUtc DATETIME2(7) NULL,
    ModifiedByUserId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Users_Role FOREIGN KEY (RoleId) REFERENCES UserRoles(Id),
    CONSTRAINT FK_Users_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id),
    CONSTRAINT FK_Users_ModifiedBy FOREIGN KEY (ModifiedByUserId) REFERENCES Users(Id)
);

-- 4. Kreiranje tablice Partners
CREATE TABLE Partners (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FirstName NVARCHAR(255) NOT NULL,
    LastName NVARCHAR(255) NOT NULL,
    Address NVARCHAR(MAX),
    PartnerNumber VARCHAR(20) NOT NULL,
    CroatianPIN VARCHAR(11),
    PartnerTypeId INT NOT NULL,
    GenderId INT NOT NULL,
    IsForeign BIT NOT NULL,
    ExternalCode NVARCHAR(20) UNIQUE, -- Jedinstven kod
    CreatedAtUtc DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CreatedByUserId INT NULL,
    ModifiedAtUtc DATETIME2(7) NULL,
    ModifiedByUserId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Partners_Type FOREIGN KEY (PartnerTypeId) REFERENCES PartnerTypes(Id),
    CONSTRAINT FK_Partners_Gender FOREIGN KEY (GenderId) REFERENCES Genders(Id),
    CONSTRAINT FK_Partners_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id),
    CONSTRAINT FK_Partners_ModifiedBy FOREIGN KEY (ModifiedByUserId) REFERENCES Users(Id)
);

-- 5. Kreiranje tablice Policies
CREATE TABLE Policies (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PartnerId INT NOT NULL,
    PolicyNumber NVARCHAR(15) NOT NULL,
    Amount DECIMAL(18, 2) NOT NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CreatedByUserId INT NULL,
    ModifiedAtUtc DATETIME2(7) NULL,
    ModifiedByUserId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Policies_Partner FOREIGN KEY (PartnerId) REFERENCES Partners(Id),
    CONSTRAINT FK_Policies_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id),
    CONSTRAINT FK_Policies_ModifiedBy FOREIGN KEY (ModifiedByUserId) REFERENCES Users(Id)
);

```

## Konfiguracija aplikacije

Prije pokretanja, ažurirajte `appsettings.json` sa vašim `ConnectionStrings` parametrima:


## Prvo pokretanje

Aplikacija pri prvom pokretanju automatski kreira administratora (ukoliko ne postoji u bazi):

* **Email:** `weiner@gmail.com`
* **Lozinka:** `Admin123!`

## Bilješka o implementaciji

* **Vizualno označavanje:** Partneri s više od 5 polica ili iznosom većim od 5000 € označeni su simbolom `*` ispred imena u realnom vremenu.
* **Validacija:** Implementirana je serverska i klijentska validacija za sve obavezne podatke (PartnerNumber, ExternalCode itd.).
* **Vizualni feedback:** Nakon uspješnog unosa partnera, korisnik se preusmjerava na listu gdje je novi partner vizualno istaknut.

