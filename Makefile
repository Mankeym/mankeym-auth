.PHONY: up down clean-db build run test test-unit test-int migrate-add migrate-update

# === Переменные с путями к проектам ===
API_PROJ = AuthService.Api/AuthService.Api.csproj
UNIT_PROJ = AuthService.UnitTests/AuthService.UnitTests.csproj
INT_PROJ = AuthService.IntegrationTests/AuthService.IntegrationTests.csproj

# ==========================================
# DOCKER И ОКРУЖЕНИЕ
# ==========================================

# Поднять базу данных и API в фоновом режиме
up:
	docker-compose up -d --build

# Остановить контейнеры
down:
	docker-compose down

# Остановить контейнеры и УДАЛИТЬ данные БД (сброс)
clean-db:
	docker-compose down -v

# ==========================================
# СБОРКА И ЗАПУСК .NET
# ==========================================

build:
	dotnet build

run:
	dotnet run --project $(API_PROJ)

# ==========================================
# ТЕСТИРОВАНИЕ
# ==========================================

# Запустить вообще все тесты
test:
	dotnet test

test-unit:
	dotnet test $(UNIT_PROJ)

test-int:
	dotnet test $(INT_PROJ)

# ==========================================
# БАЗА ДАННЫХ (ENTITY FRAMEWORK)
# ==========================================

# Добавить миграцию. Использование: make migrate-add name=AddUsersTable
migrate-add:
	@if [ -z "$(name)" ]; then echo "Укажите имя миграции: make migrate-add name=MyMigration"; exit 1; fi
	dotnet ef migrations add $(name) --project $(API_PROJ)

# Применить миграции к базе данных
migrate-update:
	dotnet ef database update --project $(API_PROJ)

.PHONY: semgrep
semgrep:
	semgrep scan --config auto --error

.PHONY: semgrep
semgrep-ci:
	semgrep scan --config auto --error --ci

.PHONY: semgrep
semgrep-wasp-top-ten:
	semgrep scan --config p/owasp-top-ten --error
