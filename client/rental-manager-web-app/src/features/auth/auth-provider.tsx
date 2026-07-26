import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";

import { authQueries, login, useMeQuery, type AuthUser, type LoginRequest } from "@/api/routes/auth";
import { PermissionProvider } from "@/components/common/permission-guard";
import {
  clearAuthSession,
  getAccessTokenFromLoginResponse,
  storeAuthUserContext,
} from "@/features/auth/auth-session";

type AuthContextValue = {
  user: AuthUser | null;
  permissions: readonly string[];
  scope: AuthUser["scope"] | null;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<AuthUser>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [tokenPresent, setTokenPresent] = useState(() => Boolean(localStorage.getItem("access_token")));
  const meQuery = useMeQuery(tokenPresent);

  useEffect(() => {
    if (meQuery.data) storeAuthUserContext(meQuery.data);
  }, [meQuery.data]);

  const logout = useCallback(() => {
    clearAuthSession(queryClient);
    setTokenPresent(false);
  }, [queryClient]);

  const authenticate = useCallback(async (credentials: LoginRequest) => {
    const loginResponse = await login({ payload: credentials });
    const token = getAccessTokenFromLoginResponse(loginResponse.data);
    if (!token) throw new Error("The login response did not include an access token.");

    localStorage.setItem("access_token", token);
    setTokenPresent(true);

    const nextUser = await queryClient.fetchQuery(authQueries.me());
    storeAuthUserContext(nextUser);
    return nextUser;
  }, [queryClient]);

  const value = useMemo<AuthContextValue>(() => ({
    user: meQuery.data ?? null,
    permissions: meQuery.data?.permissions ?? [],
    scope: meQuery.data?.scope ?? null,
    isLoading: tokenPresent && meQuery.isPending,
    login: authenticate,
    logout,
  }), [authenticate, logout, meQuery.data, meQuery.isPending, tokenPresent]);

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
