import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";

import { getMe, login, type AuthUser, type LoginRequest } from "@/api/routes/auth";
import { PermissionProvider } from "@/components/common/permission-guard";

type AuthContextValue = {
  user: AuthUser | null;
  permissions: readonly string[];
  scope: AuthUser["scope"] | null;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<AuthUser>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

function storeUserContext(user: AuthUser) {
  if (user.scope === "organization" && user.organizationId) {
    localStorage.setItem("organization_id", user.organizationId);
  } else {
    localStorage.removeItem("organization_id");
  }
}

function getToken(response: { accessToken?: string; access_token?: string; token?: string }) {
  return response.accessToken ?? response.access_token ?? response.token;
}

export async function loadAuthenticatedUser() {
  return (await getMe()).data;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(() => Boolean(localStorage.getItem("access_token")));

  const logout = useCallback(() => {
    localStorage.removeItem("access_token");
    localStorage.removeItem("organization_id");
    setUser(null);
  }, []);

  useEffect(() => {
    if (!localStorage.getItem("access_token")) return;

    void loadAuthenticatedUser()
      .then((nextUser) => {
        storeUserContext(nextUser);
        setUser(nextUser);
      })
      .catch(logout)
      .finally(() => setIsLoading(false));
  }, [logout]);

  const authenticate = useCallback(async (credentials: LoginRequest) => {
    const loginResponse = await login({ payload: credentials });
    const token = getToken(loginResponse.data);
    if (!token) throw new Error("The login response did not include an access token.");

    localStorage.setItem("access_token", token);
    const nextUser = await loadAuthenticatedUser();
    storeUserContext(nextUser);
    setUser(nextUser);
    return nextUser;
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    user,
    permissions: user?.permissions ?? [],
    scope: user?.scope ?? null,
    isLoading,
    login: authenticate,
    logout,
  }), [authenticate, isLoading, logout, user]);

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
