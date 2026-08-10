import { createQueryKeys, type inferQueryKeys } from "@lukemorales/query-key-factory";
import { keepPreviousData, queryOptions } from "@tanstack/react-query";

import { requireResponseData } from "@/api/client";

import { getProperties, getProperty } from "./requests";
import type { GetPropertiesRequest } from "./types";

export const propertiesQueryStore = createQueryKeys("properties", {
  list: (query: GetPropertiesRequest) => ({
    queryKey: [{ query }],
    queryFn: async ({ signal }) => requireResponseData(await getProperties({ query, signal })),
  }),
  detail: (propertyId: string) => ({
    queryKey: [propertyId],
    queryFn: async ({ signal }) => requireResponseData(await getProperty({ propertyId }, { signal })),
  }),
});

export type PropertiesQueryKeys = inferQueryKeys<typeof propertiesQueryStore>;

export const propertyQueries = {
  list: (params: GetPropertiesRequest) =>
    queryOptions({
      ...propertiesQueryStore.list(params),
      placeholderData: keepPreviousData,
    }),
  detail: (propertyId: string) =>
    queryOptions({
      ...propertiesQueryStore.detail(propertyId),
      enabled: Boolean(propertyId),
    }),
};
