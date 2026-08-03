# Audit events

## Purpose

Audit events record security-sensitive actions for investigation. They must be useful without exposing credentials or personal data.

## Common fields

Every event contains:

| Field | Meaning |
|---|---|
| `ActorUserId` | Authenticated initiator, when available |
| `EventType` | Stable event name from the taxonomy below |
| `Outcome` | `Success`, `Failure`, `Denied`, or `Detected` |
| `CorrelationId` | Request/trace identifier |
| `IpHash` | One-way hash of the client IP |
| `OccurredAtUtc` | Event time in UTC |
| `Metadata` | Safe, event-specific JSON only |

## Taxonomy

| Event type | Outcome | Safe metadata |
|---|---|---|
| `UserRegistered` | `Success`, `Failure` | target user ID, validation error codes |
| `LoginSucceeded` | `Success` | target user ID |
| `LoginFailed` | `Failure` | failure category only |
| `AccountLocked` | `Detected` | target user ID |
| `EmailConfirmationRequested` | `Success`, `Failure` | target user ID |
| `EmailConfirmed` | `Success`, `Failure` | target user ID |
| `ForgotPasswordRequested` | `Success` | target user ID when known |
| `PasswordResetSucceeded` | `Success` | target user ID |
| `SessionRevoked` | `Success` | target session ID, revoke reason code |
| `TokenReuseDetected` | `Detected` | target user ID, session ID |
| `RoleAssigned` | `Success` | target user ID, role name |
| `RoleRemoved` | `Success` | target user ID, role name |

## Forbidden metadata

Never write the following to `Metadata`, logs, traces, or error responses:

- passwords, password hashes, or password-reset answers;
- access, refresh, reset, confirmation, or OAuth tokens;
- OAuth authorization codes or provider responses;
- JWT private keys, SMTP credentials, connection strings, or other secrets;
- raw IP addresses, User-Agent strings, or full email addresses;
- complete request or response bodies from authentication flows.

Use identifiers, allow-listed error codes, and hashed network/device data instead.

## Naming rules

- Event types use PascalCase and describe a completed fact.
- Outcomes use the fixed vocabulary from the common fields table.
- Metadata is additive and backward-compatible; consumers must tolerate unknown fields.
