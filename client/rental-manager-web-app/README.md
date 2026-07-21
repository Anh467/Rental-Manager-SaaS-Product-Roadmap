# Rental Manager Web App

Reusable frontend baseline for the multi-tenant Rental Manager SaaS.

## Stack

- React + TypeScript + Vite
- shadcn/ui primitives
- React Hook Form + Zod
- TanStack Query for server cache
- TanStack Router for typed URL state and route prefetch
- TanStack Table for reusable server-side tables
- Axios for HTTP transport

## Run locally

```bash
cd client/rental-manager-web-app
npm install
npm run dev
npm run type-check
npm run build
```

Open `http://localhost:5173`.

Available baseline pages:

- `/` — reusable form baseline
- `/properties` — Property list with URL pagination/search and query cache
- `/rooms` — Room list business baseline

## Architecture

```text
src/
├── api/
│   ├── client.ts                 # Axios instance, auth and organization headers
│   ├── query-client.ts           # global TanStack Query cache policy
│   ├── query-store.ts            # common query-key factory
│   ├── tenant-context.ts         # clear cache when organization changes
│   ├── types.ts                  # ProblemDetails, pagination contracts
│   └── routes/
│       ├── properties/
│       │   ├── types.ts          # request/response contracts
│       │   ├── requests.ts       # HTTP calls only
│       │   ├── queries.ts        # query keys and queryOptions
│       │   ├── hooks.ts          # query/mutation hooks and invalidation
│       │   └── index.ts          # public API
│       └── rooms/                # same structure as properties
├── components/
│   ├── ui/                       # shadcn primitives only
│   ├── form/                     # common typed form system
│   └── common/                   # app shell, table, page states, permission, dialogs
├── features/
│   ├── properties/               # Property forms and pages
│   └── rooms/                    # Room forms and pages
├── hooks/                        # generic React hooks
├── router.tsx                    # typed routes, search params, loaders
└── main.tsx                      # global providers
```

## Layer rules

1. `components/ui` stays close to shadcn and contains no rental business rules.
2. `components/form` owns labels, errors, disabled states and React Hook Form wiring.
3. `components/common` owns reusable application patterns such as tables and dialogs.
4. `api/routes/<resource>` owns all server-state behavior for one resource.
5. `features/<resource>` owns business forms, columns and page composition.
6. Pages do not call Axios directly.
7. List pagination, filters, sorting and search belong in URL search params.
8. Mutations invalidate or update cache inside `hooks.ts`, never inside pages.
9. Route loaders use `queryClient.ensureQueryData` to reuse cache and prefetch navigation.
10. Changing organization must clear Query cache before rendering the new tenant context.

## API resource baseline

Every resource uses the same five files:

```text
api/routes/contracts/
├── types.ts
├── requests.ts
├── queries.ts
├── hooks.ts
└── index.ts
```

### `types.ts`

```ts
export type Contract = {
  id: string;
  tenantId: string;
  roomId: string;
  startDate: string;
  endDate?: string;
};

export type GetContractsRequest = PageRequest & {
  tenantId?: string;
  roomId?: string;
};
```

### `requests.ts`

```ts
export function getContracts(params: GetContractsRequest, signal?: AbortSignal) {
  return apiRequest<PageResult<Contract>>({
    url: "/api/contracts",
    method: "GET",
    params,
    signal,
  });
}
```

### `queries.ts`

```ts
export const contractKeys = createEntityQueryKeys("contracts");

export const contractQueries = {
  list: (params: GetContractsRequest) =>
    queryOptions({
      queryKey: contractKeys.list(params),
      queryFn: ({ signal }) => getContracts(params, signal),
      placeholderData: keepPreviousData,
    }),
};
```

### `hooks.ts`

```ts
export function useCreateContractMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createContract,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: contractKeys.lists() }),
  });
}
```

## Form baseline

A feature form only defines schema, defaults, field composition and submit request:

```tsx
const roomSchema = z.object({
  propertyId: requiredText("Khu nhà"),
  code: requiredText("Mã phòng", 50),
  monthlyRent: requiredNumber("Giá thuê", 0),
  status: z.enum(["vacant", "occupied", "maintenance", "inactive"]),
  isActive: optionalBoolean,
});

type RoomValues = z.infer<typeof roomSchema>;

<AppForm<RoomValues>
  schema={roomSchema}
  defaultValues={defaults}
  onSubmit={(values) => createRoomMutation.mutateAsync(values)}
>
  {(form) => (
    <>
      <TextFormField control={form.control} name="code" label="Mã phòng" required />
      <MoneyFormField control={form.control} name="monthlyRent" label="Giá thuê" required />
      <FormSubmitButton>Lưu phòng</FormSubmitButton>
    </>
  )}
</AppForm>
```

Included common fields:

- Text, number, date, textarea, select and switch
- Email, phone, password, money and checkbox
- Common Zod schema helpers
- `.NET ProblemDetails` mapping to exact React Hook Form fields
- Root server error rendering
- Loading and submit state management

## URL state baseline

```ts
const searchSchema = z.object({
  page: z.coerce.number().int().positive().catch(1),
  pageSize: z.coerce.number().int().min(10).max(100).catch(20),
  search: z.string().catch(""),
});
```

The URL is the source of truth for list state, so refresh, bookmark and browser back/forward preserve the same page and filters.

## Multi-tenant cache safety

```ts
await setOrganizationId(nextOrganizationId);
```

`setOrganizationId` stores the organization context, clears all Query cache and invalidates the router. This prevents data from the previous organization appearing after a tenant switch.

## Permission baseline

```tsx
<PermissionGuard required="room.create">
  <Button>Thêm phòng</Button>
</PermissionGuard>
```

The UI guard improves UX only. The backend must still enforce every permission.

## Validation and CI

The repository includes `.github/workflows/client-ci.yml`, which runs:

```bash
npm install
npm run type-check
npm run build
```

Do not merge frontend changes when Client CI is red.
