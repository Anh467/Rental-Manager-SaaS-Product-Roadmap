import axios, {
  type AxiosResponse,
  type InternalAxiosRequestConfig,
  type Method,
} from "axios";

import {
  clearCsrfToken,
  ensureCsrfToken,
  getCsrfHeaderName,
  isUnsafeHttpMethod,
  refreshCsrfToken,
} from "./csrf";
import type {
  ApiBlobRequestOptions,
  ApiClient,
  ApiClientOptions,
  ApiEmptyObject,
  ApiJsonRequestOptions,
  ApiRequestOptions,
  ApiResponse,
} from "./types";
import { createApiError, isApiError, parseSuccessResponse, requireResponseData, toApiError } from "./utils";

type CsrfAxiosConfig = InternalAxiosRequestConfig & {
  __csrfRetried?: boolean;
};

const CORRELATION_ID_HEADER = "x-correlation-id";
const UNKNOWN_CORRELATION_ID = "unknown";

function isAntiforgeryFailure(error: unknown) {
  return isApiError(error) && error.status === 400 && error.messageKey === "ERR-001";
}

function getResponseCorrelationId(response: AxiosResponse<unknown>): string {
  const header = response.headers?.[CORRELATION_ID_HEADER];
  const value = Array.isArray(header) ? header[0] : header;
  return typeof value === "string" && value.length > 0 ? value : UNKNOWN_CORRELATION_ID;
}

