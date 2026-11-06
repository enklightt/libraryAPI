# ─────────────────────────────────────────────────────────
#  Multi-stage Dockerfile for LibraryAPI (.NET 10)
# ─────────────────────────────────────────────────────────

# Етап 1. Збірка застосунку
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копіюємо тільки файли проєктів для кешування restore
COPY libraryAPI.csproj ./
RUN dotnet restore libraryAPI.csproj

# Копіюємо решту коду та публікуємо
COPY . .
RUN dotnet publish libraryAPI.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Етап 2. Запуск застосунку
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Порт для контейнера
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

USER app
ENTRYPOINT ["dotnet", "libraryAPI.dll"]
