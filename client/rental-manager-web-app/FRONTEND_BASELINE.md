# Frontend baseline

## Layers

```text
src/
├── api/
│   ├── client.ts            # Axios, auth header, organization header, error normalization
│   ├── query-client.ts      # global cache policy and query key factory
│   └── types.ts             # ProblemDetails, pagination and API contracts
├── components/
│   ├── ui/                  # shadcn primitives only
│   ├── form/                # AppForm and typed field wrappers
│   └── common/              # DataTable, page states, permission guard
├── features/
│   └── properties/
│       ├── api.ts           # queryOptions, mutationOptions and cache invalidation
│       ├── components/      # feature forms
│       └── pages/           # route pages
└── router.tsx               # typed URL, loaders and route prefetch
```

## Rules for every feature

1. Put HTTP requests and TanStack Query hooks in `features/<feature>/api.ts`.
2. Use a query-key factory. Do not write ad-hoc string query keys in pages.
3. List filters, pagination and sorting belong in the URL search params.
4. Route loaders call `queryClient.ensureQueryData` so hover/navigation can prefetch.
5. Mutations update detail cache and invalidate list caches in one shared hook.
6. Pages compose common components and do not implement loading, empty or error markup again.
7. Forms own only schema, defaults and submit payload. Common form behavior stays in `components/form`.
8. `components/ui` must remain replaceable shadcn primitives without business rules.

## API route baseline

```ts
export const roomQueries = {
  list: (params: PageRequest) =>
    queryOptions({
      queryKey: queryKeys.rooms.list(params),
      queryFn: () => apiRequest<PageResult<Room>>({
        url: "/api/rooms",
        method: "GET",
        params,
      }),
      placeholderData: (previous) => previous,
    }),
};

export function useCreateRoom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: RoomFormValues) =>
      apiRequest<Room>({ url: "/api/rooms", method: "POST", data: payload }),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: queryKeys.rooms.all() }),
  });
}
```

## URL state baseline

```ts
const searchSchema = z.object({
  page: z.coerce.number().int().positive().catch(1),
  pageSize: z.coerce.number().int().min(10).max(100).catch(20),
  search: z.string().catch(""),
});
```

The URL becomes the source of truth for list state. Refreshing, bookmarking and browser back/forward retain the same filters and page.

## Multi-tenant rule

`apiClient` automatically sends `X-Organization-Id`. When organization changes:

```ts
localStorage.setItem("organization_id", organizationId);
queryClient.clear();
router.invalidate();
```

Clearing the client cache prevents data from the previous organization appearing in the new organization context.

## Common components included

- `AppForm`
- typed text, number, date, textarea, select and switch fields
- common Zod schema helpers
- .NET `ProblemDetails` field mapping
- `PageHeader` and `PageContent`
- `LoadingState`, `EmptyState`, `ErrorState`
- generic server-side `DataTable`
- `PermissionGuard`
- TanStack Query client and key factory
- TanStack Router typed search params and loaders
- property list/form/API examples as the feature baseline
