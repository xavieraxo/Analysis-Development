-- Script SQL para crear tablas de Identity manualmente
-- Esto evita conflictos con tablas existentes

CREATE TABLE IF NOT EXISTS "IdentityRoles" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_IdentityRoles" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NULL,
    "NormalizedName" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL
);

CREATE TABLE IF NOT EXISTS "IdentityUsers" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_IdentityUsers" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Role" INTEGER NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "LastLoginAt" TEXT NULL,
    "LegacyUserId" INTEGER NULL,
    "UserName" TEXT NULL,
    "NormalizedUserName" TEXT NULL,
    "Email" TEXT NULL,
    "NormalizedEmail" TEXT NULL,
    "EmailConfirmed" INTEGER NOT NULL,
    "PasswordHash" TEXT NULL,
    "SecurityStamp" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL,
    "PhoneNumber" TEXT NULL,
    "PhoneNumberConfirmed" INTEGER NOT NULL,
    "TwoFactorEnabled" INTEGER NOT NULL,
    "LockoutEnd" TEXT NULL,
    "LockoutEnabled" INTEGER NOT NULL,
    "AccessFailedCount" INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS "IdentityRoleClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_IdentityRoleClaims" PRIMARY KEY AUTOINCREMENT,
    "RoleId" INTEGER NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_IdentityRoleClaims_IdentityRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "IdentityRoles" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "IdentityUserClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_IdentityUserClaims" PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_IdentityUserClaims_IdentityUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "IdentityUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "IdentityUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT NULL,
    "UserId" INTEGER NOT NULL,
    CONSTRAINT "PK_IdentityUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_IdentityUserLogins_IdentityUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "IdentityUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "IdentityUserRoles" (
    "UserId" INTEGER NOT NULL,
    "RoleId" INTEGER NOT NULL,
    CONSTRAINT "PK_IdentityUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_IdentityUserRoles_IdentityRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "IdentityRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_IdentityUserRoles_IdentityUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "IdentityUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "IdentityUserTokens" (
    "UserId" INTEGER NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT NULL,
    CONSTRAINT "PK_IdentityUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_IdentityUserTokens_IdentityUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "IdentityUsers" ("Id") ON DELETE CASCADE
);

-- Crear índices
CREATE UNIQUE INDEX IF NOT EXISTS "RoleNameIndex" ON "IdentityRoles" ("NormalizedName");
CREATE INDEX IF NOT EXISTS "IX_IdentityRoleClaims_RoleId" ON "IdentityRoleClaims" ("RoleId");
CREATE INDEX IF NOT EXISTS "IX_IdentityUserClaims_UserId" ON "IdentityUserClaims" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_IdentityUserLogins_UserId" ON "IdentityUserLogins" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_IdentityUserRoles_RoleId" ON "IdentityUserRoles" ("RoleId");
CREATE INDEX IF NOT EXISTS "EmailIndex" ON "IdentityUsers" ("NormalizedEmail");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_IdentityUsers_Email" ON "IdentityUsers" ("Email");
CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex" ON "IdentityUsers" ("NormalizedUserName");

