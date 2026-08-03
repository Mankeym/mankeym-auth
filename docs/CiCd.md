# CI/CD

The GitHub Actions workflow runs on pull requests to `main` and on pushes to `main`.

- Quality job: restore, format verification, Release build, unit tests, integration tests with Testcontainers, and test/coverage artifacts.
- Security jobs: dependency review on pull requests, Gitleaks secret scan, and Trivy image scan.
- Container images use the immutable commit SHA (`authservice:<sha>`), never `latest`.
- A push to `main` also validates that the EF model has no pending migration and builds the release image.

Deploy is intentionally not performed by CI. A deployment workflow must target a GitHub Environment (for example `staging` or `production`) and use its protected secrets/approval rules.
