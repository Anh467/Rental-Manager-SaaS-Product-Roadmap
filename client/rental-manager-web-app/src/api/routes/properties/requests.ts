import { v1Client, type ApiRequestOptions, type PageResult } from "@/api/client";
import type {
  CreatePropertyRequest,
  GetPropertiesRequest,
  Property,
  PropertyPathParams,
  UpdatePropertyRequest,
} from "./types";

export function getProperties(options: ApiRequestOptions<GetPropertiesRequest>) {
  return v1Client.get<PageResult<Property>, GetPropertiesRequest>("/api/properties", options);
}

export function getProperty(
  path: PropertyPathParams,
  options?: ApiRequestOptions,
) {
  return v1Client.get<Property>(`/api/properties/${path.propertyId}`, options);
}

export function createProperty(
  options: ApiRequestOptions<Record<string, never>, CreatePropertyRequest>,
) {
  return v1Client.post<Property, CreatePropertyRequest>("/api/properties", options);
}

export function updateProperty(
  path: PropertyPathParams,
  options: ApiRequestOptions<Record<string, never>, UpdatePropertyRequest>,
) {
  return v1Client.put<Property, UpdatePropertyRequest>(
    `/api/properties/${path.propertyId}`,
    options,
  );
}

export function deleteProperty(
  path: PropertyPathParams,
  options?: ApiRequestOptions,
) {
  return v1Client.delete(`/api/properties/${path.propertyId}`, options);
}
