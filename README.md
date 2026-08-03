# Auth Service

Auth Service — pet-проект на .NET 9, демонстрирующий production-oriented подход к аутентификации: RS256 JWT, refresh-token rotation, role/permission authorization, audit log, transactional outbox, Redis rate limiting и observability.

## Quick start

1. Start the local stack:

   ```bash
   docker compose up --build
   ```

2. Open Scalar at `http://localhost:5000/scalar` and Mailpit at `http://localhost:8025`.

The Development profile creates ephemeral RS256 keys automatically and uses a local default database password. For persistent local JWT keys or a custom database password, copy `.env.example` to `.env` and configure User Secrets. To include Prometheus and Grafana, run `docker compose --profile observability up --build`.

To reset only Docker data (without touching source files):

```bash
docker compose down -v
```

If `make` is installed, the same commands are available as `make up`, `make health`, `make logs-api`, `make reset` and `make up-observability`. Run `make help` to list them.

## Git hooks

Enable the repository hook once after cloning:

```bash
make hooks-install
```

Before each commit it verifies formatting of staged C# files, performs a clean build and runs unit tests. The formatting baseline is migrated incrementally, so existing unrelated files do not block a commit. `make pre-commit` runs the build and unit-test parts manually. Integration tests remain a separate command (`make test-integration`) because they start Docker containers.

## What it demonstrates

- Access JWT signed by RS256; refresh token kept in an HttpOnly/Secure/SameSite cookie and rotated on use.
- Reuse detection revokes the refresh-token family and emits an audit event/metric.
- Permissions are policy-based (`users.manage`, `roles.read`, `audit.read`), not role-name checks.
- Audit events are persisted with business changes; email notifications use a transactional outbox with retry/backoff.
- Redis-backed limits protect login, register, reset, refresh and OAuth callback endpoints.
- OpenTelemetry traces, Prometheus metrics, Grafana dashboard and structured JSON logs support local troubleshooting.

## Architecture and decisions

- [Architecture diagrams](docs/ArchitectureDiagrams.md)
- [Security design](docs/SecurityDesign.md)
- [JWT signing ADR](docs/adr/0001-jwt-signing.md)
- [SPA token-storage ADR](docs/adr/0002-spa-token-storage.md)
- [Audit taxonomy](docs/AuditEvents.md)

## Tests

```bash
dotnet test AuthService.UnitTests/AuthService.UnitTests.csproj
dotnet test AuthService.IntegrationTests/AuthService.IntegrationTests.csproj
```

Integration tests launch PostgreSQL and Redis through Testcontainers. CI runs formatting, tests, secret/dependency scans and a container scan; see [CI/CD](docs/CiCd.md).

## Safe API examples

Use [AuthService.http](examples/AuthService.http). It uses placeholders only and does not contain tokens, passwords or real email addresses.

## Known boundaries

- Notification Service is represented by an outbox transport contract; its receiver must deduplicate `messageId` values.
- Local observability uses default Grafana credentials and must not be exposed publicly.
- Deployment templates are intentionally inactive until a real target is chosen.
