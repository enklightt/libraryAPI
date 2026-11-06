# Модуль 4 — Контейнеризація, CI/CD та розгортання

## Розділ 1. Контейнеризація (Docker)

### Dockerfile

Створено **multi-stage** `Dockerfile` в корені проєкту для ASP.NET Core (.NET 10):

- **Етап 1 (build)**: Використовує `mcr.microsoft.com/dotnet/sdk:10.0`. Копіює `.csproj`, виконує `dotnet restore`, потім копіює код і виконує `dotnet publish` в Release.
- **Етап 2 (final)**: Використовує легковісний `mcr.microsoft.com/dotnet/aspnet:10.0`. Копіює лише опублікований результат. Створює непривілейованого користувача `appuser`. Expose порт 8080.

**Збірка та запуск:**
```bash
docker build -t libraryapi:latest .
docker run --rm -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Port=3306;Database=library_db;User=root;Password=xxx" \
  -e JwtSettings__Secret="your-32-char-secret-here-change-it" \
  libraryapi:latest
```

### Docker Compose

Створено `compose.yaml` з двома сервісами:

| Сервіс | Образ | Порт | Опис |
|--------|-------|------|------|
| `api` | Збирається з `Dockerfile` | 8080 | ASP.NET Core API |
| `db` | `mysql:8.0` | 3307→3306 | MySQL база даних |

Особливості:
- `depends_on` з `condition: service_healthy` — API стартує тільки після готовності MySQL
- `healthcheck` для обох сервісів (API: `/health`, DB: `mysqladmin ping`)
- Іменований том `mysql_data` для збереження даних між перезапусками
- Секрети через змінні середовища і `.env` файл

**Запуск:**
```bash
cp .env.docker.example .env   # заповнити реальні значення
docker compose up --build -d
docker compose ps
docker compose logs api
```

### .dockerignore

Створено `.dockerignore` для виключення непотрібних файлів: `.git`, тести, build-артефакти, `.env` файли.

### Container Registry

Для публікації в Docker Hub:
```bash
docker login
docker tag libraryapi:latest YOUR_DOCKERHUB_USERNAME/libraryapi:v1.0.0
docker push YOUR_DOCKERHUB_USERNAME/libraryapi:v1.0.0
```

## Розділ 2. Розгортання (Deployment)

### Платформа

Рекомендована платформа: **Render** (render.com) — підтримує Docker-образи, GitHub інтеграцію, має безкоштовний тариф.

Для розгортання на Render:
1. Створити Web Service → підключити GitHub-репозиторій
2. Вибрати Docker Runtime
3. Додати Environment Variables (секрети):
   - `ConnectionStrings__DefaultConnection`
   - `JwtSettings__Secret`
   - `GroqSettings__ApiKey` (опціонально)
4. Для бази даних — використати Render MySQL або зовнішній сервіс (PlanetScale, Railway)

### CORS

У production потрібно замінити `AllowAnyOrigin()` на конкретні адреси фронтенду через `Cors__AllowedOrigins__0` тощо.

## Розділ 3. CI/CD (GitHub Actions)

### Pipeline `.github/workflows/ci-cd.yml`

| Етап | Job | Тригер | Опис |
|------|-----|--------|------|
| 1. Build & Test | `test` | push/PR в main, develop | Restore → Build → Test |
| 2. Publish | `publish` | push в main (після test) | Build Docker image → Push до Docker Hub |
| 3. Deploy | `deploy` | push в main (після publish) | Trigger Render deploy hook |
| 4. Health Check | `post-deploy-check` | push в main (після deploy) | Перевірка `/health` endpoint |

**Необхідні GitHub Secrets:**
- `DOCKERHUB_USERNAME` — логін Docker Hub
- `DOCKERHUB_TOKEN` — access token Docker Hub
- `RENDER_DEPLOY_HOOK_URL` (опціонально) — webhook URL для автодеплою
- `DEPLOYED_API_URL` (опціонально) — URL розгорнутого API для health check

**Continuous Testing:**
- Тести запускаються автоматично при кожному push та PR
- Docker-образ НЕ публікується, якщо тести впали
- Деплой НЕ виконується, якщо публікація впала

## Результат

| Артефакт | Файл |
|----------|------|
| Dockerfile | `Dockerfile` |
| Docker ignore | `.dockerignore` |
| Docker Compose | `compose.yaml` |
| Env-приклад для Compose | `.env.docker.example` |
| CI/CD pipeline | `.github/workflows/ci-cd.yml` |
