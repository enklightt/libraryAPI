# Test Coverage Report

## Scope

This report covers the Favorites module in the `libraryAPI` ASP.NET Core API.

## Test Results

| Layer | Tests | Result |
| --- | ---: | --- |
| Unit tests for `FavoriteService` | 21 | Passed |
| Mock-based controller tests (Moq) | 12 | Passed |
| API/database integration tests | 12 | Passed |
| End-to-end HTTP workflows | 3 | Passed |
| **Total** | **48** | **48 passed, 0 failed** |

The unit suite includes 10 exception/input-validation scenarios. Integration tests use an isolated EF Core InMemory database. E2E tests exercise registration, login with JWT, favorite add/list/check/remove, and a missing-book failure workflow.

## Coverage

Coverage was collected with Coverlet in Cobertura format.

| Scope | Line coverage | Branch coverage |
| --- | ---: | ---: |
| Entire API repository | 4.68% | 8.38% |
| `FavoriteService` | 100% | 100% |
| `FavoritesController` | 100% | 100% |

The repository-wide percentage is low because this test module intentionally targets Favorites; other API controllers and services are outside this report's scope and need their own tests.

## Remaining Notes

The final .NET test run completed with no build warnings. The prior `Microsoft.OpenApi` high-severity advisory was resolved by upgrading from 2.4.1 to 2.7.5.
