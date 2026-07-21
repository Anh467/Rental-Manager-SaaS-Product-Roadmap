import {
  mutationOptions,
  queryOptions,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";

import { apiRequest } from "@/api/client";
import { queryKeys } from "@/api/query-client";
import type { PageRequest, PageResult } from "@/api/types";
import type { PropertyFormValues } from "@/features/properties/components/property-form";

export type Property = PropertyFormValues & { id: string };

export const propertyQueries = {
  list: (params: PageRequest) =>
    queryOptions({
      queryKey: queryKeys.properties.list(params),
      queryFn: () =>
        apiRequest<PageResult<Property>>({
          url: "/api/properties",
          method: "GET",
          params,
        }),
      placeholderData: (previous) => previous,
    }),
  detail: (id: string) =>
    queryOptions({
      queryKey: queryKeys.properties.detail(id),
      queryFn: () =>
        apiRequest<Property>({ url: `/api/properties/${id}`, method: "GET" }),
      enabled: Boolean(id),
    }),
};

export function useCreateProperty() {
  const queryClient = useQueryClient();
  return useMutation(
    mutationOptions({
      mutationFn: (payload: PropertyFormValues) =>
        apiRequest<Property>({ url: "/api/properties", method: "POST", data: payload }),
      onSuccess: async () => {
        await queryClient.invalidateQueries({ queryKey: queryKeys.properties.lists() });
      },
    }),
  );
}

export function useUpdateProperty(id: string) {
  const queryClient = useQueryClient();
  return useMutation(
    mutationOptions({
      mutationFn: (payload: PropertyFormValues) =>
        apiRequest<Property>({ url: `/api/properties/${id}`, method: "PUT", data: payload }),
      onSuccess: async (property) => {
        queryClient.setQueryData(queryKeys.properties.detail(id), property);
        await queryClient.invalidateQueries({ queryKey: queryKeys.properties.lists() });
      },
    }),
  );
}
