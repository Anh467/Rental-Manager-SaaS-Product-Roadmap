import type { AxiosInstance, AxiosRequestConfig, Method } from "axios";

export {
  ACTIVE_ERROR_MESSAGE_KEYS,
  ACTIVE_SUCCESS_MESSAGE_KEYS,
  ALL_ERROR_MESSAGE_KEYS,
  ALL_MESSAGE_KEYS,
  ALL_SUCCESS_MESSAGE_KEYS,
  DEPRECATED_ERROR_MESSAGE_KEYS,
  DEPRECATED_SUCCESS_MESSAGE_KEYS,
  isActiveErrorMessageKey,
  isActiveSuccessMessageKey,
  isDeprecatedErrorMessageKey,
  isKnownErrorMessageKey,
} from "./message-catalog";
export type {
  ActiveErrorMessageKey,
  ActiveSuccessMessageKey,
  DeprecatedErrorMessageKey,
  DeprecatedSuccessMessageKey,
  ErrorMessageKey,
  MessageKey,
  SuccessMessageKey,
} from "./message-catalog";
import type { ErrorMessageKey, SuccessMessageKey } from "./message-catalog";

export type ApiSerializablePrimitive = string | number | boolean;
export type ApiMessageParameter =
  | ApiSerializablePrimitive
  | ApiSerializablePrimitive[]
  | null
  | undefined;
export type ApiMessageParameters = Record<string, ApiMessageParameter>;
export type ApiEmptyObject = Record<string, never>;
export type ApiPathParams<T extends string> = Record<T, string>;

export type ApiFieldError = {
  fieldKey: string;
  messageKey: ErrorMessageKey;
  parameters?: ApiMessageParameters;
};

export type ApiSuccessResponse<T> = {
  success: true;
  messageKey: SuccessMessageKey;
  data: T | null;
  parameters?: ApiMessageParameters;
  correlationId: string;
};

export type ApiErrorResponse = {
  success: false;
  messageKey: ErrorMessageKey;
  parameters?: ApiMessageParameters;
  fieldErrors?: ApiFieldError[];
  correlationId: string;
};

export type ApiResponse<T> = ApiSuccessResponse<T>;

export type ApiError = Error & {
  name: "ApiError";
  status: number;
  messageKey: ErrorMessageKey;
  parameters: ApiMessageParameters;
  fieldErrors: ApiFieldError[];
  correlationId?: string;
  data?: unknown;
  cause?: unknown;
};

export type ApiClientOptions = {
  baseUrl: string;
  timeoutMs?: number;
  withCredentials?: boolean;
  defaultHeaders?: Record<string, string>;
  onUnauthorized?: () => void;
};

export type ApiRequestOptions<
  QueryParams = ApiEmptyObject,
  RequestPayload = ApiEmptyObject,
> = Omit<AxiosRequestConfig<RequestPayload>, "url" | "params" | "data"> & {
  query?: QueryParams;
  payload?: RequestPayload;
};

export type ApiJsonRequestOptions<
  QueryParams = ApiEmptyObject,
  RequestPayload = ApiEmptyObject,
> = Omit<ApiRequestOptions<QueryParams, RequestPayload>, "responseType">;

export type ApiBlobRequestOptions<QueryParams = ApiEmptyObject> = Omit<
  ApiRequestOptions<QueryParams, ApiEmptyObject>,
  "responseType" | "method" | "payload"
>;

export type ApiClient = {
  axios: AxiosInstance;
  request<ResponseBody, QueryParams = ApiEmptyObject, RequestPayload = ApiEmptyObject>(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ): Promise<ApiResponse<ResponseBody>>;
  get<ResponseBody, QueryParams = ApiEmptyObject>(
    path: string,
    options?: ApiRequestOptions<QueryParams, ApiEmptyObject>,
  ): Promise<ApiResponse<ResponseBody>>;
  post<ResponseBody, RequestPayload = ApiEmptyObject, QueryParams = ApiEmptyObject>(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ): Promise<ApiResponse<ResponseBody>>;
  put<ResponseBody, RequestPayload = ApiEmptyObject, QueryParams = ApiEmptyObject>(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ): Promise<ApiResponse<ResponseBody>>;
  patch<ResponseBody, RequestPayload = ApiEmptyObject, QueryParams = ApiEmptyObject>(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ): Promise<ApiResponse<ResponseBody>>;
  delete<ResponseBody = void, QueryParams = ApiEmptyObject, RequestPayload = ApiEmptyObject>(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ): Promise<ApiResponse<ResponseBody>>;
  /** Expect HTTP 204 with no body. Does not invent messageKey/data. */
  requestNoContent<QueryParams = ApiEmptyObject, RequestPayload = ApiEmptyObject>(
    path: string,
    options: ApiJsonRequestOptions<QueryParams, RequestPayload> & { method: Method },
  ): Promise<void>;
  deleteNoContent<QueryParams = ApiEmptyObject, RequestPayload = ApiEmptyObject>(
    path: string,
    options?: ApiJsonRequestOptions<QueryParams, RequestPayload>,
  ): Promise<void>;
  /** Successful file body; errors still go through toApiError envelope parsing. */
  getBlob<QueryParams = ApiEmptyObject>(
    path: string,
    options?: ApiBlobRequestOptions<QueryParams>,
  ): Promise<Blob>;
  download<QueryParams = ApiEmptyObject>(
    path: string,
    options?: ApiBlobRequestOptions<QueryParams>,
  ): Promise<Blob>;
};

export type PageRequest = {
  page: number;
  pageSize: number;
  search?: string;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
};

export type PageResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};
