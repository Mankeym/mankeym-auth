# Analyzer backlog

Diagnostics that are currently suggestions are tracked here so that a clean build
does not imply that all analyzer findings have been eliminated.

| Priority | Rule | Plan |
|---|---|---|
| P1 | CA1062 | Enable per application area after excluding generated migrations and framework callbacks; add guards only at meaningful public boundaries. |
| P2 | CA1305, CA1307, CA1308, CA1310 | Audit culture-sensitive formatting and string comparisons; use invariant culture or an explicit comparison where behavior matters. |
| P2 | CA2000 | Keep as a suggestion for JWT signing keys because IdentityModel caches signing providers; do not dispose a key while the cache can still use it. |
| P3 | CA1002, CA1024, CA1031, CA1054, CA1056, CA1724, CA1725, CA1861, CA1866 | Review gradually when touching the owning API surface; many are framework/DTO-oriented rather than correctness issues. |

EF Core migrations remain excluded because they are generated source. Any new
application warning from an enforced rule must be resolved before merge.
