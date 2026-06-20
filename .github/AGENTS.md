<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# .github

## Purpose
GitHub Actions CI/CD configuration for automated build and deployment of the AoDaiNhaUyen platform to development servers.

## Key Files
| File | Description |
|------|-------------|
| `workflows/` | GitHub Actions workflow definitions (see `workflows/AGENTS.md`) |

## For AI Agents

### Working In This Directory
- Workflow files trigger on push to `master` and manual `workflow_dispatch`.
- Secrets are sourced from Infisical (development environment slug only) at job runtime — never hardcode values.
- Deploy targets two independent servers: HK1 (Hong Kong) and US1 (United States).
- Frontend builds (Bun + Vite) are per-host with different `VITE_API_BASE_URL` and OAuth env vars.
- Backend is published once as a shared artifact and deployed to both servers.
- Admin frontend is also built per-host.
- Build artifacts are staged via S3-compatible storage between build and deploy jobs.
- SSH deploy uses either native SSH or Cloudflare Tunnel depending on `ENABLE_CLOUDFLARE_TUNNEL` per server.
- PM2 manages all three processes (aodai-api, aodai-frontend, aodai-admin) on the remote server.

### Common Patterns
- Always use `${{ secrets.INFISICAL_TOKEN }}` for Infisical access; never commit credentials.
- Guard against `prod|production` environment slug — `deploy-dev.yml` must never fetch production secrets.
- SSH key stored per server as `HK1_PRODUCTION_SERVER_SSH_KEY` / `US1_PRODUCTION_SERVER_SSH_KEY` in GitHub secrets.
- Cloudflare Access service token (`CF_ACCESS_CLIENT_ID` / `CF_ACCESS_CLIENT_SECRET`) is optional but recommended for tunnel mode.

## Dependencies
### Internal
- `frontend/` — customer SPA source
- `frontend-admin/` — admin SPA source
- `backend/` — API source

### External
- GitHub Actions (ubuntu-latest runners)
- Infisical secret management (`https://infisical.shiphard.studio`)
- Cloudflare Tunnel for SSH deploy (optional per server)
- AWS S3-compatible storage for build artifact staging
- PM2 for process management on remote servers
