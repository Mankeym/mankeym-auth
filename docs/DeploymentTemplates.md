# Deployment templates

These templates are deliberately inactive: this pet project has no selected cloud/VPS/Kubernetes target.

## GitHub Environments

Create `staging` and `production` in GitHub repository settings. Restrict `production` to `main` and require reviewer approval.

Set these **environment-scoped** secrets, never repository files:

| Secret | Purpose |
|---|---|
| `DB_CONNECTION` | PostgreSQL connection string |
| `REDIS_CONFIGURATION` | Redis endpoint/configuration |
| `JWT_PRIVATE_KEY` | RS256 signing key |
| `JWT_PUBLIC_KEY` | RS256 validation key |
| target-specific credentials | SSH key, kubeconfig, cloud token or registry credential |

## Enabling deploy later

1. Choose a target and copy `templates/github/deploy.yml` to `.github/workflows/deploy.yml`.
2. Replace the placeholder with the target's official deploy command/action.
3. Copy `templates/compose.production.yaml` outside source control if using Docker Compose on a VPS.
4. Deploy only an immutable SHA image tag. Do not deploy `latest`.
5. Run migrations as one controlled release job before increasing application replicas.
6. Verify `/health/ready`, then retain the prior image SHA for rollback.

## Rollback

Redeploy the preceding image SHA. Do not automatically roll back a database migration; use forward-compatible migrations and an explicit recovery plan.
