<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# workflows

## Purpose
GitHub Actions workflow definitions for the CI/CD pipeline. Builds all frontend and backend artifacts, stages them via S3, then deploys to remote servers with health verification.

## Key Files
| File | Description |
|------|-------------|
| `deploy-dev.yml` | Main deploy workflow: parallel per-host frontend builds + shared backend build, S3 artifact staging, SSH/Cloudflare deploy to HK1 and US1, PM2 restart, backend health check |

## For AI Agents

### Working In This Directory

### Pipeline Architecture (`deploy-dev.yml`)
| Job | Depends On | What It Does |
|-----|-----------|--------------|
| `build-frontend-hk1` | — | Fetches Infisical secrets, builds customer SPA with HK1 env vars, uploads `frontend-dist-hk1.tar.gz` to S3 |
| `build-frontend-us1` | — | Same as above for US1 env vars, uploads `frontend-dist-us1.tar.gz` |
| `build-backend` | — | Fetches Infisical secrets, `dotnet publish` AoDaiNhaUyen.Api, uploads `backend-publish.tar.gz` to S3 |
| `build-admin-hk1` | — | Builds admin SPA with HK1 `VITE_API_BASE_URL`, uploads `admin-dist-hk1.tar.gz` to S3 |
| `build-admin-us1` | — | Same for US1, uploads `admin-dist-us1.tar.gz` |
| `deploy-hk1` | build-frontend-hk1, build-backend, build-admin-hk1 | Downloads artifacts from S3, resolves SSH transport, rsync to server, generates `runtime-env.sh`, restarts PM2, health-checks `/health` |
| `deploy-us1` | build-frontend-us1, build-backend, build-admin-us1 | Same as deploy-hk1 for US1 server |

### Deploy Flow Detail
1. Infisical secrets fetched into `$GITHUB_ENV` (masked with `::add-mask::`).
2. Build jobs run in parallel; artifacts uploaded to S3 at path `artifacts/{repo}/{branch}/{sha}/`.
3. Deploy jobs resolve SSH transport: native SSH or Cloudflare Tunnel (validated hostname, not IP).
4. SSH client prepared with server key + optional CF Access service token ProxyCommand.
5. Artifacts downloaded from S3, rsynced to `/tmp/aodai-*-deploy` on server.
6. `runtime-env.sh` generated with all env vars; deployed to `/root/AoDaiNhaUyen/backend/runtime-env.sh` (mode 600).
7. JWT secret fallback: if Infisical JWT key < 64 chars, a persistent server-generated key is used.
8. PM2 processes stopped, dist directories replaced (old dist backed up with timestamp), PM2 restarted.
9. Backend health polled at `$ASPNETCORE_URLS/health` (6 attempts × 10s).
10. Old dist backups pruned to last 3; backend publish backup removed.

### Common Patterns
- `INFISICAL_ENVIRONMENT_SLUG: development` — guard blocks production slug in this file.
- `S3_ARTIFACT_PREFIX: artifacts/{repo}/{branch}/{sha}` — unique per commit, shared across jobs.
- Per-server env var prefix: `HK1_*` / `US1_*` for all host-specific secrets.
- Shared secrets (S3, common OAuth) use unprefixed names.
- `set -euo pipefail` in all bash steps.
- `StrictHostKeyChecking no` + `UserKnownHostsFile /dev/null` for CI SSH (ephemeral runners).

## Dependencies
### External
- `oven-sh/setup-bun@v1` — Bun runtime setup
- `actions/setup-dotnet@v4` — .NET 10 SDK setup
- `actions/checkout@v4` — source checkout
- AWS CLI (pre-installed on ubuntu-latest) — S3 artifact upload/download
- `cloudflared` (downloaded at runtime if tunnel mode enabled) — Cloudflare Tunnel SSH proxy
- Infisical REST API v3 — secret fetching
- PM2 + Bun `serve` — process management on remote servers
