# HTTP security policy

- API accepts JSON request bodies only, up to 1 MiB. Oversized bodies receive `413`; unsupported content types receive `415`.
- CORS uses an explicit `Cors:AllowedOrigins` allow-list and supports credentials only for those origins.
- Production enables HTTPS redirection and HSTS. Security headers are emitted for every response.
- The API does not use an application authentication cookie: access and refresh tokens are sent by the client, so cookie-based CSRF protection is not applicable. OAuth's framework correlation cookie remains `HttpOnly`, `Secure` in production and `SameSite=Lax`.
- `Authorization`, passwords, refresh tokens and reset tokens must never be written to application logs.
