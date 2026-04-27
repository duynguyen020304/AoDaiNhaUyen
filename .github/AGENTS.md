<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# .github

## Purpose
GitHub Actions CI/CD config for auto deploy.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `workflows/` | GitHub Actions workflow definitions (see `workflows/AGENTS.md`) |

## For AI Agents
### Working In This Directory
- Workflows trigger on push to master and manual dispatch
- Secrets via `${{ secrets.* }}` — never hardcode values
- Deploy uses SSH/Cloudflare Tunnel to multiple hosts (HK1, US1)
- Frontend builds use Bun; backend publishes .NET app

## Dependencies
### External
- GitHub Actions (ubuntu-latest runners)
- Cloudflare Tunnel for SSH deploy