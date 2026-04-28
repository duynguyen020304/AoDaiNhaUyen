<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# workflows

## Purpose
GitHub Actions workflow defs for CI/CD pipeline.

## Key Files
| File | Description |
|------|-------------|
| `deploy-dev.yml` | Main deploy workflow — builds frontend + backend, deploys via SSH/Cloudflare Tunnel |

## For AI Agents
### Working In This Directory
- `deploy-dev.yml` handles: frontend build (Bun + Vite) for multiple hosts (HK1, US1), backend publish (.NET), SSH deploy
- Triggered on push to `master` branch or manual `workflow_dispatch`
- Uses repo secrets for API URLs, SSH keys, host config
- Frontend built per-host with different `VITE_API_BASE_URL` env vars
- Backend published as self-contained .NET app
- Supports native SSH + Cloudflare Tunnel deploy modes
- Uses PM2 for process management on remote servers

## Dependencies
### External
- `oven-sh/setup-bun@v1` — Bun setup
- `actions/setup-dotnet@v4` — .NET setup
- `actions/checkout@v4` — Source checkout
- `actions/upload-artifact@v4` and `actions/download-artifact@v4` — Build artifact storage
- `appleboy/scp-action@v4` and `appleboy/ssh-action@v1` — Remote deploy (note: workflow uses custom rsync/ssh via bash)