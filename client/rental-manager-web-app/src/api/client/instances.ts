import { createApiClient } from "./factory";

export const BASE_URLS = {
  v1: import.meta.env.VITE_API_URL ?? "http://localhost:5008",
  bff: import.meta.env.VITE_BFF_URL ?? import.meta.env.VITE_API_URL ?? "http://localhost:5008",
} as const;

function getAccessToken() {
  return localStorage.getItem("access_token");
}

function getOrganizationId() {
  return localStorage.getItem("organization_id");
}

function handleUnauthorized() {
  localStorage.removeItem("access_token");
}

const sharedOptions = {
  timeoutMs: 30_000,
  getAccessToken,
  getOrganizationId,
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
