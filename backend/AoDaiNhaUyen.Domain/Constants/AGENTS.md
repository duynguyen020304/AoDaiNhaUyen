<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Constants

## Purpose
Application-wide domain constants that need to be shared across layers without creating circular dependencies. Currently holds role name strings used in authorization policy setup, seed data, and service logic.

## Key Files
| File | Description |
|------|-------------|
| `RoleNames.cs` | Static class: `Admin = "admin"`, `Customer = "customer"` — canonical role name strings |

## For AI Agents
### Working In This Directory
- Add new constant files here when a string/value is referenced in more than one layer (Domain, Application, Infrastructure, Api) and must stay in Domain to avoid circular refs.
- Keep each file focused on a single concern (e.g., one file per constant group).
- Do not add runtime logic, EF references, or service dependencies.

### Common Patterns
- Constants are `public static class` with `public const string` members.
- `RoleNames` values must stay in sync with `DefaultRoles.cs` in `../SeedData/` and the `Role` seed records inserted at startup.
- Authorization policies in `Api/Configuration/` reference `RoleNames` constants.

## Dependencies
### Internal
- Referenced by `AoDaiNhaUyen.Application` interfaces and `AoDaiNhaUyen.Api` authorization setup
- `SeedData/DefaultRoles.cs` uses the same string values (kept in sync manually)
### External
- None

<!-- MANUAL: -->
