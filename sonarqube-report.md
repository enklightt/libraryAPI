# SonarQube — звіт (Module 3)

## Як відтворити

```bash
docker run -d --name sonarqube -p 9000:9000 \
  -e SONAR_ES_BOOTSTRAP_CHECKS_DISABLE=true sonarqube:community

# дочекатися статусу UP
curl -s http://localhost:9000/api/system/status

# токен (замініть на свій)
export SONAR_TOKEN=<token>

dotnet-sonarscanner begin \
  /k:"libraryAPI" /n:"libraryAPI" /v:"1.0" \
  /d:sonar.host.url="http://localhost:9000" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.cs.opencover.reportsPaths="TestResults/**/coverage.opencover.xml" \
  /d:sonar.exclusions="**/bin/**,**/obj/**,**/Migrations/**,**/wwwroot/**,LibraryAPI.Module3Tests/**"

dotnet build
dotnet test --settings coverlet.runsettings \
  --collect:"XPlat Code Coverage" --results-directory TestResults
dotnet-sonarscanner end /d:sonar.token="$SONAR_TOKEN"
```

Dashboard: <http://localhost:9000/dashboard?id=libraryAPI>

## Результати (фінальний прогін)

| Метрика | Значення |
|---|---:|
| Bugs | **0** |
| Vulnerabilities | **0** |
| Security hotspots | **0** |
| Reliability rating | **A (1.0)** |
| Security rating | **A (1.0)** |
| Maintainability rating | **A (1.0)** |
| Technical debt | **0** |
| Code smells | 16 (INFO) |
| Duplicated lines | 4.4% |
| Coverage | 34.4% |
| Lines of code (NCLOC) | 4063 |

## Що виправлено після першого прогону

Початковий прогін: 2 bugs, 2 vulnerabilities, 27 smells, ratings B/C.

1. **CORS wildcard** — прибрано `AllowAnyOrigin()`; дозволені лише origins з
   `Cors:AllowedOrigins`, у Development — локальні адреси. У Production без
   налаштованих origins застосунок не стартує.
2. **Path traversal у PDF** — `BookPdfService` перевіряє, що локальний шлях
   залишається всередині `wwwroot`; `..\..\appsettings.json` повертає 404.
3. **Weak RNG у TestDataSeeder** — `Random` замінено на
   `RandomNumberGenerator.GetInt32`.
4. **Book без namespace** — модель перенесено в `LibraryAPI.Models`.
5. **Cancellation у ExceptionMiddleware** — `WriteAsync(..., RequestAborted)`.
6. **`app.Run()` → `await app.RunAsync()`**, прибрано зайвий
   `AddEndpointsApiExplorer()`, використано typed-властивості заголовків.
7. **LIKE-пошук** — замість `ToLower().Contains(...)` використано
   `EF.Functions.Like` з екрануванням `%`, `_`, `\`.

## Залишкові 16 code smells

Усі — INFO, технічний борг 0. Це переважно підказки Sonar щодо structured
logging (`LogInformation` з властивостями DTO) та стилю. Вони не впливають на
рейтинги A/A/A.

## Покриття

Покриття 34.4% з 12 інтеграційними тестами (`LibraryAPI.Module3Tests`).
Найкраще покрито: контролери книг/жанрів, авторизація, доступ до профілів друзів,
кешування, валідація, path traversal. Для зростання покриття варто додати
unit-тести на `AuthService`, `ReadingProgressService`, `GamificationService`.
