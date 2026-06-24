-- Repair script for a database left in a partial state after the original AddCities migration failed.
-- Run: psql -U postgres -d ticket_system_dev -f scripts/repair-partial-add-cities-migration.sql
--
-- This removes broken route data (city names were lost), rolls back partial DDL,
-- restores the pre-migration Routes schema, and clears the failed migration record
-- so the fixed AddCities migration can be applied with: dotnet ef database update

BEGIN;

DELETE FROM "Tickets";
DELETE FROM "Schedules";
DELETE FROM "Routes";

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN ('20260621101436_AddCities', '20260621102650_AddCityDistanceFromAddis');

DROP INDEX IF EXISTS "IX_Routes_FromCityId_ToCityId";
DROP INDEX IF EXISTS "IX_Routes_ToCityId";
DROP INDEX IF EXISTS "IX_Cities_Name";

ALTER TABLE "Routes" DROP CONSTRAINT IF EXISTS "FK_Routes_Cities_FromCityId";
ALTER TABLE "Routes" DROP CONSTRAINT IF EXISTS "FK_Routes_Cities_ToCityId";

ALTER TABLE "Routes" DROP COLUMN IF EXISTS "FromCityId";
ALTER TABLE "Routes" DROP COLUMN IF EXISTS "ToCityId";

DROP TABLE IF EXISTS "Cities";

ALTER TABLE "Routes" ADD COLUMN IF NOT EXISTS "FromCity" character varying(100) NOT NULL DEFAULT '';
ALTER TABLE "Routes" ADD COLUMN IF NOT EXISTS "ToCity" character varying(100) NOT NULL DEFAULT '';

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Routes_FromCity_ToCity" ON "Routes" ("FromCity", "ToCity");

COMMIT;
