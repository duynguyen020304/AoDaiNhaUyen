<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-08 | Updated: 2026-06-08 -->

# AoDaiNhaUyen.Mcp

## Purpose
Standalone ASP.NET Core MCP service exposing admin tools for agents. Separate host from `AoDaiNhaUyen.Api`; shares Application/Infrastructure contracts and admin services.

## Key Files
| File | Description |
|------|-------------|
| `Program.cs` | MCP host entry: loads config, auth, DI, tool registration |
| `AoDaiNhaUyen.Mcp.csproj` | MCP project refs + packages |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Auth/` | MCP auth helpers/policies |
| `Tools/` | Admin tool classes: product/category/user/dashboard/tool JSON |

## For AI Agents
### Working Here
- Tool classes are integration boundary for agent/admin actions; keep inputs/outputs explicit and stable.
- Reuse Application interfaces/services; do not duplicate business logic in tools.
- Admin actions must preserve backend envelope/error semantics where exposed to clients.
- Treat tool args as untrusted input: validate IDs, paging, filters, role/risk flags.
- Keep JSON helpers centralized in `Tools/ToolJson.cs`.

### Code Map
- `Tools/AdminProductTools.cs` → product CRUD/search/admin catalog operations.
- `Tools/AdminCategoryTools.cs` → category management.
- `Tools/AdminUserTools.cs` → user/admin role operations.
- `Tools/AdminDashboardTools.cs` → dashboard/reporting reads.
- `Tools/ToolJson.cs` → shared JSON serialization/result helpers.

### Commands
- Build from `backend/`: `dotnet build AoDaiNhaUyen.slnx`
- Targeted build: `dotnet build AoDaiNhaUyen.Mcp/AoDaiNhaUyen.Mcp.csproj`
- Tests touching tools: `cd backend/AoDaiNhaUyen.Tests && dotnet test`

### Gotchas
- This project is not the public REST API; REST endpoints live in `AoDaiNhaUyen.Api/Controllers/`.
- Security-sensitive: admin/user/category/product tools can mutate persisted data.
- Do not hardcode secrets; use same config/env pattern as backend host.
