# ADR-0002: SPA token storage

**Status:** Accepted  
**Date:** 2026-08-03

## Decision

Keep the refresh token only in an `HttpOnly`, `Secure`, `SameSite=Strict` cookie. Return the short-lived access token in the login/refresh response and keep it in SPA memory only.

## Rationale

`localStorage` and `sessionStorage` expose refresh tokens to XSS. An HttpOnly cookie prevents JavaScript from reading it. SameSite Strict limits cross-site cookie sending; state-changing API endpoints must not opt into unsafe cross-site credentialed CORS.

## Consequences

- A browser reload requires a refresh request to obtain a new in-memory access token.
- The SPA must not persist access or refresh tokens in browser storage.
- Non-browser clients use an explicit secure token transport and do not rely on the browser cookie model.
