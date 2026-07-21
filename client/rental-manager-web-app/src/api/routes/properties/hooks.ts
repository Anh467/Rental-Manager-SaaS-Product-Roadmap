import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import {
  createProperty,
  deleteProperty,
  updateProperty,
} from "@/api/routes/properties/requests";
import { propertyKeys, propertyQueries } from "@/api/routes/properties/queries";
import type {
  CreatePropertyRequest,
  GetPropertiesRequest,
  UpdatePropertyRequest,
} from "@/api/routes/properties/types";

export function usePropertiesQuery(params: GetPropertiesRequest) {
  return useQuery(propertyQueries.list(params));
}

export function usePropertyQuery(propertyId: string) {
  return useQuery(propertyQueries.detail(propertyId));
}

export function useCreatePropertyMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreatePropertyRequest) => createProperty(payload),
    onSuccess: async () => {
      toast.success("Tạo khu nhà thành công.");
      await queryClient.invalidateQueries({ queryKey: propertyKeys.lists() });
    },
  });
}

export function useUpdatePropertyMutation(propertyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: UpdatePropertyRequest) => updateProperty(propertyId, payload),
    onSuccess: async (property) => {
      toast.success("Cập nhật khu nhà thành công.");
      queryClient.setQueryData(propertyKeys.detail(propertyId), property);
      await queryClient.invalidateQueries({ queryKey: propertyKeys.lists() });
    },
  });
}

export function useDeletePropertyMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (propertyId: string) => deleteProperty(propertyId),
    onSuccess: async (_result, propertyId) => {
      toast.success("Xóa khu nhà thành công.");
      queryClient.removeQueries({ queryKey: propertyKeys.detail(propertyId) });
      await queryClient.invalidateQueries({ queryKey: propertyKeys.lists() });
    },
  });
}
