import type { AxiosInstance, AxiosRequestConfig } from "axios";

export const SUCCESS_MESSAGE_KEYS = [
  "SCS-001", "SCS-002", "SCS-003", "SCS-004", "SCS-005",
  "SCS-006", "SCS-007", "SCS-008", "SCS-009", "SCS-010",
  "SCS-011", "SCS-012", "SCS-013", "SCS-014", "SCS-015",
  "SCS-016", "SCS-017", "SCS-018", "SCS-019", "SCS-020",
] as const;

export const ERROR_MESSAGE_KEYS = [
  "ERR-001", "ERR-002", "ERR-003", "ERR-004", "ERR-005",
  "ERR-006", "ERR-007", "ERR-008", "ERR-009", "ERR-010",
  "ERR-011", "ERR-012", "ERR-013", "ERR-014", "ERR-015",
  "ERR-016", "ERR-017", "ERR-018", "ERR-019", "ERR-020",
  "ERR-021", "ERR-022", "ERR-023", "ERR-024", "ERR-025",
  "ERR-026", "ERR-027", "ERR-028", "ERR-029", "ERR-030",
  "ERR-031", "ERR-032", "ERR-033", "ERR-034", "ERR-035",
  "ERR-036", "ERR-037", "ERR-038", "ERR-039", "ERR-040",
  "ERR-041", "ERR-042", "ERR-043", "ERR-044", "ERR-045",
  "ERR-046", "ERR-047", "ERR-048", "ERR-049", "ERR-050",
] as const;

export type SuccessMessageKey = (typeof SUCCESS_MESSAGE_KEYS)[number];
export type ErrorMessageKey = (typeof ERROR_MESSAGE_KEYS)[number];
export type MessageKey = SuccessMessageKey | ErrorMessageKey;

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
  messageKey?: SuccessMessageKey;
  data: T;
  parameters?: ApiMessageParameters;
  correlationId?: string;
};

export type ApiErrorResponse = {
  success: false;
  messageKey: ErrorMessageKey;
  parameters?: ApiMessageParameters;
  fieldErrors?: ApiFieldError[];
  correlationId?: string;
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
  getAccessToken?: () => string | null | undefined;
  getOrganizationId?: () => string | null | undefined;
  onUnauthorized?: () => void;
};

export type ApiRequestOptions<
  QueryParams = ApiEmptyObject,
  RequestPayload = ApiEmptyObject,
> = Omit<AxiosRequestConfig<RequestPayload>, "url" | "params" | "data"> & {
  query?: QueryParams;
  payload?: RequestPayload;
};

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
