import { createQueryKeys, type inferQueryKeys } from "@lukemorales/query-key-factory";
import { queryOptions } from "@tanstack/react-query";

import { getGlobalFieldTypes } from "./requests";

export const globalFieldTypesQueryStore = createQueryKeys("globalFieldTypes", {
  list: {
    queryKey: null,
    queryFn: async ({ signal }) => (await getGlobalFieldTypes({ signal })).data,
  },
});

export type GlobalFieldTypesQueryKeys = inferQueryKeys<typeof globalFieldTypesQueryStore>;

export const globalFieldTypeQueries = {
  list: () =>
    queryOptions({
      ...globalFieldTypesQueryStore.list,
      staleTime: 5 * 60_000,
    }),
};
