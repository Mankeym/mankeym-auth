# Auth service: actors, flows and security contract

## Actors

| Actor | Capabilities |
|---|---|
| Anonymous user | Register, login, confirm email, request/reset password, OAuth start/callback. |
| Authenticated user | View own profile/sessions, refresh access token, logout, revoke own session, unlink provider. |
| Administrator | Manage user roles; view roles/permission mappings and failed outbox messages. |
| Service-to-service client | Validates Auth Service access tokens using the RS256 public key; never receives the signing private key. |

## User stories

- As an anonymous user, I can register and receive a generic confirmation response.
- As a registered user, I can log in after email confirmation and receive a short-lived access token plus a refresh-token cookie.
- As a user, I can confirm my email through a single-use confirmation token.
- As a user who forgot a password, I receive the same generic response whether or not my email exists; a valid reset revokes active sessions.
- As an authenticated user, I can refresh access without re-entering credentials; refresh token rotation detects reuse.
- As an authenticated user, I can log out and revoke an individual session.

## Token lifetime policy

| Token | Default TTL | Change only when |
|---|---:|---|
| Access JWT | 60 minutes | Risk assessment, user experience or downstream-validation requirements change. |
| Refresh token | 7 days | Product session policy changes or a security incident requires shortening it. |
| Password reset / email confirmation | 1 hour | Threat model or user-support data indicates a different safe window. |

Any TTL change requires updating tests and release notes. Refresh tokens are rotated on use; password reset revokes all active sessions.

## Threat model and mitigations

| Threat | Mitigation |
|---|---|
| Credential stuffing | Redis login limits, Identity lockout, audit events. |
| Token theft | HttpOnly/Secure/SameSite refresh cookie, short access TTL, session revocation and refresh rotation. |
| Refresh replay | One-time refresh tokens, family revocation on reuse, alert metric. |
| Email enumeration | Identical generic response for unknown and known email. |
| CSRF | Refresh token is cookie-bound with `SameSite=Strict`; state-changing browser requests must remain same-site. |
| Open redirect | Frontend redirect URL allow-list. |
| OAuth state attack | Framework external-auth correlation/state validation; callback rate limiting. |

## Privacy policy

- Store hashed IP and user-agent values only where they are needed for session/audit correlation.
- Do not store raw IP/user-agent, passwords, token values, OAuth codes, secrets or request bodies in audit metadata or logs.
- Retain audit events 180 days; retain successful outbox messages 30 days; do not auto-delete failed outbox messages.

## Public error contract

| Situation | HTTP | Public code/message |
|---|---:|---|
| Invalid credentials or unknown account | 400 | `invalid_credentials` |
| Email confirmation/reset requested | 202/200 | Generic success response, never confirms account existence. |
| Missing/invalid refresh token | 401 | `invalid_refresh_token` |
| Missing permission | 403 | `forbidden` |
| Rate limit | 429 | `rate_limited` with `Retry-After` |
| Invalid request model | 422 | ProblemDetails validation errors |

Internal causes belong in structured logs/audit data only, never in client responses.
