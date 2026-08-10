import { isAxiosError, type AxiosError } from "axios";

import type {
  ApiError,
  ApiErrorResponse,
  ApiMessageParameters,
  ApiSerializablePrimitive,
  ApiSuccessResponse,
  ErrorMessageKey,
} from "./types";
import { isActiveSuccessMessageKey, isKnownErrorMessageKey } from "./message-catalog";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.length > 0;
}

export function isApiSuccessResponse<T>(value: unknown): value is ApiSuccessResponse<T> {
  return (
    isRecord(value) &&
    value.success === true &&
    typeof value.messageKey === "string" &&
    isActiveSuccessMessageKey(value.messageKey) &&
    isNonEmptyString(value.correlationId) &&
    "data" in value
  );
}

export function isApiErrorResponse(value: unknown): value is ApiErrorResponse {
  return (
    isRecord(value) &&
    value.success === false &&
    typeof value.messageKey === "string" &&
    isKnownErrorMessageKey(value.messageKey) &&
    isNonEmptyString(value.correlationId)
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

/**
 * Reads the error body for an Axios error, converting a Blob/ArrayBuffer
 * payload back into JSON when the server responded with a JSON error
 * envelope. This matters for requests made with `responseType: "blob"` or
 * `"arraybuffer"`, where Axios does not know that an *error* response has a
 * different content type than the success response the caller expected.
 */
async function resolveErrorPayload(error: AxiosError): Promise<unknown> {
  const data: unknown = error.response?.data;

  if (typeof Blob !== "undefined" && data instanceof Blob) {
    if (!data.type.toLowerCase().includes("json")) return undefined;
    try {
      return JSON.parse(await data.text());
    } catch {
      return undefined;
    }
  }

  if (typeof ArrayBuffer !== "undefined" && data instanceof ArrayBuffer) {
    try {
      return JSON.parse(new TextDecoder().decode(data));
    } catch {
      return undefined;
    }
  }

  return data;
}

export async function toApiError(error: unknown): Promise<ApiError> {
  if (isApiError(error)) return error;

  if (isAxiosError(error)) {
    const status = error.response?.status ?? 0;
    const payload = await resolveErrorPayload(error);
    return createApiError(
      status,
      isApiErrorResponse(payload) ? payload : undefined,
      error,
    );
  }

  return createApiError(500, undefined, error);
}

/**
 * Strictly validates that a response body is the success envelope: `success
 * === true`, a catalog-active `messageKey`, a non-empty `correlationId`, and a
 * `data` property (which may be null). Arbitrary JSON is never silently
 * accepted as a success response — an unrecognized shape is a contract
 * violation and is reported as `ERR-050`, not wrapped.
 */
export function parseSuccessResponse<T>(value: unknown): ApiSuccessResponse<T> {
  if (isApiSuccessResponse<T>(value)) return value;
  throw createApiError(500, { messageKey: "ERR-050" });
}

/**
 * Endpoints that contractually return a non-null `data` payload. A null data
 * body is a contract violation (distinct from HTTP 204, which has no body).
 */
export function requireResponseData<T>(response: ApiSuccessResponse<T>): T {
  if (response.data === null) {
    throw createApiError(500, {
      messageKey: "ERR-050",
      correlationId: response.correlationId,
    });
  }

  return response.data;
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
