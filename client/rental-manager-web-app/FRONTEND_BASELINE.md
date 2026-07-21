# Frontend baseline checklist

Use this checklist for Property, Room, Renter, Contract, Invoice, Payment and every new business resource.

## 1. Resource folders

```text
src/api/routes/<resource>/
├── types.ts
├── requests.ts
├── queries.ts
├── hooks.ts
└── index.ts

src/features/<resource>/
├── components/
└── pages/

src/routes/_authenticated/_standard/(tenant)/<resource>/
├── index.tsx
└── $resourceId.tsx

src/locales/en/<resource>.json
src/locales/vi/<resource>.json
```

## 2. Layer responsibilities

| Layer | Responsibility |
| --- | --- |
| `api/client` | Axios factories, instances, auth headers and normalized errors |
| `api/routes/*/types.ts` | API request, response and path contracts |
| `api/routes/*/requests.ts` | HTTP calls only |
| `api/routes/*/queries.ts` | query keys and queryOptions |
| `api/routes/*/hooks.ts` | hooks, mutations and cache changes |
| `api/mocks` | Faker data and contract-compatible endpoint handlers |
| `components/ui` | shadcn primitives only |
| `components/form` | reusable RHF/Zod behavior |
| `components/common` | generic application patterns |
| `features/*` | business composition |
| `routes/*` | URL, auth, permissions and loader prefetch |
| `locales/*` | UI and API-message localization |

## 3. New resource procedure

For `contracts`:

1. Copy `api/routes/rooms` to `api/routes/contracts`.
2. Define `Contract`, list params, payloads and `ContractPathParams` in `types.ts`.
3. Call only `v1Client` or `bffClient` in `requests.ts`.
4. Create `contractsQueryStore` with `createQueryKeys`.
5. Add it to the root `queryStore`.
6. Configure invalidation and detail-cache updates in `hooks.ts`.
7. Add deterministic Faker entities and handlers when backend APIs are unavailable.
8. Create `locales/en/contract.json` and `locales/vi/contract.json`.
9. Add the namespace to `i18n/resources.ts`.
10. Build forms only from `components/form` wrappers.
11. Create file-based list/detail routes.
12. Store list search, filters, sorting and pagination in URL search parameters.
13. Add UI permission guards and enforce the same permissions in the backend.
14. Run route generation, type-check and build.

## 4. API message rule

Do not return or depend on localized backend text.

```ts
translateApiMessage(response.messageKey ?? "SCS-001", {
  object: "contract",
  ...response.parameters,
});
```

Use only the canonical Confluence codes:

- `SCS-001` create
- `SCS-002` update
- `SCS-003` deactivate
- `SCS-005` retrieve
- `ERR-001` validation
- `ERR-002` not found
- `ERR-004` permission
- `ERR-007` duplicate
- `ERR-017` active dependency
- `ERR-049` service unavailable
- `ERR-050` unexpected error

Prefer parameters such as `object`, `action`, `field`, `dependency`, `service`, `currentState` and `requestedState` instead of adding module-specific codes.

## 5. Mock rule

Mocks must return the same envelope and status codes as the real backend. A page, query hook or form must not know whether mocks are enabled.

```env
VITE_ENABLE_MOCK_API=true
VITE_MOCK_API_DELAY=250
```

Use fixed Faker seeds so screenshots and tests remain repeatable.

## 6. Router rule

- Use `createFileRoute`.
- Use pathless layouts for authentication and standard shell.
- Use route groups such as `(tenant)` for organization without adding a URL segment.
- Use Zod for `validateSearch`.
- Use `ensureQueryData` in loaders.
- Use `getRouteApi` in pages.
- Never edit `routeTree.gen.ts`.

## 7. Form rule

- Schema owns validation.
- Translation owns labels and messages.
- Field wrappers own RHF wiring and error rendering.
- `AppForm` owns submit and server-error mapping.
- Use `optionalText(label, max)` so max-length errors are localized.
- Nested API paths must normalize every segment (`Owner.FirstName` → `owner.firstName`).

## 8. Multi-tenant rule

Never reuse cached data after changing Organization.

```ts
await setOrganizationId(nextOrganizationId);
```

The helper clears Query cache and invalidates the Router.

## 9. Required checks

```bash
npm run routes:generate
npm run type-check
npm run build
```
