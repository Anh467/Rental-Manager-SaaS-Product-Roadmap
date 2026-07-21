import { createQueryKeys, type inferQueryKeys } from "@lukemorales/query-key-factory";
import { keepPreviousData, queryOptions } from "@tanstack/react-query";

import { getProperties, getProperty } from "./requests";
import type { GetPropertiesRequest } from "./types";

export const propertiesQueryStore = createQueryKeys("properties", {
  list: (query: GetPropertiesRequest) => ({
    queryKey: [{ query }],
    queryFn: async ({ signal }) => (await getProperties({ query, signal })).data,
  }),
  detail: (propertyId: string) => ({
    queryKey: [propertyId],
    queryFn: async ({ signal }) => (await getProperty({ propertyId }, { signal })).data,
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
