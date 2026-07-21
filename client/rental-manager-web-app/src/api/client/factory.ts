import axios, { type AxiosResponse, type Method } from "axios";

import type {
  ApiClient,
  ApiClientOptions,
  ApiEmptyObject,
  ApiRequestOptions,
  ApiResponse,
} from "./types";
import { normalizeSuccessResponse, toApiError } from "./utils";

export function createApiClient(clientOptions: ApiClientOptions): ApiClient {
  const axiosInstance = axios.create({
    baseURL: clientOptions.baseUrl,
    timeout: clientOptions.timeoutMs ?? 30_000,
    withCredentials: clientOptions.withCredentials ?? false,
    headers: {
      "Content-Type": "application/json",
      ...clientOptions.defaultHeaders,
    },
  });

  axiosInstance.interceptors.request.use((config) => {
    const accessToken = clientOptions.getAccessToken?.();
    const organizationId = clientOptions.getOrganizationId?.();

    if (accessToken) config.headers.set("Authorization", `Bearer ${accessToken}`);
    if (organizationId) config.headers.set("X-Organization-Id", organizationId);

    return config;
  });

  axiosInstance.interceptors.response.use(
    (response) => response,
    (error: unknown) => {
      const apiError = toApiError(error);
      if (apiError.status === 401) clientOptions.onUnauthorized?.();
      return Promise.reject(apiError);
    },
  );

  const execute = async <
    ResponseBody,
    QueryParams = ApiEmptyObject,
    RequestPayload = ApiEmptyObject,
  >(
    method: Method,
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ): Promise<ApiResponse<ResponseBody>> => {
    const { query, payload, ...requestOptions } = options ?? {};
    const response: AxiosResponse<unknown> = await axiosInstance.request({
      ...requestOptions,
      method,
      url: path,
      params: query,
      data: payload,
    });

    return normalizeSuccessResponse<ResponseBody>(response.data);
  };

  const request = <
    ResponseBody,
    QueryParams = ApiEmptyObject,
    RequestPayload = ApiEmptyObject,
  >(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ) => execute<ResponseBody, QueryParams, RequestPayload>(options?.method ?? "GET", path, options);

  const get = <ResponseBody, QueryParams = ApiEmptyObject>(
    path: string,
    options?: ApiRequestOptions<QueryParams, ApiEmptyObject>,
  ) => execute<ResponseBody, QueryParams, ApiEmptyObject>("GET", path, options);

  const post = <
    ResponseBody,
    RequestPayload = ApiEmptyObject,
    QueryParams = ApiEmptyObject,
  >(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ) => execute<ResponseBody, QueryParams, RequestPayload>("POST", path, options);

  const put = <
    ResponseBody,
    RequestPayload = ApiEmptyObject,
    QueryParams = ApiEmptyObject,
  >(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ) => execute<ResponseBody, QueryParams, RequestPayload>("PUT", path, options);

  const patch = <
    ResponseBody,
    RequestPayload = ApiEmptyObject,
    QueryParams = ApiEmptyObject,
  >(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ) => execute<ResponseBody, QueryParams, RequestPayload>("PATCH", path, options);

  const deleteRequest = <
    ResponseBody = void,
    QueryParams = ApiEmptyObject,
    RequestPayload = ApiEmptyObject,
  >(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ) => execute<ResponseBody, QueryParams, RequestPayload>("DELETE", path, options);

  return {
    axios: axiosInstance,
    request,
    get,
    post,
    put,
    patch,
    delete: deleteRequest,
  };
}
