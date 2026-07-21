# Frontend baseline

This document is the copy/reference checklist for every new Rental Manager feature.

## Required resource structure

```text
src/api/routes/<resource>/
├── types.ts       # API request and response contracts
├── requests.ts    # Axios calls only
├── queries.ts     # query keys and queryOptions
├── hooks.ts       # useQuery/useMutation and cache invalidation
└── index.ts       # public exports

src/features/<resource>/
├── components/    # form, status widget, business-specific UI
└── pages/         # page composition only
```

## Responsibilities

| Layer | Allowed responsibility |
| --- | --- |
| `components/ui` | shadcn primitives |
| `components/form` | React Hook Form wiring, labels, validation rendering |
| `components/common` | generic application patterns |
| `api/client.ts` | HTTP transport and headers |
| `api/routes/*/requests.ts` | endpoint calls |
| `api/routes/*/queries.ts` | query keys and cache definitions |
| `api/routes/*/hooks.ts` | mutations, invalidation and cache updates |
| `features/*` | rental business composition |
| `router.tsx` | URL schema, route loader and navigation |

## List page flow

```text
URL search params
      ↓
TanStack Router validateSearch
      ↓
route loader ensureQueryData
      ↓
resource queryOptions
      ↓
request function
      ↓
Axios client
      ↓
.NET API
```

The page consumes a resource hook and common components. It must not call Axios directly.

## Mutation flow

```text
Feature form
   ↓ mutateAsync(values)
resource mutation hook
   ↓
request function
   ↓
update detail cache + invalidate list cache
```

## New feature checklist

For a new resource such as `contracts`:

1. Copy `api/routes/rooms` to `api/routes/contracts`.
2. Replace contracts in `types.ts`.
3. Replace URLs in `requests.ts`.
4. Keep the query key factory pattern in `queries.ts`.
5. Configure invalidation in `hooks.ts`.
6. Create `ContractForm` using fields from `components/form`.
7. Create the page using `PageHeader`, `SearchInput`, `DataTable` and page states.
8. Add a TanStack Router search schema and route loader.
9. Add required permission keys to `PermissionGuard`.
10. Run `npm run type-check` and `npm run build`.

## Common components

### Form

- `AppForm`
- `TextFormField`
- `NumberFormField`
- `DateFormField`
- `TextareaFormField`
- `SelectFormField`
- `SwitchFormField`
- `EmailFormField`
- `PhoneFormField`
- `PasswordFormField`
- `MoneyFormField`
- `CheckboxFormField`
- `FormGrid`, `FormSection`, `FormActions`, `FormSubmitButton`

### Application

- `AppShell`
- `PageHeader`, `PageContent`
- `LoadingState`, `EmptyState`, `ErrorState`, `AsyncState`
- `SearchInput`
- `DataTable`
- `ConfirmDialog`
- `StatusBadge`
- `PermissionProvider`, `PermissionGuard`

### shadcn primitives

- Alert and AlertDialog
- Badge and Button
- Card
- Checkbox
- Dialog
- DropdownMenu
- Form, Input, Label, Select, Switch and Textarea
- Separator, Skeleton, Table, Tabs and Tooltip

## Multi-tenant rule

Never reuse cached data after an organization switch.

```ts
await setOrganizationId(nextOrganizationId);
```

The helper clears Query cache and invalidates the Router before the new organization view is rendered.

## Permission rule

`PermissionGuard` only controls visibility and UX. Every API endpoint must independently enforce its permission.

## Validation rule

Feature schemas own validation. Components render errors but do not duplicate business validation rules.

Server validation follows `.NET ProblemDetails` and is mapped centrally by `AppForm`.
