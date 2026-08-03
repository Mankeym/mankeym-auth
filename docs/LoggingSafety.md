# Logging safety

Request logging records method, path, status and duration only. It never records request/response bodies or headers.
Audit metadata is persisted only after the audit taxonomy validation and is not written to application logs.

The following values are forbidden in any log property or message: `Authorization`, password, password hash, refresh token, reset token, email-confirmation token, OAuth code, client secret and private key.
