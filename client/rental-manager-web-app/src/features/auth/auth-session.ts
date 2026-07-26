import type { QueryClient } from "@tanstack/react-query";
import { redirect, isRedirect } from "@tanstack/react-router";

import { isApiError } from "@/api/client";
import { authQueries } from "@/api/routes/auth";
import type { AuthUser } from "@/api/routes/auth";

export const AUTH_ME_QUERY_KEY = ["auth", "me"] as const;

export function clearAuthSession(queryClient?: QueryClient) {
  localStorage.removeItem("access_token");
  localStorage.removeItem("organization_id");
  queryClient?.removeQueries({ queryKey: AUTH_ME_QUERY_KEY });
}

export function storeAuthUserContext(user: AuthUser) {
  if (user.scope === "organization" && user.organizationId) {
    localStorage.setItem("organization_id", user.organizationId);
  } else {
    localStorage.removeItem("organization_id");
  }
}

export function getAccessTokenFromLoginResponse(response: {
  accessToken?: string;
  access_token?: string;
  token?: string;
}) {
  return response.accessToken ?? response.access_token ?? response.token;
}

/**
 * Resolve how authenticated loaders should react to a failed /me request.
 * Only HTTP 401 clears the session and sends the user to /login.
 * HTTP 403 goes to /access-denied. All other failures are rethrown (keep token).
 */
export function resolveAuthenticatedMeFailure(error: unknown, queryClient?: QueryClient): never {
  if (isRedirect(error)) throw error;

  if (isApiError(error) && error.status === 403) {
    throw redirect({ to: "/access-denied", replace: true });
  }

  if (isApiError(error) && error.status === 401) {
    clearAuthSession(queryClient);
    throw redirect({ to: "/login", replace: true });
  }

  throw error;
}

export async function ensureAuthenticatedUser(queryClient: QueryClient): Promise<AuthUser> {
  if (!localStorage.getItem("access_token")) {
    throw redirect({ to: "/login", replace: true });
  }

  try {
    return await queryClient.ensureQueryData(authQueries.me());
  } catch (error) {
    resolveAuthenticatedMeFailure(error, queryClient);
  }
}

/**
 * Login route: only redirect away when /me succeeds for the stored token.
 * Unverified / invalid / transient failures stay on /login (no redirect loop).
 */
export async function redirectIfAuthenticated(queryClient: QueryClient) {
  if (!localStorage.getItem("access_token")) return;

  try {
    await queryClient.ensureQueryData(authQueries.me());
    throw redirect({ to: "/", replace: true });
  } catch (error) {
    if (isRedirect(error)) throw error;

    if (isApiError(error) && error.status === 401) {
      clearAuthSession(queryClient);
    }
  }
}
