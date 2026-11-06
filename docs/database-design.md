# Database Design

## Scope

The logical model supports book discovery, accounts and roles, reading activity, reviews, personal lists, and friendships. MySQL 8 stores the model through Entity Framework Core; the schema is created and evolved by the migrations in `Migrations/`.

## Entity relationship model

```mermaid
erDiagram
    ROLES ||--o{ USERS : assigns
    USERS ||--o{ REFRESH_TOKENS : owns
    GENRES o|--o{ BOOKS : classifies
    USERS ||--o{ REVIEWS : writes
    BOOKS ||--o{ REVIEWS : receives
    USERS ||--o{ FAVORITES : saves
    BOOKS ||--o{ FAVORITES : appears_in
    USERS ||--o{ WANT_TO_READ : tracks
    BOOKS ||--o{ WANT_TO_READ : appears_in
    USERS ||--o{ READING_PROGRESS : records
    BOOKS ||--o{ READING_PROGRESS : tracks
    USERS ||--o{ DAILY_ACTIVITY : accumulates
    USERS ||--o{ FRIENDS : initiates
    USERS ||--o{ FRIEND_REQUESTS : sends
    USERS ||--o{ FRIEND_REQUESTS : receives
```

`FRIENDS` and `FRIEND_REQUESTS` each reference `USERS` twice, in sender/recipient roles. Accepted friendships are stored as friend links for both users.

## Tables and keys

| Table | Primary key | Important columns and relationships |
|---|---|---|
| `roles` | `Id` | `Name`; one role can be assigned to many users. |
| `users` | `Id` (Guid) | `Name`, required `Email` (max 254; unique index), BCrypt `Password`, `RoleId` FK, active flag, profile fields and timestamps. |
| `refresh_tokens` | `Id` | `UserId` FK, token and expiry/creation timestamps. |
| `genres` | `Id` (Guid) | `Name`; a genre can classify many books. |
| `books` | `Id` (Guid) | Required `Isbn` (max 20; unique index), required title/author, optional `GenreId` FK, copy counts, description, rating, page count, PDF/image/source fields and timestamps. |
| `reviews` | `Id` (Guid) | `BookId` and `UserId` FKs, rating, optional text and timestamps. Unique `(BookId, UserId)` limits a user to one review per book. |
| `favorites` | `(UserId, BookId)` | Both columns are FKs; the composite key prevents duplicate favorites. `AddedAt` records when saved. |
| `want_to_read` | `(UserId, BookId)` | Both columns are FKs; the composite key prevents duplicate list entries. |
| `reading_progress` | `Id` (Guid) | `UserId` and `BookId` FKs, current/total pages, percent, status and timestamps. Unique `(UserId, BookId)` keeps one current progress record per user/book. |
| `daily_activity` | `Id` (Guid) | `UserId` FK, activity date and pages read; used to build the reading heatmap. |
| `friends` | `Id` (Guid) | `UserId` and `FriendUserId` are both FKs to `users`; accepted links are represented for both participants. |
| `friend_requests` | `Id` (Guid) | `FromUserId` and `ToUserId` are FKs to `users`; stores request status and timestamps. |

Tables use explicit `snake_case` names. GUID identifiers are used for most domain records; favorites and reading-list entries use composite keys because membership itself is the relationship.

## Integrity and business rules

- Reviews are unique per `(BookId, UserId)`; progress is unique per `(UserId, BookId)`.
- Email and ISBN uniqueness are enforced by unique database indexes in the EF model and migration `20260925200546_AddUniqueUserEmailAndBookIsbn`.
- Favorites and want-to-read membership are unique per `(UserId, BookId)` by their composite primary keys.
- Book checks constrain `total_copies` to 1–1000 and `available_copies` to 0–`total_copies`.
- Optional book page count is constrained to 1–100000; optional rating is constrained to 0–5.
- Book text-field maximum lengths are enforced by the EF model: title 255, author 150, ISBN 20, quote 2000, description 5000, image URL 1000, and source URL 1000 characters.
- Email is limited to 254 characters; login and registration DTOs apply the same maximum.
- Reading-progress service logic derives completion status at 100% and contributes page counts to daily activity.
- The review service recalculates the book's aggregate rating after review changes.

## Constraint and normalization review

- **Primary/foreign keys:** every table has a primary key; relationships use foreign keys. Favorites and want-to-read use composite keys for their many-to-many links. User-to-role and genre-to-book are one-to-many. There is no required one-to-one relationship in the current feature set.
- **NOT NULL:** required strings and foreign keys are non-null in the EF model; optional profile, book metadata, and review text fields are nullable.
- **UNIQUE:** user email, book ISBN, one review per user/book, and one progress record per user/book are modeled as unique indexes. Favorites and want-to-read pairs are unique by primary key.
- **CHECK:** book copy counts, page count, and rating have database check constraints. Email/ISBN formats and other request rules are validated at the API layer.
- **DEFAULT:** property initializers such as `RoleId = 2`, `IsActive = true`, and book copy counts are application-side defaults; the current EF model does not declare database `DEFAULT` constraints for them.
- **3NF:** user roles and book genres are stored in separate lookup tables, while many-to-many relationships use join tables. `books.rating`, `reading_progress.progress_percent`, and `daily_activity` are derived/aggregated values, so they are deliberate denormalizations maintained by services and can drift if writes bypass those services.

## Findings and migration safety

1. The original schema had no database-level unique indexes for `users.email` or `books.isbn`; service-side checks alone allowed a race between concurrent inserts. The EF model now declares both unique indexes, and the generated migration adds them. **Before applying it**, check for duplicate values and email values longer than 254 characters.
2. Migration `20260917194034_AddBookValidationConstraints` narrows existing book columns and adds checks. Existing rows that violate the new limits can make the migration fail or be altered depending on MySQL SQL mode. Run [database-migration-preflight.sql](database-migration-preflight.sql) against the target database and resolve any returned counts before applying either migration.
3. Derived rating/progress/activity data is not protected from direct database writes. Keep updates inside the owning services or consider recalculation/constraints if direct writes become a supported workflow.

The preflight script is read-only. Its zero-row/zero-count result is a prerequisite check, not a replacement for a backup or review of the migration against a staging copy.

The explicit keys, checks, and string limits are configured in `Data/Appdbcontext.cs` and versioned in `Migrations/`; do not edit the database schema manually without adding a corresponding migration.

## Creating the schema

Configure `ConnectionStrings:DefaultConnection`, then apply migrations:

```powershell
dotnet ef database update
```

The application seeder creates the base roles and genres. Development-only test data is seeded only when `ASPNETCORE_ENVIRONMENT=Development`.