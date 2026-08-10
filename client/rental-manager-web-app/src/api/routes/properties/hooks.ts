import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { mergeMessageParameters, requireResponseData } from "@/api/client";
import { translateApiMessage } from "@/i18n/api-message";
import { createProperty, deleteProperty, updateProperty } from "./requests";
import { propertiesQueryStore, propertyQueries } from "./queries";
import type { CreatePropertyRequest, GetPropertiesRequest, UpdatePropertyRequest } from "./types";

export function usePropertiesQuery(params: GetPropertiesRequest) {
  return useQuery(propertyQueries.list(params));
}

export function usePropertyQuery(propertyId: string) {
  return useQuery(propertyQueries.detail(propertyId));
}

export function useCreatePropertyMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreatePropertyRequest) => createProperty({ payload }),
    onSuccess: async (response) => {
      toast.success(translateApiMessage(
        response.messageKey ?? "SCS-001",
        mergeMessageParameters({ object: "property" }, response.parameters),
      ));
      await queryClient.invalidateQueries({ queryKey: propertiesQueryStore.list._def });
      return requireResponseData(response);
    },
  });
}

export function useUpdatePropertyMutation(propertyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: UpdatePropertyRequest) => updateProperty({ propertyId }, { payload }),
    onSuccess: async (response) => {
      toast.success(translateApiMessage(
        response.messageKey ?? "SCS-002",
        mergeMessageParameters({ object: "property" }, response.parameters),
      ));
      const property = requireResponseData(response);
      queryClient.setQueryData(propertiesQueryStore.detail(propertyId).queryKey, property);
      await queryClient.invalidateQueries({ queryKey: propertiesQueryStore.list._def });
      return property;
    },
  });
}

export function useDeletePropertyMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (propertyId: string) => deleteProperty({ propertyId }),
    onSuccess: async (response, propertyId) => {
      toast.success(translateApiMessage(
        response.messageKey ?? "SCS-003",
        mergeMessageParameters({ object: "property" }, response.parameters),
      ));
      queryClient.removeQueries({ queryKey: propertiesQueryStore.detail(propertyId).queryKey });
      await queryClient.invalidateQueries({ queryKey: propertiesQueryStore.list._def });
    },
  });
}
