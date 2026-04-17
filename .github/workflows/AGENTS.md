<!-- Parent: ../../AGENTS.md -->

# CI/CD Workflows

## Purpose

GitHub Actions workflow for building and deploying the Ao Dai Nha Uyen project to two servers (HK1 and US1). Triggered on pushes to the `dev` branch or manual `workflow_dispatch`.

## Key Files

| File | Description |
|------|-------------|
| `deploy-dev.yml` | Build and deploy workflow for dev environment |

## Workflow: `deploy-dev.yml`

**Trigger:** Push to `dev` branch or manual `workflow_dispatch`

**Pipeline:**

```
build-frontend-hk1 ──┐   build-frontend-us1 ──┐   build-backend
(HK1 env vars)       │   (US1 env vars)       │   (shared)
                      │                        │        │
                      ▼                        ▼        ▼
                 deploy-hk1                deploy-us1
```

### Build Jobs

| Job | Runner | Description |
|-----|--------|-------------|
| `build-frontend-hk1` | `ubuntu-latest` | Builds Vite React SPA with HK1 env vars |
| `build-frontend-us1` | `ubuntu-latest` | Builds Vite React SPA with US1 env vars |
| `build-backend` | `ubuntu-latest` | Builds .NET 10 API (shared, no server-specific env) |

Frontend builds use Bun. Backend uses `dotnet publish AoDaiNhaUyen.Api -c Release`.

### Deploy Jobs

Each deploy job supports **direct SSH** or **Cloudflare Tunnel** independently.

| Job | Runner | Depends On | Deploys To |
|-----|--------|------------|------------|
| `deploy-hk1` | `ubuntu-latest` | `build-frontend-hk1`, `build-backend` | HK1 server |
| `deploy-us1` | `ubuntu-latest` | `build-frontend-us1`, `build-backend` | US1 server |

### Deploy Steps (per server)

1. Download server-specific frontend artifact + shared backend artifact
2. Resolve SSH transport — native SSH or Cloudflare Tunnel
3. Prepare SSH client — install rsync/openssh-client, optionally cloudflared, write SSH config
4. Verify SSH connectivity
5. Deploy frontend and backend via rsync to `/tmp/` staging dirs
6. Finalize via remote SSH script:
   - Ensure .NET 10 runtime on server
   - Create deployment directories under `/root/AoDaiNhaUyen/`
   - Write backend `runtime-env.sh` with production secrets
   - Stop/restart PM2 process: `aodai-api`
   - Cleanup old backups (keeps last 3)

### Required Secrets

Each server has its own prefixed secrets:

| Secret | Purpose |
|--------|---------|
| `{HK1,US1}_PRODUCTION_SERVER_HOST` | Server IP or hostname |
| `{HK1,US1}_PRODUCTION_SERVER_PORT` | SSH port |
| `{HK1,US1}_PRODUCTION_SERVER_USER` | SSH username |
| `{HK1,US1}_PRODUCTION_SERVER_SSH_KEY` | SSH private key |
| `{HK1,US1}_PUBLIC_BACKEND_DOMAIN` | Backend API URL (embedded in frontend at build time) |
| `{HK1,US1}_PUBLIC_GOOGLE_CLIENT_ID` | Google OAuth client ID (embedded in frontend at build time) |
| `{HK1,US1}_POSTGRESQL_CONNECTION_STRING` | Database connection string |
| `{HK1,US1}_ENABLE_CLOUDFLARE_TUNNEL` | Set to `true` to use Cloudflare Tunnel |
| `{HK1,US1}_CLOUDFLARE_TUNNEL_HOST` | Cloudflare Tunnel hostname |
| `{HK1,US1}_CF_ACCESS_CLIENT_ID` | Cloudflare Access service token ID |
| `{HK1,US1}_CF_ACCESS_CLIENT_SECRET` | Cloudflare Access service token secret |

## For AI Agents

- Two frontend builds produce separate artifacts with different domain configs
- One shared backend build (no env-specific compilation)
- PM2 process name: `aodai-api`
- Production paths: `/root/AoDaiNhaUyen/{frontend,backend}`
- Remote commands use `ssh ... 'bash -se' << 'REMOTE_SCRIPT'`
- All bash steps use `set -euo pipefail`
