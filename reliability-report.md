# Reliability Report (Module 3)

## Що перевірено

| Перевірка | Результат |
| --- | --- |
| `GET /health` | `200 Healthy` |
| Retry на тимчасові `503` | 3 спроби, успіх на третій |
| Circuit breaker | відкривається після 2 помилок |
| Timeout повільної залежності | `TimeoutRejectedException` |
| Fallback `BookTextService` | повертає `(false, null, ...)` при відкритому circuit breaker |
| Fallback `BookTextService` | повертає `(false, null, ...)` при `HttpRequestException` |

## Тести

Файл: `LibraryAPI.Module3Tests/ReliabilityTests.cs`

```bash
dotnet test LibraryAPI.Module3Tests/LibraryAPI.Module3Tests.csproj
```

Результат: **6 passed, 0 failed**.

Обробка винятків `ExceptionMiddleware` (404/400/403/500 без stack trace)
додатково покрита в `libraryAPI.Tests/ExceptionMiddlewareTests.cs`.
