import { apiRequest } from "@/api/client";
import type { PageResult } from "@/api/types";
import type {
  CreatePropertyRequest,
  GetPropertiesRequest,
  Property,
  UpdatePropertyRequest,
} from "@/api/routes/properties/types";

export function getProperties(params: GetPropertiesRequest, signal?: AbortSignal) {
  return apiRequest<PageResult<Property>>({
    url: "/api/properties",
    method: "GET",
    params,
    signal,
  });
}

export function getProperty(propertyId: string, signal?: AbortSignal) {
  return apiRequest<Property>({
    url: `/api/properties/${propertyId}`,
    method: "GET",
    signal,
  });
}

export function createProperty(payload: CreatePropertyRequest) {
  return apiRequest<Property>({
    url: "/api/properties",
    method: "POST",
    data: payload,
  });
}

export function updateProperty(propertyId: string, payload: UpdatePropertyRequest) {
  return apiRequest<Property>({
    url: `/api/properties/${propertyId}`,
    method: "PUT",
    data: payload,
  });
}

export function deleteProperty(propertyId: string) {
  return apiRequest<void>({
    url: `/api/properties/${propertyId}`,
    method: "DELETE",
  });
}
