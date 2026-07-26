import { clearCsrfToken } from "./csrf";
import { createApiClient } from "./factory";

export const BASE_URLS = {
  v1: import.meta.env.VITE_API_URL ?? "",
  bff: import.meta.env.VITE_BFF_URL ?? import.meta.env.VITE_API_URL ?? "",
} as const;

function handleUnauthorized() {
  clearCsrfToken();
  if (window.location.pathname !== "/login") {
    window.location.assign("/login");
  }
}

const sharedOptions = {
  timeoutMs: 30_000,
  withCredentials: true,
  onUnauthorized: handleUnauthorized,
};

export const v1Client = createApiClient({
  ...sharedOptions,
  baseUrl: BASE_URLS.v1,
});

export const bffClient = createApiClient({
  ...sharedOptions,
  baseUrl: BASE_URLS.bff,
});
