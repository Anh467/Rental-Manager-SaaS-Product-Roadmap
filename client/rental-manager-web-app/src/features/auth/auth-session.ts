import type { QueryClient } from "@tanstack/react-query";
import { redirect, isRedirect } from "@tanstack/react-router";

import { clearCsrfToken, isApiError } from "@/api/client";
import { authQueries } from "@/api/routes/auth";
import type { AuthUser } from "@/api/routes/auth";
import { clearPendingOrganizationSelection } from "@/features/auth/organization-selection";

export const AUTH_ME_QUERY_KEY = ["auth", "me"] as const;

export function clearAuthSession(queryClient?: QueryClient) {
  clearCsrfToken();
  clearPendingOrganizationSelection();
  queryClient?.removeQueries({ queryKey: AUTH_ME_QUERY_KEY });
}

/**
 * Resolve how authenticated loaders should react to a failed /me request.
 * Only HTTP 401 clears the session and sends the user to /login.
 * HTTP 403 goes to /access-denied. All other failures are rethrown.
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
  try {
    return await queryClient.ensureQueryData(authQueries.me());
  } catch (error) {
    resolveAuthenticatedMeFailure(error, queryClient);
  }
}

/**
 * Login route: only redirect away when /me succeeds for the cookie session.
 * Anonymous / invalid / transient failures stay on /login (no redirect loop).
 */
export async function redirectIfAuthenticated(queryClient: QueryClient) {
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
