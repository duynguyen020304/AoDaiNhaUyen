<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-23 | Updated: 2026-06-23 -->

# intermediate

## Purpose
Transient scratch space for Understand Anything analysis runs. Contains temporary scan results that can be regenerated at any time.

## Key Files
| File | Description |
|------|-------------|
| `scan-result.json` | Intermediate scan output from analysis tooling |

## For AI Agents
### Working In This Directory
- Treat contents as disposable cache
- Regenerate rather than manually edit
- Do not use as evidence for current source behavior

### Testing Requirements
- No app tests here
- If files change, confirm analyzer rerun produces expected output

### Common Patterns
- Single-purpose generated JSON artifacts

## Dependencies
### External
- Understand Anything analysis tooling

<!-- MANUAL: -->