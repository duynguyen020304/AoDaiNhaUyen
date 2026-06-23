<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-23 | Updated: 2026-06-23 -->

# .understand-anything

## Purpose
Generated analysis cache for code intelligence tooling. Holds repository knowledge graph, scan metadata, fingerprints, ignore rules, and transient intermediate results. Treat as tooling output, not source of truth.

## Key Files
| File | Description |
|------|-------------|
| `config.json` | Analyzer config |
| `meta.json` | Output language and tool metadata |
| `knowledge-graph.json` | Extracted repository knowledge graph |
| `fingerprints.json` | File fingerprints for incremental analysis |
| `.understandignore` | Exclusion rules for analysis runs |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `intermediate/` | Transient scan output and scratch results (see `intermediate/AGENTS.md`) |

## For AI Agents
### Working In This Directory
- Do not treat cached data as current repo state without verifying source files
- Regenerate instead of hand-editing when tool output changes
- Ignore this directory when assessing product code quality or runtime behavior

### Testing Requirements
- No app tests here
- If config changes, rerun analysis and confirm cache files refresh cleanly

### Common Patterns
- Generated JSON artifacts
- Transient intermediate results
- Tool-specific metadata

## Dependencies
### External
- Understand Anything analysis tooling

<!-- MANUAL: -->