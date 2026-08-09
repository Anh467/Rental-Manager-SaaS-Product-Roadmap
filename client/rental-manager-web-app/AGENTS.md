# Frontend Instructions

These instructions extend the repository-root `AGENTS.md` for
`client/rental-manager-web-app/**`.

## Current stack and baseline

- React 18, TypeScript, Vite, TanStack Router, TanStack Query, React Hook Form, Zod, i18next,
  Tailwind CSS, shadcn/Radix UI, Vitest, and Playwright.
- Treat `FRONTEND_BASELINE.md` as the detailed implementation checklist. Reuse its resource folder,
  responsive, form, router, API, mock, and localization patterns.
- Keep strict TypeScript. Do not use `any`, non-null assertions, or type casts to hide an unresolved
  contract problem.
- Search `src/components`, `src/api`, `src/hooks`, `src/lib`, and an existing analogous feature before
  adding a component/helper/hook.

## Layer ownership

- `src/api/client`: Axios instances/factories, auth and organization headers, response validation,
  and normalized transport errors.
- `src/api/routes/<resource>/types.ts`: request, response, list, and path contracts.
- `requests.ts`: HTTP calls only; use the existing `v1Client`/`bffClient` boundary.
- `queries.ts`: stable query keys and `queryOptions`.
- `hooks.ts`: query/mutation hooks, invalidation, and targeted cache updates.
- `src/api/mocks`: deterministic, contract-compatible handlers and data.
- `src/components/ui`: shadcn/Radix primitives only.
- `src/components/form`: reusable React Hook Form/Zod behavior.
- `src/components/common`: generic app and responsive patterns.
- `src/features`: business composition, feature components, and feature pages.
- `src/routes`: URL structure, authentication/permission routing, search validation, and loader
  prefetch. Never edit generated `routeTree.gen.ts`.
- `src/locales`: UI and API-message localization in both Vietnamese and English.

Do not bypass these layers by placing Axios calls directly in pages/components, creating feature-local
copies of common controls, or putting business feature code into UI primitives.

## API contract handling

- Use the one canonical API client and envelope types. For JSON envelopes, `messageKey` and
  `correlationId` are required, not optional convenience fields.
- Runtime-validate the JSON envelope. Do not turn arbitrary JSON into a successful envelope and do
  not fabricate a success key when the backend violates the contract.
- Handle `204` as no content without JSON parsing. Handle successful file/blob responses separately;
  parse the canonical JSON error envelope when a file request fails.
- Translate `messageKey` plus safe `parameters` on the client. Never display or depend on localized
  backend text and never show raw provider/SQL/server error content.
- Keep the TypeScript catalog and `src/locales/vi`/`src/locales/en` synchronized with the canonical
  Active/Deprecated catalog. New code must not use Deprecated keys.
- Add contract tests for envelope shape, catalog/locale parity, `204`, blob/file handling, and invalid
  backend responses whenever these areas change.

## Query, router, and organization state

- Use `createQueryKeys` and register new resource query stores in the existing root query store.
- Use file routes, Zod `validateSearch`, loaders with `ensureQueryData`, and `getRouteApi` in pages.
- Put shareable list search, filters, sorting, and pagination in URL search parameters.
- Keep server state in TanStack Query; do not duplicate it in component/global state without a clear
  non-server-state reason.
- On Organization change, use the existing organization helper that clears the Query cache and
  invalidates the Router. Never display cached data from the previous Organization.
- Client OrganizationId is only a selection hint. Do not derive or grant Role/Permission on the
  client. UI permission guards improve UX; the backend remains authoritative.

## Forms, localization, and UI reuse

- Zod schemas own validation; locale resources own labels and messages; shared field wrappers own
  React Hook Form wiring and error rendering; `AppForm` owns submit/server-field-error mapping.
- Use existing `FormGrid`, `FormActions`, page shell/header/toolbar, data table, mobile card, dialog,
  and sheet patterns. Do not create feature-specific substitutes with the same responsibility.
- Normalize nested API field paths consistently when mapping server errors to form fields.
- Add every user-facing string to both `vi` and `en` namespaces. Do not hard-code business UI text in
  components or tests.
- Keep loading, empty, error, success, disabled, unauthorized, and long-content states intentional.

## Responsive and accessible behavior

- Build mobile-first and preserve the baseline at `320`, `390`, `768`, `1024`, `1440`, and `1920`
  pixels.
- No horizontal page overflow at phone widths. Actions stack/full-width on mobile where appropriate;
  data-heavy lists use the existing mobile-card fallback.
- Reuse `AppShell`, `PageContent`, `PageHeader`, `PageToolbar`, `DataTable`, and `MobileDataCard` rather
  than inventing independent breakpoint behavior.
- Dialogs/sheets use viewport-bounded height and internal scrolling. Inputs use mobile-safe text
  sizing; interactive controls have touch-friendly targets; long content wraps safely.
- Preserve keyboard navigation, visible focus, semantic labels, accessible error messages, reduced
  motion, safe-area insets, and sufficient contrast.
- Manually inspect normal, loading, empty, error, long-text, validation, and open-overlay states when a
  UI surface changes.

## Mocks and tests

- A page, hook, or form must not know whether mock mode is enabled. Mock handlers use the same URL,
  envelope, status codes, permissions, validation errors, and pagination shape as the real backend.
- Use fixed Faker seeds and deterministic data so tests and screenshots are repeatable.
- Add focused Vitest coverage for types/helpers/hooks/components and Playwright coverage for critical
  responsive/user flows. Do not replace behavioral assertions with snapshots alone.
- Never loosen types, catch and ignore API failures, or remove assertions to make tests pass.

## Required frontend checks

Run targeted tests while developing, then run the CI-equivalent commands from this directory:

```bash
npm ci
npm run type-check
npm run test:unit
npm run build
npm run test:responsive
npm audit
npm audit --omit=dev
```

`type-check` and `build` generate routes through the configured scripts. Do not edit generated route
output manually. If Playwright browsers or another required runtime are unavailable, report the exact
blocked command instead of claiming the responsive suite passed.
