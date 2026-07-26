import { v1Client, type ApiRequestOptions } from "@/api/client";

import type { AuthUser, LoginRequest, LoginResponse } from "./types";

export function login(options: ApiRequestOptions<Record<string, never>, LoginRequest>) {
  return v1Client.post<LoginResponse, LoginRequest>("/api/v1/auth/login", options);
}

export function getMe(options?: ApiRequestOptions) {
  return v1Client.get<AuthUser>("/api/v1/auth/me", options);
}
