import type { ApiPathParams, PageRequest } from "@/api/client";

export type GlobalFieldTypeId = 1 | 2 | 4 | 6;

export type FieldOption = {
  id?: string;
  key: string;
  name: string;
  description?: string;
  displayOrder: number;
  isActive: boolean;
};

export type GlobalField = {
  id: string;
  key: string;
  name: string;
  description?: string;
  fieldTypeId: GlobalFieldTypeId;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
  options: FieldOption[];
};

export type GetGlobalFieldsRequest = PageRequest & {
  fieldTypeId?: GlobalFieldTypeId;
  isActive?: boolean;
  pageNumber?: number;
};

export type GlobalFieldPathParams = ApiPathParams<"fieldId">;

export type GlobalFieldPayload = Pick<GlobalField, "key" | "name" | "description" | "fieldTypeId" | "isActive" | "options">;
export type CreateGlobalFieldRequest = GlobalFieldPayload;
export type UpdateGlobalFieldRequest = GlobalFieldPayload & { rowVersion: string };
export type ChangeGlobalFieldStatusRequest = Pick<GlobalField, "isActive" | "rowVersion">;
export type DeleteGlobalFieldRequest = Pick<GlobalField, "rowVersion">;
