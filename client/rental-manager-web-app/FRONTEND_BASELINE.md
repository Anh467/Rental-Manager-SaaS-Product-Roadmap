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
| `components/common` | generic application and responsive patterns |
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
14. Provide `renderMobileCard` for every data-heavy list.
15. Verify long text, empty state, loading state, error state and actions on all baseline viewports.
16. Run route generation, type-check and build.

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
- Use `FormGrid`; never hard-code feature-specific grid breakpoints for standard forms.
- Use `FormActions`; mobile actions must be full width and desktop actions may be inline.
- Use `stickyOnMobile` only for long forms where the submit action must remain reachable.
- Use `optionalText(label, max)` so max-length errors are localized.
- Nested API paths must normalize every segment (`Owner.FirstName` → `owner.firstName`).

## 8. Responsive rule

The baseline is mobile-first. Feature pages should compose common components instead of adding independent media-query behavior.

### Viewport matrix

| Width | Baseline device | Expected behavior |
| ---: | --- | --- |
| 320px | small phone | no horizontal page overflow; full-width actions; readable cards |
| 375–430px | common phone | mobile drawer, card list, touch targets |
| 768px | tablet portrait | table or card transition remains stable |
| 1024px | tablet landscape/small laptop | desktop sidebar and table layout |
| 1440px | desktop | standard content density |
| 1920px | wide desktop | content remains bounded by `max-w-screen-2xl` |

### Required common patterns

- `AppShell` owns mobile navigation and desktop sidebar behavior.
- `PageContent` owns responsive page padding and safe-area spacing.
- `PageHeader` owns action stacking and long-title wrapping.
- `PageToolbar` owns search/filter wrapping.
- `DataTable` uses `renderMobileCard` below `md`; its table remains horizontally scrollable as a fallback.
- `MobileDataCard` owns label/value layout on phone screens.
- Dialogs and sheets must use `100dvh`-bounded height and internal scrolling.
- Inputs, selects and textareas use at least `16px` text on mobile to prevent browser zoom.
- Interactive controls use touch-friendly targets close to 44px on mobile.
- Long user/API text must use `break-words`; identifiers may use controlled horizontal scrolling when breaking is unsafe.
- Never use a fixed pixel width without a mobile `w-full`, `max-w-*` or overflow fallback.
- Respect safe-area insets and `prefers-reduced-motion`.

### Mobile list example

```tsx
<DataTable
  data={query.data?.items ?? []}
  columns={columns}
  renderMobileCard={(contract) => (
    <MobileDataCard
      title={contract.code}
      subtitle={contract.renterName}
      status={<ContractStatus status={contract.status} />}
      fields={[
        { label: t("fields.startDate"), value: formatDate(contract.startDate) },
        { label: t("fields.monthlyRent"), value: formatMoney(contract.monthlyRent) },
      ]}
    />
  )}
/>
```

## 9. Multi-tenant rule

Never reuse cached data after changing Organization.

```ts
await setOrganizationId(nextOrganizationId);
```

The helper clears Query cache and invalidates the Router.

## 10. Required checks

```bash
npm run routes:generate
npm run type-check
npm run build
```

Before merging a UI baseline change, manually inspect at 320, 390, 768, 1024, 1440 and 1920 pixels. Check normal, loading, empty, error, long-text and open-dialog states.
