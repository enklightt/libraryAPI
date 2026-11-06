# Звіт з тестування

Додано тести для модуля відгуків (`ReviewService`, `ReviewsController`, `ReviewsApiTests`, `ExceptionMiddleware`).

| Категорія | Файл | Тестів |
| --- | --- | ---: |
| Unit Testing | `libraryAPI.Tests/ReviewServiceTests.cs` | 9 |
| Exception Testing | `libraryAPI.Tests/ExceptionMiddlewareTests.cs` | 5 |
| Mocking (Moq) | `libraryAPI.Tests/ReviewsControllerTests.cs` | 8 |
| Integration Testing | `LibraryAPI.IntegrationTests/ReviewsApiTests.cs` | 2 |
| REST API Testing | `LibraryAPI.IntegrationTests/ReviewsApiTests.cs` | 3 |

## Результат

```
dotnet test libraryAPI.slnx

libraryAPI.Tests              77 passed
LibraryAPI.IntegrationTests   11 passed
LibraryAPI.Module3Tests       12 passed
Total                        100 passed, 0 failed
```

## Покриття (Coverlet)

```
dotnet test libraryAPI.slnx --collect:"XPlat Code Coverage" \
  --settings coverlet.runsettings --results-directory TestResults
```

| Клас | Line coverage | Branch coverage |
| --- | ---: | ---: |
| `ReviewService` | 99.1% | 100% |
| `ReviewsController` | 100% | 100% |
| `ExceptionMiddleware` | 97.4% | 87.5% |
| Весь проєкт | 47.8% | 31.3% |

Тест також виявив і виправив маршрут `GET /api/v1/reviews/my` (було 404).
