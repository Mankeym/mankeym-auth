API_PROJECT := AuthService.Api/AuthService.Api.csproj
UNIT_TESTS := AuthService.UnitTests/AuthService.UnitTests.csproj
INTEGRATION_TESTS := AuthService.IntegrationTests/AuthService.IntegrationTests.csproj
COMPOSE := docker compose

.DEFAULT_GOAL := help

.PHONY: help up up-observability down reset ps logs logs-api logs-db health mailpit \
	build restore run format format-check test test-unit test-integration \
	migrate-add migrate-update compose-config semgrep semgrep-ci semgrep-owasp

help:
	@echo "Local development commands:"
	@echo "  make up                 Build and start API, PostgreSQL, Redis and Mailpit"
	@echo "  make up-observability   Start the stack with Prometheus and Grafana"
	@echo "  make down               Stop containers, preserving PostgreSQL data"
	@echo "  make reset              Stop containers and delete named volumes (clean start)"
	@echo "  make health             Check API readiness and Mailpit"
	@echo "  make logs-api           Follow API logs"
	@echo "  make mailpit            Print the local Mailpit URL"
	@echo "  make test               Run all tests"
	@echo "  make migrate-add NAME=Name  Add an EF Core migration"

up:
	$(COMPOSE) up --build -d

up-observability:
	$(COMPOSE) --profile observability up --build -d

down:
	$(COMPOSE) down

reset:
	$(COMPOSE) down -v

ps:
	$(COMPOSE) ps

logs:
	$(COMPOSE) logs -f

logs-api:
	$(COMPOSE) logs -f auth-api

logs-db:
	$(COMPOSE) logs -f postgres

health:
	curl --fail --silent --show-error http://localhost:5000/health/ready
	@echo
	curl --fail --silent --show-error http://localhost:8025/livez
	@echo

mailpit:
	@echo "Mailpit: http://localhost:8025"

restore:
	dotnet restore AuthService.sln

build:
	dotnet build AuthService.sln --no-restore

run:
	dotnet run --project $(API_PROJECT)

format:
	dotnet format AuthService.sln

format-check:
	dotnet format AuthService.sln --verify-no-changes --no-restore

test:
	dotnet test AuthService.sln --no-restore

test-unit:
	dotnet test $(UNIT_TESTS) --no-restore

test-integration:
	dotnet test $(INTEGRATION_TESTS) --no-restore

migrate-add:
	dotnet ef migrations add $(NAME) --project $(API_PROJECT) --startup-project $(API_PROJECT)

migrate-update:
	dotnet ef database update --project $(API_PROJECT) --startup-project $(API_PROJECT)

compose-config:
	$(COMPOSE) config

semgrep:
	semgrep scan --config auto --error

semgrep-ci:
	semgrep scan --config auto --error --ci

semgrep-owasp:
	semgrep scan --config p/owasp-top-ten --error
