# API reference

The API is versioned under `/api/v1`. In Development, the interactive OpenAPI UI is available at `/scalar` and the document at `/openapi/v1.json`.

## Conventions

- Protected endpoints require `Authorization: Bearer <access-token>`.
- Login and refresh set a `refreshToken` cookie (`HttpOnly`, `Secure`, `SameSite=Strict`). Do not send refresh tokens in JSON, URLs, or logs.
- JSON validation errors use `application/problem+json` with HTTP `422`.
- Protected endpoints return `401` without valid authentication and `403` without a required permission.
- Abuse-protected endpoints may return `429` with `Retry-After`.
- All timestamps are UTC ISO-8601 values.

## Authentication

| Method | Path | Auth | Purpose |
|---|---|---|---|
| POST | `/auth/register` | Anonymous | Register an account and request confirmation email. |
| POST | `/auth/login` | Anonymous | Return access token and set refresh cookie. |
| POST | `/auth/refresh` | Refresh cookie | Rotate refresh cookie and return access token. |
| POST | `/auth/logout` | Refresh cookie | Revoke the refresh-token family and clear the cookie. |
| POST | `/auth/forgot-password` | Anonymous | Request password-reset email; response does not disclose account existence. |
| POST | `/auth/reset-password` | Anonymous | Reset password and revoke sessions. |
| POST | `/auth/confirm-email` | Anonymous | Confirm email with the one-time token. |
| POST | `/auth/request-email-confirmation` | Anonymous | Request another confirmation email. |
| GET | `/auth/external/{provider}` | Anonymous | Start configured external OAuth flow. |
| GET | `/auth/external/{provider}/callback` | Anonymous | OAuth callback; sets cookie and redirects without tokens in URL. |
| DELETE | `/auth/external/{provider}` | Bearer | Unlink an external provider. |

`/auth/*` paths in this table are relative to `/api/v1`.

### Login request and response

```http
POST /api/v1/auth/login
Content-Type: application/json

{ "email": "user@example.test", "password": "ReplaceWithLocalPassword123!" }
```

```json
{ "accessToken": "eyJ..." }
```

The response also includes `Set-Cookie: refreshToken=...`; clients must use credentialed requests and keep the cookie inaccessible to JavaScript.

## User and session endpoints

| Method | Path | Required permission | Purpose |
|---|---|---|---|
| GET | `/users/me` | Authenticated | Current profile and permissions. |
| GET | `/sessions/me` | Authenticated | Current user's sessions. |
| DELETE | `/sessions/{sessionId}` | Authenticated | Revoke one of the current user's sessions. |
| POST | `/users/{id}/roles` | `users.manage` | Assign a role. Body is a JSON role name string. |
| DELETE | `/users/{id}/roles/{role}` | `users.manage` | Remove a role. |

## Administration

| Method | Path | Required permission | Purpose |
|---|---|---|---|
| GET | `/roles` | `roles.read` | List roles. |
| GET | `/roles/{role}/permissions` | `roles.read` | List permissions for one role. |
| GET | `/admin/outbox/failed` | `audit.read` | List failed outbox messages without delivery payloads. |

## Health and metrics

| Method | Path | Purpose |
|---|---|---|
| GET | `/health/live` | Process liveness. |
| GET | `/health/ready` | PostgreSQL and Redis readiness. |
| GET | `/metrics` | Prometheus metrics endpoint. |

For ready-to-send local examples, use [AuthService.http](../examples/AuthService.http). Use placeholders only; never commit tokens, passwords, email links, or OAuth credentials.
