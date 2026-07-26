import { v1Client, type ApiRequestOptions } from "@/api/client";

import type {
  AuthUser,
  CsrfResponse,
  LoginRequest,
  LoginResponse,
  SelectOrganizationRequest,
} from "./types";

export function getCsrf(options?: ApiRequestOptions) {
  return v1Client.get<CsrfResponse>("/api/v1/auth/csrf", options);
}

export function login(options: ApiRequestOptions<Record<string, never>, LoginRequest>) {
  return v1Client.post<LoginResponse, LoginRequest>("/api/v1/auth/login", options);
}

export function selectOrganization(
  options: ApiRequestOptions<Record<string, never>, SelectOrganizationRequest>,
) {
  return v1Client.post<LoginResponse, SelectOrganizationRequest>(
    "/api/v1/auth/select-organization",
    options,
  );
}

export function logout(options?: ApiRequestOptions) {
  return v1Client.post<null>("/api/v1/auth/logout", options);
}

export function getMe(options?: ApiRequestOptions) {
  return v1Client.get<AuthUser>("/api/v1/auth/me", options);
}
