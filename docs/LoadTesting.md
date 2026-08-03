# Load testing

The baseline scenario is [auth-flow.js](../loadtests/auth-flow.js).

```bash
k6 run -e BASE_URL=http://localhost:5000 -e AUTH_EMAIL=user@example.test -e AUTH_PASSWORD='replace-me' loadtests/auth-flow.js
```

It runs five virtual users for 30 seconds and checks login/refresh responses. `429` on login is an expected outcome once the intentionally low abuse-protection limit is reached; it is not an API availability failure.

Record a run's machine, Docker resource limits, timestamp, p95 latency, error rate and observed `429` count before comparing results. No portable numerical baseline is committed because it would depend on the local machine and containers.
