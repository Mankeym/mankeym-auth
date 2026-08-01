# ===== STAGE 1: Build =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем все проекты
COPY AuthService.sln .
COPY AuthService.Api/*.csproj AuthService.Api/
COPY AuthService.UnitTests/*.csproj AuthService.UnitTests/
COPY AuthService.IntegrationTests/*.csproj AuthService.IntegrationTests/
COPY AuthService.ArchitectureTests/*.csproj AuthService.ArchitectureTests/

# Восстанавливаем все зависимости для всего решения
RUN dotnet restore

# Копируем весь исходный код
COPY . .
# Собираем основной проект (или всё решение)
WORKDIR /src/AuthService.Api
RUN dotnet build -c Release

# ===== STAGE 2: Publish =====
FROM build AS publish
RUN dotnet publish "AuthService.Api.csproj" -c Release -o /app/publish --no-restore --no-build

# ===== STAGE 3: Runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN adduser --disabled-password --no-create-home appuser

COPY --from=publish /app/publish .

# Скопируем .env и проверим
COPY .env .
RUN ls -la /app/.env || echo ".env NOT FOUND"

RUN chown -R appuser:appuser /app
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AuthService.Api.dll"]
