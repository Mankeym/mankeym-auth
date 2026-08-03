# Portfolio notes

## Key trade-offs

1. **RS256 over HS256:** more key-management work, but downstream services validate tokens without receiving the signing key.
2. **Refresh token in cookie:** lowers XSS exposure versus localStorage; requires deliberate SameSite/CORS/CSRF policy.
3. **Outbox at-least-once delivery:** avoids lost notifications after DB commit, but receiver-side `messageId` deduplication is required.
4. **Redis rate-limit fail-open:** preserves availability during a Redis outage, while health checks/alerts surface reduced abuse protection.
5. **EF/PostgreSQL integration tests:** slower than mocks but catch migration and concurrency failures.

## Interview story

“The service signs short-lived access tokens with RS256 and rotates opaque refresh tokens. Reusing a refresh token revokes its whole family. Permissions are claims backed by role mappings, so authorization is explicit and testable. Admin changes are audited in the same persistence flow, while email delivery goes through an outbox to avoid a commit/publish gap. The test suite uses Testcontainers for real PostgreSQL and Redis, including rate limits, refresh concurrency and security edge cases.”
