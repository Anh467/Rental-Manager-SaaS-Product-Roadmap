import { keepPreviousData, queryOptions } from "@tanstack/react-query";

import { createEntityQueryKeys } from "@/api/query-store";
import { getProperties, getProperty } from "@/api/routes/properties/requests";
import type { GetPropertiesRequest } from "@/api/routes/properties/types";

export const propertyKeys = createEntityQueryKeys("properties");

export const propertyQueries = {
  list: (params: GetPropertiesRequest) =>
    queryOptions({
      queryKey: propertyKeys.list(params),
      queryFn: ({ signal }) => getProperties(params, signal),
      placeholderData: keepPreviousData,
    }),
  detail: (propertyId: string) =>
    queryOptions({
      queryKey: propertyKeys.detail(propertyId),
      queryFn: ({ signal }) => getProperty(propertyId, signal),
      enabled: Boolean(propertyId),
    }),
};
