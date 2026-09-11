-- PostgreSQL setup script for GromCore Laser Server
-- Run this script to create the necessary tables in PostgreSQL

-- Table: accounts
CREATE TABLE IF NOT EXISTS accounts (
    "Id" bigint NOT NULL PRIMARY KEY,
    "Trophies" integer NOT NULL DEFAULT 0,
    "Data" jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_accounts_trophies ON accounts("Trophies");

-- Table: alliances
CREATE TABLE IF NOT EXISTS alliances (
    "Id" bigint NOT NULL PRIMARY KEY,
    "Name" varchar(255) NOT NULL,
    "Trophies" integer NOT NULL DEFAULT 0,
    "Data" jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_alliances_trophies ON alliances("Trophies");
CREATE INDEX IF NOT EXISTS idx_alliances_name ON alliances("Name");

-- Table: account_links (created automatically by AccountLinkSystem.Init, but included here for reference)
-- CREATE TABLE IF NOT EXISTS account_links (
--     "AccountId" integer NOT NULL PRIMARY KEY,
--     "AccountLogin" varchar(255) NOT NULL,
--     "AccountEmail" varchar(255) DEFAULT NULL,
--     "AccountPassword" varchar(255) NOT NULL,
--     "AccountLinkedToTelegram" boolean NOT NULL DEFAULT false,
--     "AccountTelegramLinkedId" bigint DEFAULT NULL,
--     "LinkToken" varchar(255) DEFAULT NULL,
--     "VerificationCode" integer DEFAULT NULL,
--     "VerificationCodeAlive" timestamp DEFAULT NULL,
--     "TelegramLinkCode" varchar(255) DEFAULT NULL,
--     "TelegramLinkCodeExpire" timestamp DEFAULT NULL
-- );

