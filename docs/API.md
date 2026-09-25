# API Reference

Base path: `/api/v1`. The API uses JSON request and response bodies unless noted otherwise. Swagger UI is enabled only in the Development environment at `/swagger`.

## Authentication

Send an access token as `Authorization: Bearer <accessToken>` to protected endpoints. Endpoints described as public do not require a token. Role names are `admin`, `manager`, `librarian`, and `customer`; where a role is listed, only the named roles are allowed.

`[ApiController]` model validation returns `400 Bad Request` with a `message` and field-level `errors`. Protected endpoints return `401` without a valid token; role-restricted endpoints return `403` when the token lacks the required role.

## Auth

| Method | Path | Access | Request | Success |
|---|---|---|---|---|
| `POST` | `/auth/register` | Public | `RegisterDto` | `200`, registered user/token data; `409` on conflict |
| `POST` | `/auth/login` | Public | `LoginDto` | `200`, token data; `401` on invalid credentials |
| `POST` | `/auth/refresh` | Public | `RefreshTokenRequest` | `200`, refreshed token data; `401` if refresh fails |

## Books

| Method | Path | Access | Request | Success / errors |
|---|---|---|---|---|
| `GET` | `/books` | Public | Query: `page` (1), `pageSize` (20), `search`, `genreId`, `sortBy`, `sortOrder` (`asc`) | `200 PagedResponse<BookResponseDto>` |
| `GET` | `/books/{id}` | Public | — | `200 BookResponseDto`; `404` if missing |
| `POST` | `/books` | `admin`, `manager` | `CreateBookDto` | `201 BookResponseDto`; `409` on conflict |
| `PUT` | `/books/{id}` | `admin`, `manager` | `UpdateBookDto` | `200`; `404` if missing; `409` on conflict |
| `DELETE` | `/books/{id}` | `admin`, `manager` | — | `204`; `404` if missing |
| `POST` | `/books/{id}/pdf` | `admin`, `manager` | Multipart form file `pdf` | `200`; `400` on upload failure |
| `GET` | `/books/{id}/pdf` | Public | — | `200 application/pdf`; `404` if missing/unavailable |
| `GET` | `/books/{id}/text` | Public | — | `200 { text, title, author }`; `404` if unavailable |
| `POST` | `/books/{id}/chat` | Authenticated | `ChatRequest`: `question` (2–2000 chars), optional `currentPage` (>=1) | `200 { answer }`; `400` invalid question; `404` if book missing |
| `POST` | `/books/{id}/chat/stream` | Authenticated | Same `ChatRequest` | Server-sent events (`text` chunks, then `[DONE]`; errors are sent as event data) |

## Favorites and reading list

| Method | Path | Access | Request | Success / errors |
|---|---|---|---|---|
| `GET` | `/favorites` | Authenticated | — | `200`, current user's favorites |
| `POST` | `/favorites/{bookId}` | Authenticated | — | `200`; `400` if operation fails |
| `DELETE` | `/favorites/{bookId}` | Authenticated | — | `200`; `404` if not found |
| `GET` | `/favorites/check/{bookId}` | Authenticated | — | `200 { isFavorite }` |
| `GET` | `/want-to-read` | Authenticated | — | `200`, current user's books |
| `GET` | `/want-to-read/{bookId}` | Authenticated | — | `200 boolean` |
| `POST` | `/want-to-read/{bookId}` | Authenticated | — | `200 { isWanted }`; toggles membership |

## Reviews

| Method | Path | Access | Request | Success / errors |
|---|---|---|---|---|
| `GET` | `/books/{bookId}/reviews` | Public | — | `200 ReviewResponseDto[]` |
| `POST` | `/books/{bookId}/reviews` | Authenticated | `CreateReviewDto` | `200 ReviewResponseDto`; creates or updates the caller's review |
| `DELETE` | `/books/{bookId}/reviews` | Authenticated | — | `204`; deletes the caller's review; `404` if missing |
| `GET` | `/reviews/my` | Authenticated | — | `200`, caller's reviews |
| `DELETE` | `/books/{bookId}/reviews/{reviewId}` | `admin`, `manager` | — | `204`; `404` if missing |

## Reading progress

All routes require authentication. Progress is scoped to the current user.

| Method | Path | Request | Success / errors |
|---|---|---|---|
| `POST` | `/reading-progress` | `UpdateReadingProgressDto` | `200 ReadingProgressDto` |
| `GET` | `/reading-progress/my` | — | `200 ReadingProgressDto[]` |
| `GET` | `/reading-progress/book/{bookId}` | — | `200 ReadingProgressDto`; `404` if missing |
| `GET` | `/reading-progress/recent` | Query: `count` (5) | `200 ReadingProgressDto[]` |
| `GET` | `/reading-progress/activity` | Query: `weeks` (12) | `200`, daily activity data |

## Friends

All routes require authentication.

| Method | Path | Request | Success / errors |
|---|---|---|---|
| `GET` | `/friends` | — | `200 FriendDto[]` |
| `POST` | `/friends/request` | `AddFriendDto` | `200`; `400` if request fails |
| `GET` | `/friends/requests` | — | `200 FriendRequestDto[]`, incoming |
| `GET` | `/friends/requests/sent` | — | `200 FriendRequestDto[]`, outgoing |
| `POST` | `/friends/requests/{requestId}/accept` | — | `200`; `400` if request cannot be accepted |
| `POST` | `/friends/requests/{requestId}/reject` | — | `200`; `404` if missing |
| `DELETE` | `/friends/requests/{requestId}` | — | `200`; cancels caller's outgoing request; `404` if missing |
| `DELETE` | `/friends/{friendUserId}` | — | `200`; `404` if friendship missing |
| `GET` | `/friends/{friendUserId}/profile` | — | `200 FriendProfileDto`; `404` if user missing |
| `GET` | `/friends/{friendUserId}/favorites` | — | `200 FavoriteBookDto[]` |
| `GET` | `/friends/{friendUserId}/reading` | — | `200 ReadingProgressDto[]` |
| `GET` | `/friends/search` | Query: `q` | `200 UserSearchResultDto[]` |

## Gamification and genres

| Method | Path | Access | Success |
|---|---|---|---|
| `GET` | `/gamification/achievements` | Authenticated | `200 AchievementDto[]` |
| `GET` | `/gamification/titles` | Authenticated | `200`, unlocked title list |
| `GET` | `/genres` | Public | `200`, genre IDs and names |

## Users

| Method | Path | Access | Request | Success / errors |
|---|---|---|---|---|
| `GET` | `/users/me` | Authenticated | — | `200`, profile, favorites, reading progress, achievements, and stats; `404` if missing |
| `PUT` | `/users/me/title` | Authenticated | JSON `{ "title": "..." }` or `{ "title": null }` | `200 { equippedTitle }` |
| `GET` | `/users` | `admin`, `manager` | — | `200`, user list |
| `PUT` | `/users/{id}/role` | `admin` | JSON string role name | `200`; `404` if user/role missing |
| `PUT` | `/users/{id}/toggle-active` | `admin`, `manager` | — | `200`; `404` if user missing |

For DTO field-level constraints, see the DTO definitions in `DTOs/` and `ChatRequest` in `Controllers/BooksController.cs`. The running Development API's Swagger document is the machine-generated reference for the current build.