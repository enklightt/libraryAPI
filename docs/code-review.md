# Code Review

## Review scope

- Compared feature commit `66933b3` on `feature/validation` with base commit `5464333` on `main`/`develop`; also reviewed the local XML documentation, EF index migration, and review-delete correction.
- Inspected the book DTO/model validation, ISBN checksum validator, EF constraints, and migration.
- Inspected the EF snapshot for unique indexes and found that email and ISBN uniqueness was not database-enforced in the original schema.
- Corrected the admin review-delete action to verify that `reviewId` belongs to the `bookId` in the route.
- Ran `dotnet build libraryAPI.csproj --no-restore`: build succeeded. No database migration or automated test suite was run as part of this review.

## Finding

### Migration needs a pre-deployment data check

`Migrations/20260917194034_AddBookValidationConstraints.cs` narrows existing `books` text columns from `longtext` to bounded `varchar` and adds range constraints. The migration does not first check whether existing rows exceed the new string limits or violate the new copy/page/rating ranges. If such rows exist, applying the migration may fail or alter data according to the MySQL SQL mode. This is especially relevant when applying the migration to a database with data created before these validations existed.

**Required before integration:** run read-only checks against the target database for all new length/range limits, then agree on a data remediation or migration strategy for any violating rows. Do not apply this migration to production data until the check is complete.

### Unique indexes require duplicate-data preflight

The EF model now declares unique indexes for `users.email` and `books.isbn`, and migration `20260925200546_AddUniqueUserEmailAndBookIsbn` creates them while limiting email to 254 characters. The previous schema had no such unique indexes, so existing duplicate emails/ISBNs or overlong emails will prevent the migration from being applied.

**Required before integration:** run `docs/database-migration-preflight.sql` against the target database and resolve every returned row/count. The migration has been generated but not applied to any database.

## Review disposition

Local review findings recorded; teammate review is still required. This is not an approval to merge. The feature branch must remain separate, and `main` is not an allowed push or merge target.