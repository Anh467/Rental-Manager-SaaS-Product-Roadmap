import { createContext, useCallback, useContext, useMemo, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";

import { clearCsrfToken, refreshCsrfToken, requireResponseData } from "@/api/client";
import {
  authQueries,
  getCsrf,
  getExternalLoginStartUrl,
  isOrganizationSelectionRequired,
  login,
  logout as logoutRequest,
  selectOrganization,
  useMeQuery,
  type AuthUser,
  type LoginRequest,
  type OrganizationOption,
} from "@/api/routes/auth";
import { PermissionProvider } from "@/components/common/permission-guard";
import { clearAuthSession } from "@/features/auth/auth-session";
import {
  clearPendingOrganizationSelection,
  getPendingOrganizationSelection,
  setPendingOrganizationSelection,
} from "@/features/auth/organization-selection";

export type AuthenticateResult =
  | { status: "authenticated"; user: AuthUser }
  | { status: "organizationSelectionRequired"; organizations: OrganizationOption[] };

type AuthContextValue = {
  user: AuthUser | null;
  permissions: readonly string[];
  scope: AuthUser["scope"] | null;
  isLoading: boolean;
  /** Completes sign-in after an external principal is present (or mock provider/subject). */
  completeLogin: (payload?: LoginRequest) => Promise<AuthenticateResult>;
  /** Starts the browser OIDC redirect to GET /api/v1/auth/login. */
  startExternalLogin: (returnUrl?: string) => void;
  completeOrganizationSelection: (organizationId: string) => Promise<AuthUser>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

async function ensureFreshCsrf() {
  await refreshCsrfToken(async () => requireResponseData(await getCsrf()).requestToken);
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const meQuery = useMeQuery(true);

  const completeLogin = useCallback(async (payload: LoginRequest = {}): Promise<AuthenticateResult> => {
    await ensureFreshCsrf();
    const loginData = requireResponseData(await login({ payload }));

    if (isOrganizationSelectionRequired(loginData)) {
      setPendingOrganizationSelection({
        selectionTicket: loginData.selectionTicket,
        organizations: loginData.organizations,
      });
      return {
        status: "organizationSelectionRequired",
        organizations: loginData.organizations,
      };
    }

    clearPendingOrganizationSelection();
    await ensureFreshCsrf();
    const nextUser = await queryClient.fetchQuery(authQueries.me());
    return { status: "authenticated", user: nextUser };
  }, [queryClient]);

  const startExternalLogin = useCallback((returnUrl = "/login/callback") => {
    window.location.assign(getExternalLoginStartUrl(returnUrl));
  }, []);

  const completeOrganizationSelection = useCallback(async (organizationId: string) => {
    const pending = getPendingOrganizationSelection();
    if (!pending?.selectionTicket) {
      throw new Error("Organization selection is not available. Please sign in again.");
    }

    await ensureFreshCsrf();
    const selected = requireResponseData(await selectOrganization({
      payload: {
        selectionTicket: pending.selectionTicket,
        organizationId,
      },
    }));

    if (isOrganizationSelectionRequired(selected)) {
      throw new Error("Organization selection did not complete.");
    }

    clearPendingOrganizationSelection();
    await ensureFreshCsrf();
    return queryClient.fetchQuery(authQueries.me());
  }, [queryClient]);

  const logout = useCallback(async () => {
    try {
      await ensureFreshCsrf();
      await logoutRequest();
    } catch {
      // Always clear local auth state even if the network call fails.
    } finally {
      clearCsrfToken();
      clearAuthSession(queryClient);
    }
  }, [queryClient]);

  const value = useMemo<AuthContextValue>(() => ({
    user: meQuery.data ?? null,
    permissions: meQuery.data?.permissions ?? [],
    scope: meQuery.data?.scope ?? null,
    isLoading: meQuery.isPending,
    completeLogin,
    startExternalLogin,
    completeOrganizationSelection,
    logout,
  }), [
    completeLogin,
    completeOrganizationSelection,
    logout,
    meQuery.data,
    meQuery.isPending,
    startExternalLogin,
  ]);

  return (
    <AuthContext.Provider value={value}>
      <PermissionProvider permissions={value.permissions}>{children}</PermissionProvider>
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error("useAuth must be used within AuthProvider.");
  return value;
}

export function useLogoutAndRedirect() {
  const { logout } = useAuth();
  const navigate = useNavigate();

  return useCallback(async () => {
    await logout();
    await navigate({ to: "/login", replace: true });
  }, [logout, navigate]);
}
