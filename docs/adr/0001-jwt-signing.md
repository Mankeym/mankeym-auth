# ADR-0001: JWT signing algorithm

**Status:** Accepted  
**Date:** 2026-08-03

## Context

Auth Service issues access tokens. Other services may need to validate those tokens without being allowed to issue new ones. The application supports two realistic approaches:

- symmetric HMAC signing (`HS256`): one shared secret signs and validates;
- asymmetric RSA signing (`RS256`): Auth Service signs with a private key, other services validate with a public key.

## Decision

Use **RS256** for production and any multi-service deployment.

Auth Service keeps `JwtSettings:PrivateKey` only in User Secrets locally and in a secret store in deployed environments. Consumers receive only `JwtSettings:PublicKey`. Private keys must never be committed, emitted in logs, sent through API responses, or added to Docker image layers.

For a strictly local single-service MVP, `HS256` is acceptable when key distribution is not a concern. Its secret must still be stored outside source control and rotated if exposed.

## Consequences

### RS256

- A compromised downstream service can validate tokens but cannot mint them.
- Public keys can be distributed through configuration or a JWKS endpoint.
- Key rotation can use `kid`: publish the new public key, begin signing with its private key, then retire the old key after all old access tokens expire.
- RSA key management and signing have slightly more operational complexity than HMAC.

### HS256

- Setup is simpler: a single random secret is enough.
- Every validating service receives the same secret and can therefore forge tokens; this is unsuitable for a production multi-service topology.
- Rotation requires updating the secret in every validator at the same time or allowing a temporary overlap of secrets.

## Operational rules

- Access tokens remain short-lived; signing algorithm choice does not replace expiry, issuer, audience and signature validation.
- Refresh tokens are opaque random values and are not JWTs.
- Store PEM material with escaped newlines correctly preserved; do not base64-decode it unless the chosen secret store requires base64 transport.
- Local Docker mounts the existing User Secrets file read-only; production uses a managed secret store.
