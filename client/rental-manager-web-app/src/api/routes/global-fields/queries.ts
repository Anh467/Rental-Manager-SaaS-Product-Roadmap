import { createQueryKeys, type inferQueryKeys } from "@lukemorales/query-key-factory";
import { keepPreviousData, queryOptions } from "@tanstack/react-query";

import { getGlobalField, getGlobalFields } from "./requests";
import type { GetGlobalFieldsRequest } from "./types";

export const globalFieldsQueryStore = createQueryKeys("globalFields", {
  list: (query: GetGlobalFieldsRequest) => ({
    queryKey: [{ query }],
    queryFn: async ({ signal }) => (await getGlobalFields({ query, signal })).data,
  }),
  detail: (fieldId: string) => ({
    queryKey: [fieldId],
    queryFn: async ({ signal }) => (await getGlobalField({ fieldId }, { signal })).data,
  }),
});

export type GlobalFieldsQueryKeys = inferQueryKeys<typeof globalFieldsQueryStore>;

export const globalFieldQueries = {
  list: (params: GetGlobalFieldsRequest) => queryOptions({
    ...globalFieldsQueryStore.list(params),
    placeholderData: keepPreviousData,
  }),
  detail: (fieldId: string) => queryOptions({
    ...globalFieldsQueryStore.detail(fieldId),
    enabled: Boolean(fieldId),
  }),
};
