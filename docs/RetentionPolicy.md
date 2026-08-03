# Retention policy

- Successfully delivered outbox messages are deleted after 30 days.
- Audit events are retained for 180 days, then archived or deleted according to the organisation's compliance policy.
- Failed outbox messages (`Attempts >= 5` and `ProcessedAtUtc IS NULL`) are never deleted automatically. An admin/support operator must investigate and resolve them.

The cleanup job must only delete messages with a non-null `ProcessedAtUtc`; it must never delete failed messages.
