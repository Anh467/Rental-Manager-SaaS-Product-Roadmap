import { createQueryKeys, type inferQueryKeys } from "@lukemorales/query-key-factory";
import { keepPreviousData, queryOptions } from "@tanstack/react-query";

import { requireResponseData } from "@/api/client";

import { getGlobalField, getGlobalFields } from "./requests";
import type { GetGlobalFieldsRequest } from "./types";

export const globalFieldsQueryStore = createQueryKeys("globalFields", {
  list: (query: GetGlobalFieldsRequest) => ({
    queryKey: [{ query }],
    queryFn: async ({ signal }) => requireResponseData(await getGlobalFields({ query, signal })),
  }),
  detail: (fieldId: string) => ({
    queryKey: [fieldId],
    queryFn: async ({ signal }) => requireResponseData(await getGlobalField({ fieldId }, { signal })),
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
