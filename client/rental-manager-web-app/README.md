# Rental Manager Web App

Reusable frontend baseline for the multi-tenant Rental Manager SaaS.

## Stack

- React, TypeScript and Vite
- shadcn/ui primitives
- React Hook Form and Zod
- TanStack Query, Router and Table
- Axios with reusable typed clients
- `@lukemorales/query-key-factory`
- i18next and react-i18next
- Axios Mock Adapter and Faker

## Run locally

```bash
cd client/rental-manager-web-app
npm install
npm run dev
```

Open `http://localhost:5173`. The root route redirects to `/properties`.

Local development enables the Faker-backed mock API, so `/properties` and `/rooms` work without the .NET backend. After changing `.env` values, restart Vite.

```env
VITE_API_URL=http://localhost:5008
VITE_BFF_URL=http://localhost:5008
VITE_ENABLE_MOCK_API=true
VITE_MOCK_API_DELAY=250
```

Use `VITE_ENABLE_MOCK_API=false` to call the real backend.

## Architecture

```text
src/
├── api/
│   ├── client/
│   │   ├── factory.ts       # createApiClient and Axios interceptors
│   │   ├── instances.ts     # v1Client and bffClient
│   │   ├── types.ts         # response, error, path and request contracts
│   │   ├── utils.ts         # error normalization and query helpers
│   │   └── index.ts
│   ├── mocks/               # deterministic Faker database and handlers
│   ├── routes/
│   │   └── <resource>/
│   │       ├── types.ts
│   │       ├── requests.ts
│   │       ├── queries.ts
│   │       ├── hooks.ts
│   │       └── index.ts
│   ├── query-client.ts
│   ├── query-store.ts
│   └── tenant-context.ts
├── components/
│   ├── ui/                  # shadcn primitives only
│   ├── form/                # typed RHF/Zod wrappers
│   └── common/              # shell, tables, states, permission and language
├── features/
│   └── <resource>/          # business forms and page composition
├── i18n/
│   ├── index.ts
│   ├── resources.ts
│   └── api-message.ts
├── locales/
│   ├── en/
│   │   ├── common.json
│   │   ├── property.json
│   │   ├── room.json
│   │   ├── success.json
│   │   └── error.json
│   └── vi/                  # same namespaces
├── router/index.tsx
├── routes/                  # TanStack file-based routes
└── routeTree.gen.ts         # generated, never edit manually
```

## API client contract

`api/client` is the only transport layer. Feature code must not create Axios instances.

```ts
const response = await v1Client.get<PageResult<Room>, GetRoomsRequest>(
  "/api/rooms",
  { query, signal },
);

return response.data;
```

The client:

- adds access-token and Organization headers;
- supports `request`, `get`, `post`, `put`, `patch` and `delete`;
- converts network/HTTP failures to `ApiError`;
- keeps request/query/path types reusable;
- supports separate v1 and BFF instances.

## API resource rule

Every business resource uses exactly five public files:

```text
src/api/routes/contracts/
├── types.ts       # request/response/path contracts
├── requests.ts    # endpoint calls only
├── queries.ts     # query-key factory and queryOptions
├── hooks.ts       # useQuery/useMutation and cache invalidation
└── index.ts       # public exports
```

Pages import from `@/api/routes/contracts`; they never import Axios or endpoint URLs.

## Query keys and cache

Each resource owns a query store created by `createQueryKeys`. The root `queryStore` merges resource stores. Mutations update detail cache and invalidate list definitions inside `hooks.ts`.

```ts
export const roomsQueryStore = createQueryKeys("rooms", {
  list: (query: GetRoomsRequest) => ({
    queryKey: [{ query }],
    queryFn: async ({ signal }) => (await getRooms({ query, signal })).data,
  }),
});
```

## File-based Router

Routes live under `src/routes`. A route file owns URL validation, auth/permission checks, loader prefetching and the selected page component.

```tsx
export const Route = createFileRoute(
  "/_authenticated/_standard/(tenant)/rooms/",
)({
  validateSearch: (search) => searchSchema.parse(search),
  loaderDeps: ({ search }) => search,
  loader: ({ context, deps }) =>
    context.queryClient.ensureQueryData(roomQueries.list(deps)),
  component: RoomListPage,
});
```

Use `getRouteApi(routeId)` inside pages for typed search parameters and navigation. Do not edit `routeTree.gen.ts`.

## Mock API

The mock adapter intercepts `v1Client.axios`, so routes, requests and Query hooks are identical in mock and real modes.

- Faker uses fixed seeds.
- Property and Room support list, detail, create, update and delete.
- Search, filters and server pagination are simulated.
- Mock success/error bodies use the same message-key envelope as the backend.
- Unknown endpoints pass through to the real API.

## Message-key and i18n contract

The canonical catalog is Confluence page `08.1 Danh mục Message Key`.

Success:

```json
{
  "success": true,
  "messageKey": "SCS-001",
  "data": {},
  "parameters": { "object": "room" },
  "correlationId": "..."
}
```

Failure:

```json
{
  "success": false,
  "messageKey": "ERR-001",
  "parameters": {},
  "fieldErrors": [
    {
      "fieldKey": "code",
      "messageKey": "ERR-007",
      "parameters": { "object": "room" }
    }
  ],
  "correlationId": "..."
}
```

Rules:

1. Backend returns stable message keys, never localized contract text.
2. `locales/<language>/success.json` owns `SCS-*` messages.
3. `locales/<language>/error.json` owns `ERR-*` messages.
4. Domain UI text stays in files such as `property.json` and `room.json`.
5. Reuse generic keys and parameters; do not create module-specific duplicate codes.
6. `AppForm` maps `fieldErrors` to React Hook Form fields centrally.
7. Nested backend fields such as `Owner.FirstName` normalize to `owner.firstName`.

## Form baseline

Feature forms define schema, defaults and composition only. Labels and validation messages come from i18n.

```tsx
const schema = z.object({
  code: requiredText(t("form.fields.code.label"), 50),
  description: optionalText(t("form.fields.description.label"), 1000),
});

<AppForm schema={schema} defaultValues={defaults} onSubmit={mutation.mutateAsync}>
  {(form) => (
    <TextFormField
      control={form.control}
      name="code"
      label={t("form.fields.code.label")}
      required
    />
  )}
</AppForm>
```

## Multi-tenant cache safety

```ts
await setOrganizationId(nextOrganizationId);
```

This stores the Organization context, clears Query cache and invalidates the Router. Cached data from one Organization must never appear in another.

## Required checks

```bash
npm run routes:generate
npm run type-check
npm run build
```

Client CI runs install, route generation, type-check and build. Do not merge when Client CI is red.
