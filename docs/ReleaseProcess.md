# Release process

Release Please creates and maintains a release pull request from commits merged into `main`. Merging that pull request updates `CHANGELOG.md`, creates a SemVer tag, and publishes a GitHub Release.

The workflow uses the `RELEASE_PLEASE_TOKEN` Actions secret rather than `GITHUB_TOKEN`, so the normal CI workflow runs on generated release pull requests.

Release pull requests have squash auto-merge enabled by the workflow. GitHub merges them only after all status checks required by the `main` ruleset have passed and the branch is up to date.

## Versioning rules

| Commit format | Version change | Example |
|---|---|---|
| `feat:` | Minor | `feat: add passkey registration` → `0.1.0` to `0.2.0` |
| `fix:` | Patch | `fix: revoke reused refresh-token family` → `0.1.0` to `0.1.1` |
| `type!:` or `BREAKING CHANGE:` footer | Major | `feat!: replace refresh endpoint contract` → `0.1.0` to `1.0.0` |
| `docs:`, `test:`, `ci:`, `build:`, `chore:`, `refactor:`, `style:` | No version bump alone | Included in the changelog when a releasable commit is present. |

Use `fix(security):` for a security patch so it both appears in the security context and triggers a patch release. The configured policy does not weaken SemVer while the version is below `1.0.0`: a breaking change still creates the next major version, and a feature still creates the next minor version.

## Commit examples

```text
feat: add recovery-code generation
fix: prevent duplicate outbox delivery
fix(security): reject an invalid OAuth state
feat!: rename the refresh endpoint

BREAKING CHANGE: clients must use /api/v2/auth/refresh.
```

The first Release Please pull request includes the complete existing history. Later releases include only commits since the previous release tag.

## Verifying the release workflow

To verify the full release path after changing the workflow, merge a small `feat:` or `fix:` pull request. Release Please should create a release pull request, enable squash auto-merge, wait for the required checks, then publish the next tag and GitHub Release.
