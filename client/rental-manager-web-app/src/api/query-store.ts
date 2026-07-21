export function createEntityQueryKeys<TEntity extends string>(entity: TEntity) {
  const root = ["rental-manager", entity] as const;

  return {
    all: root,
    lists: () => [...root, "list"] as const,
    list: <TParams>(params: TParams) => [...root, "list", params] as const,
    details: () => [...root, "detail"] as const,
    detail: (id: string) => [...root, "detail", id] as const,
    lookups: () => [...root, "lookup"] as const,
  };
}
