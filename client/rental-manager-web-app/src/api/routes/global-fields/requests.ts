import { v1Client, type ApiRequestOptions, type PageResult } from "@/api/client";

import type {
  ChangeGlobalFieldStatusRequest,
  CreateGlobalFieldRequest,
  DeleteGlobalFieldRequest,
  GetGlobalFieldsRequest,
  GlobalField,
  GlobalFieldPathParams,
  UpdateGlobalFieldRequest,
} from "./types";

const basePath = "/api/v1/global/fields";

export function getGlobalFields(options: ApiRequestOptions<GetGlobalFieldsRequest>) {
  return v1Client.get<PageResult<GlobalField>, GetGlobalFieldsRequest>(basePath, options);
}

export function getGlobalField(path: GlobalFieldPathParams, options?: ApiRequestOptions) {
  return v1Client.get<GlobalField>(`${basePath}/${path.fieldId}`, options);
}

export function createGlobalField(options: ApiRequestOptions<Record<string, never>, CreateGlobalFieldRequest>) {
  return v1Client.post<GlobalField, CreateGlobalFieldRequest>(basePath, options);
}

export function updateGlobalField(
  path: GlobalFieldPathParams,
  options: ApiRequestOptions<Record<string, never>, UpdateGlobalFieldRequest>,
) {
  return v1Client.put<GlobalField, UpdateGlobalFieldRequest>(`${basePath}/${path.fieldId}`, options);
}

export function changeGlobalFieldStatus(
  path: GlobalFieldPathParams,
  options: ApiRequestOptions<Record<string, never>, ChangeGlobalFieldStatusRequest>,
) {
  return v1Client.patch<GlobalField, ChangeGlobalFieldStatusRequest>(`${basePath}/${path.fieldId}/status`, options);
}

export function deleteGlobalField(
  path: GlobalFieldPathParams,
  options: ApiRequestOptions<Record<string, never>, DeleteGlobalFieldRequest>,
) {
  return v1Client.delete<void, Record<string, never>, DeleteGlobalFieldRequest>(`${basePath}/${path.fieldId}`, options);
}
