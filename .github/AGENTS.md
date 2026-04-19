<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# .github

## Purpose
GitHub Actions CI/CD configuration for automated deployment.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `workflows/` | GitHub Actions workflow definitions (see `workflows/AGENTS.md`) |

## For AI Agents
### Working In This Directory
- Workflows trigger on push to master and manual dispatch
- Secrets are referenced via `${{ secrets.* }}` — never hardcode values
- Deployment uses SSH/Cloudflare Tunnel to multiple hosts (HK1, US1)
- Frontend builds use Bun; backend publishes .NET app

## Dependencies
### External
- GitHub Actions (ubuntu-latest runners)
- Cloudflare Tunnel for SSH deployment