export function createApiClient(clientOptions: ApiClientOptions): ApiClient {
  const axiosInstance = axios.create({
    baseURL: clientOptions.baseUrl,
    timeout: clientOptions.timeoutMs ?? 30_000,
    withCredentials: clientOptions.withCredentials ?? true,
    headers: {
      "Content-Type": "application/json",
      ...clientOptions.defaultHeaders,
    },
  });

  const fetchCsrfRequestToken = async () => {
    const response = await axiosInstance.get<unknown>("/api/v1/auth/csrf");
    const envelope = parseSuccessResponse<{ requestToken: string }>(response.data);
    return requireResponseData(envelope).requestToken;
  };

  axiosInstance.interceptors.request.use(async (config) => {
    if (!isUnsafeHttpMethod(config.method)) return config;

    const token = await ensureCsrfToken(fetchCsrfRequestToken);
    config.headers.set(getCsrfHeaderName(), token);
    return config;
  });

  axiosInstance.interceptors.response.use(
    (response) => response,
    async (error: unknown) => {
      const apiError = await toApiError(error);
      const axiosError = axios.isAxiosError(error) ? error : null;
      const requestUrl = String(axiosError?.config?.url ?? "");
      const config = axiosError?.config as CsrfAxiosConfig | undefined;

      if (
        config
        && isUnsafeHttpMethod(config.method)
        && isAntiforgeryFailure(apiError)
        && !config.__csrfRetried
      ) {
        config.__csrfRetried = true;
        const token = await refreshCsrfToken(fetchCsrfRequestToken);
        config.headers.set(getCsrfHeaderName(), token);
        return axiosInstance.request(config);
      }

      // Failed login/select-organization is expected to be 401 — do not clear session / redirect.
      const isAuthAttempt =
        requestUrl.includes("/auth/login")
        || requestUrl.includes("/auth/select-organization");
      if (apiError.status === 401 && !isAuthAttempt) {
        clearCsrfToken();
        clientOptions.onUnauthorized?.();
      }
      return Promise.reject(apiError);
    },
  );

  const executeJson = async <
    ResponseBody,
    QueryParams = ApiEmptyObject,
    RequestPayload = ApiEmptyObject,
  >(
    method: Method,
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ): Promise<ApiResponse<ResponseBody>> => {
    const { query, payload, ...requestOptions } = options ?? {};

    if (requestOptions.responseType === "blob" || requestOptions.responseType === "arraybuffer") {
      throw createApiError(500, {
        messageKey: "ERR-050",
        correlationId: UNKNOWN_CORRELATION_ID,
      });
    }

    const response: AxiosResponse<unknown> = await axiosInstance.request({
      ...requestOptions,
      method,
      url: path,
      params: query,
      data: payload,
    });

    if (response.status === 204) {
      throw createApiError(500, {
        messageKey: "ERR-050",
        correlationId: getResponseCorrelationId(response),
      });
    }

    return parseSuccessResponse<ResponseBody>(response.data);
  };

  const executeNoContent = async <
    QueryParams = ApiEmptyObject,
    RequestPayload = ApiEmptyObject,
  >(
    method: Method,
    path: string,
    options?: ApiJsonRequestOptions<QueryParams, RequestPayload>,
  ): Promise<void> => {
    const { query, payload, ...requestOptions } = options ?? {};
    const response: AxiosResponse<unknown> = await axiosInstance.request({
      ...requestOptions,
      method,
      url: path,
      params: query,
      data: payload,
    });

    if (response.status !== 204) {
      throw createApiError(500, {
        messageKey: "ERR-050",
        correlationId: getResponseCorrelationId(response),
      });
    }
  };

  const executeBlob = async <QueryParams = ApiEmptyObject>(
    path: string,
    options?: ApiBlobRequestOptions<QueryParams>,
  ): Promise<Blob> => {
    const { query, ...requestOptions } = options ?? {};
    const response: AxiosResponse<unknown> = await axiosInstance.request({
      ...requestOptions,
      method: "GET",
      url: path,
      params: query,
      responseType: "blob",
    });

    if (!(response.data instanceof Blob)) {
      throw createApiError(500, {
        messageKey: "ERR-050",
        correlationId: getResponseCorrelationId(response),
      });
    }

    return response.data;
  };

  const request = <
    ResponseBody,
    QueryParams = ApiEmptyObject,
    RequestPayload = ApiEmptyObject,
  >(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ) => {
    const method = (options?.method ?? "GET") as Method;
    return executeJson<ResponseBody, QueryParams, RequestPayload>(method, path, options);
  };

  const get = <ResponseBody, QueryParams = ApiEmptyObject>(
    path: string,
    options?: ApiRequestOptions<QueryParams, ApiEmptyObject>,
  ) => executeJson<ResponseBody, QueryParams, ApiEmptyObject>("GET", path, options);

  const post = <
    ResponseBody,
    RequestPayload = ApiEmptyObject,
    QueryParams = ApiEmptyObject,
  >(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ) => executeJson<ResponseBody, QueryParams, RequestPayload>("POST", path, options);

  const put = <
    ResponseBody,
    RequestPayload = ApiEmptyObject,
    QueryParams = ApiEmptyObject,
  >(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ) => executeJson<ResponseBody, QueryParams, RequestPayload>("PUT", path, options);

  const patch = <
    ResponseBody,
    RequestPayload = ApiEmptyObject,
    QueryParams = ApiEmptyObject,
  >(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ) => executeJson<ResponseBody, QueryParams, RequestPayload>("PATCH", path, options);

  const deleteRequest = <
    ResponseBody = void,
    QueryParams = ApiEmptyObject,
    RequestPayload = ApiEmptyObject,
  >(
    path: string,
    options?: ApiRequestOptions<QueryParams, RequestPayload>,
  ) => executeJson<ResponseBody, QueryParams, RequestPayload>("DELETE", path, options);

  const requestNoContent = <
    QueryParams = ApiEmptyObject,
    RequestPayload = ApiEmptyObject,
  >(
    path: string,
    options: ApiJsonRequestOptions<QueryParams, RequestPayload> & { method: Method },
  ) => executeNoContent(options.method, path, options);

  const deleteNoContent = <
    QueryParams = ApiEmptyObject,
    RequestPayload = ApiEmptyObject,
  >(
    path: string,
    options?: ApiJsonRequestOptions<QueryParams, RequestPayload>,
  ) => executeNoContent("DELETE", path, options);

  const getBlob = <QueryParams = ApiEmptyObject>(
    path: string,
    options?: ApiBlobRequestOptions<QueryParams>,
  ) => executeBlob(path, options);

  const download = <QueryParams = ApiEmptyObject>(
    path: string,
    options?: ApiBlobRequestOptions<QueryParams>,
  ) => executeBlob(path, options);

  return {
    axios: axiosInstance,
    request,
    get,
    post,
    put,
    patch,
    delete: deleteRequest,
    requestNoContent,
    deleteNoContent,
    getBlob,
    download,
  };
}
