import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { createProperty, deleteProperty, updateProperty } from "./requests";
import { propertiesQueryStore, propertyQueries } from "./queries";
import type {
  CreatePropertyRequest,
  GetPropertiesRequest,
  UpdatePropertyRequest,
} from "./types";

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
      toast.success("Tạo khu nhà thành công.");
      await queryClient.invalidateQueries({ queryKey: propertiesQueryStore.list._def });
      return response.data;
    },
  });
}

export function useUpdatePropertyMutation(propertyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: UpdatePropertyRequest) =>
      updateProperty({ propertyId }, { payload }),
    onSuccess: async (response) => {
      toast.success("Cập nhật khu nhà thành công.");
      queryClient.setQueryData(
        propertiesQueryStore.detail(propertyId).queryKey,
        response.data,
      );
      await queryClient.invalidateQueries({ queryKey: propertiesQueryStore.list._def });
      return response.data;
    },
  });
}

export function useDeletePropertyMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (propertyId: string) => deleteProperty({ propertyId }),
    onSuccess: async (_response, propertyId) => {
      toast.success("Đã ngừng kích hoạt khu nhà thành công.");
      queryClient.removeQueries({
        queryKey: propertiesQueryStore.detail(propertyId).queryKey,
      });
      await queryClient.invalidateQueries({ queryKey: propertiesQueryStore.list._def });
    },
  });
}
