import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { mergeMessageParameters, requireResponseData } from "@/api/client";
import { translateApiMessage } from "@/i18n/api-message";
import {
  changeGlobalFieldStatus,
  createGlobalField,
  deleteGlobalField,
  updateGlobalField,
} from "./requests";
import { globalFieldQueries, globalFieldsQueryStore } from "./queries";
import type {
  ChangeGlobalFieldStatusRequest,
  CreateGlobalFieldRequest,
  DeleteGlobalFieldRequest,
  GetGlobalFieldsRequest,
  UpdateGlobalFieldRequest,
} from "./types";

// Success toasts live in mutation hooks; submit errors are handled by AppForm/page handlers.
export function useGlobalFieldsQuery(params: GetGlobalFieldsRequest) {
  return useQuery(globalFieldQueries.list(params));
}

export function useGlobalFieldQuery(fieldId: string) {
  return useQuery(globalFieldQueries.detail(fieldId));
}

function useGlobalFieldInvalidation() {
  const queryClient = useQueryClient();
  return async () => queryClient.invalidateQueries({ queryKey: globalFieldsQueryStore.list._def });
}

export function useCreateGlobalFieldMutation() {
  const invalidate = useGlobalFieldInvalidation();
  return useMutation({
    mutationFn: (payload: CreateGlobalFieldRequest) => createGlobalField({ payload }),
    onSuccess: async (response) => {
      toast.success(translateApiMessage(response.messageKey ?? "SCS-001", mergeMessageParameters({ object: "field" }, response.parameters)));
      await invalidate();
      return requireResponseData(response);
    },
  });
}

export function useUpdateGlobalFieldMutation(fieldId: string) {
  const queryClient = useQueryClient();
  const invalidate = useGlobalFieldInvalidation();
  return useMutation({
    mutationFn: (payload: UpdateGlobalFieldRequest) => updateGlobalField({ fieldId }, { payload }),
    onSuccess: async (response) => {
      toast.success(translateApiMessage(response.messageKey ?? "SCS-002", mergeMessageParameters({ object: "field" }, response.parameters)));
      const field = requireResponseData(response);
      queryClient.setQueryData(globalFieldsQueryStore.detail(fieldId).queryKey, field);
      await invalidate();
      return field;
    },
  });
}

export function useChangeGlobalFieldStatusMutation() {
  const invalidate = useGlobalFieldInvalidation();
  return useMutation({
    mutationFn: ({ fieldId, ...payload }: ChangeGlobalFieldStatusRequest & { fieldId: string }) =>
      changeGlobalFieldStatus({ fieldId }, { payload }),
    onSuccess: async (response) => {
      toast.success(translateApiMessage(response.messageKey ?? "SCS-003", mergeMessageParameters({ object: "field" }, response.parameters)));
      await invalidate();
    },
  });
}

export function useDeleteGlobalFieldMutation() {
  const invalidate = useGlobalFieldInvalidation();
  return useMutation({
    mutationFn: ({ fieldId, ...payload }: DeleteGlobalFieldRequest & { fieldId: string }) =>
      deleteGlobalField({ fieldId }, { payload }),
    onSuccess: async (response) => {
      toast.success(translateApiMessage(response.messageKey ?? "SCS-003", mergeMessageParameters({ object: "field" }, response.parameters)));
      await invalidate();
    },
  });
}
