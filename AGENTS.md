# Repository Guidelines

## Project Structure & Module Organization

This repository currently contains a single Vite frontend app in `frontend/`.
Source code lives in `frontend/src`, with the React entry points in `main.tsx`
and `App.tsx`. UI sections are organized under `src/components/<SectionName>/`
and usually pair a PascalCase component file with a matching CSS Module, for
example `Header/Header.tsx` and `Header/Header.module.css`. Shared global styles
are in `src/styles/`, including variables, typography, reset, transitions, and
texture styles. Static images and SVG assets are served from
`frontend/public/assets/`.

## Build, Test, and Development Commands

Run commands from `frontend/`:

- `npm install` installs dependencies from `package-lock.json`.
- `npm run dev` starts the Vite development server.
- `npm run build` runs TypeScript build checks and creates the production bundle.
- `npm run lint` runs ESLint across the project.
- `npm run preview` serves the built app locally for final inspection.

`bun.lock` is also tracked. Prefer the package manager already used by the
current branch and avoid changing lockfiles unless dependency updates require it.

## Coding Style & Naming Conventions

Use React with TypeScript and functional components. Name components and folders
in PascalCase, such as `MaterialSection`, and keep section-specific styles in
`*.module.css` beside the component. Keep global design tokens in
`src/styles/variables.css` instead of duplicating constants. Follow the existing
two-space TypeScript indentation and CSS formatting style. The ESLint flat config
uses `@eslint/js`, `typescript-eslint`, `react-hooks`, and `react-refresh`; run
`npm run lint` before submitting changes.

## Testing Guidelines

No dedicated test framework is configured yet. For now, validate changes with
`npm run lint` and `npm run build`, then inspect affected screens in `npm run dev`
or `npm run preview`. If tests are added later, place them near the code they
cover and use clear names such as `ComponentName.test.tsx`.

For tasks that change UI, layout, styling, animations, routing, or user-facing
browser behavior, use Playwright MCP to inspect and validate the affected flows
in the browser before considering the task complete. Capture screenshots or note
the validated viewport/flow when the change is visual.

For tasks that involve persisted data, APIs, migrations, seed data, or backend
data handling, validate the relevant backend behavior and use `psql` to inspect
the PostgreSQL database state directly. Prefer targeted `psql` queries that
confirm the specific tables, rows, constraints, or relationships affected by the
task.

## API Response Guidelines

All backend API responses must use the standard envelope below. Do not return raw
anonymous objects such as `Ok(new { data })`, and do not return errors as
`{ error: ... }`.

```json
{
  "success": true,
  "message": "Lấy dữ liệu thành công",
  "data": {
    "id": 1,
    "name": "Sản phẩm A"
  },
  "errors": null,
  "timestamp": "2026-04-17T15:33:00Z"
}
```

Use `success: true`, a Vietnamese success message, populated `data`,
`errors: null`, and a UTC ISO-8601 `timestamp` for successful responses. For
failed responses, use `success: false`, `data: null`, and put error details in
`errors`, for example `{ "code": "not_found", "message": "Product not found." }`.
For paginated data, keep pagination metadata inside `data.meta`, not beside
`data`.

## Commit & Pull Request Guidelines

Recent history uses short imperative messages and occasional Conventional Commit
scopes, for example `feat(material-section): add interactive material selector`.
Keep commits focused and describe the user-visible change. Pull requests should
include a concise summary, validation steps run, linked issues when applicable,
and screenshots or screen recordings for visual changes.

Commit only complete, validated work. Use short imperative commit messages,
optionally with a Conventional Commit scope when it clarifies the affected area.
Before committing, review the diff, avoid including unrelated changes, and make
sure generated files, lockfiles, or large assets are included only when they are
necessary for the task.

## Security & Configuration Tips

Do not commit `.env` files, local editor state, build output, or `node_modules/`.
Keep new assets under `frontend/public/assets/`, use descriptive lowercase names,
and verify large media files are necessary before adding them.
