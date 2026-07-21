import { isAxiosError } from "axios";

import type {
  ApiError,
  ApiErrorResponse,
  ApiMessageParameters,
  ApiSerializablePrimitive,
  ApiSuccessResponse,
  ErrorMessageKey,
} from "./types";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

export function isApiSuccessResponse<T>(value: unknown): value is ApiSuccessResponse<T> {
  return isRecord(value) && value.success === true && "data" in value;
}

export function isApiErrorResponse(value: unknown): value is ApiErrorResponse {
  return (
    isRecord(value) &&
    value.success === false &&
    typeof value.messageKey === "string" &&
    value.messageKey.startsWith("ERR-")
  );
}

export function isApiError(value: unknown): value is ApiError {
  return (
    value instanceof Error &&
    value.name === "ApiError" &&
    "status" in value &&
    "messageKey" in value
  );
}

function fallbackMessageKey(status: number): ErrorMessageKey {
  if (status === 401) return "ERR-003";
  if (status === 403) return "ERR-004";
  if (status === 404) return "ERR-002";
  if (status === 429) return "ERR-040";
  if (status === 400 || status === 422) return "ERR-001";
  if (status === 503 || status === 504 || status === 0) return "ERR-049";
  return "ERR-050";
}

export function createApiError(
  status: number,
  payload?: Partial<ApiErrorResponse>,
  cause?: unknown,
): ApiError {
  const messageKey = payload?.messageKey ?? fallbackMessageKey(status);
  const error = new Error(messageKey) as ApiError;

  error.name = "ApiError";
  error.status = status;
  error.messageKey = messageKey;
  error.parameters = payload?.parameters ?? (status === 0 ? { service: "api" } : {});
  error.fieldErrors = payload?.fieldErrors ?? [];
  error.correlationId = payload?.correlationId;
  error.data = payload;
  error.cause = cause;

  return error;
}

export function toApiError(error: unknown): ApiError {
  if (isApiError(error)) return error;

  if (isAxiosError(error)) {
    const status = error.response?.status ?? 0;
    const payload = error.response?.data;
    return createApiError(
      status,
      isApiErrorResponse(payload) ? payload : undefined,
      error,
    );
  }

  return createApiError(500, undefined, error);
}

export function normalizeSuccessResponse<T>(value: unknown): ApiSuccessResponse<T> {
  if (isApiSuccessResponse<T>(value)) return value;
  return { success: true, data: value as T };
}

export function getPaginationQueryParams({
  pageIndex,
  pageSize,
}: {
  pageIndex: number;
  pageSize: number;
}) {
  return { page: pageIndex + 1, pageSize };
}

export function getSortingQueryParams(sorting: Array<{ id: string; desc: boolean }>) {
  const first = sorting[0];
  if (!first) return {};
  return { sortBy: first.id, sortDirection: first.desc ? "desc" : "asc" } as const;
}

export function getSearchQueryParams(search: string) {
  const trimmed = search.trim();
  return trimmed ? { search: trimmed } : {};
}

export function getFilterQueryParams<
  Key extends string,
  Value extends ApiSerializablePrimitive | ApiSerializablePrimitive[],
>({
  key,
  value,
}: {
  key: Key;
  value: Value | null | undefined;
}): Partial<Record<Key, Value>> {
  if (value == null || value === "") return {};
  return { [key]: value } as Partial<Record<Key, Value>>;
}

export function mergeMessageParameters(
  ...values: Array<ApiMessageParameters | undefined>
): ApiMessageParameters {
  return Object.assign({}, ...values);
}
