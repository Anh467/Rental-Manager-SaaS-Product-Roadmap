import axios, { AxiosError, type AxiosRequestConfig } from "axios";

import type { ApiProblem } from "@/api/types";

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "http://localhost:5008",
  timeout: 30_000,
  headers: { "Content-Type": "application/json" },
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("access_token");
  const organizationId = localStorage.getItem("organization_id");

  if (token) config.headers.Authorization = `Bearer ${token}`;
  if (organizationId) config.headers["X-Organization-Id"] = organizationId;

  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiProblem>) => {
    if (error.response?.status === 401) {
      localStorage.removeItem("access_token");
    }
    return Promise.reject(error.response?.data ?? error);
  },
);

export async function apiRequest<T>(config: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.request<T>(config);
  return response.data;
}
