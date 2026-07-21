import {
  createContext,
  useContext,
  useMemo,
  type ReactNode,
} from "react";

export type PermissionMode = "all" | "any";

const PermissionContext = createContext<readonly string[]>([]);

export function PermissionProvider({
  permissions,
  children,
}: {
  permissions: readonly string[];
  children: ReactNode;
}) {
  const value = useMemo(() => [...permissions], [permissions]);
  return <PermissionContext.Provider value={value}>{children}</PermissionContext.Provider>;
}

export function usePermissions() {
  return useContext(PermissionContext);
}

export function hasPermission(
  permissions: readonly string[],
  required: string | readonly string[],
  mode: PermissionMode = "all",
) {
  const expected = typeof required === "string" ? [required] : required;
  return mode === "all"
    ? expected.every((permission) => permissions.includes(permission))
    : expected.some((permission) => permissions.includes(permission));
}

export function PermissionGuard({
  required,
  permissions: permissionOverride,
  mode = "all",
  fallback = null,
  children,
}: {
  required: string | readonly string[];
  permissions?: readonly string[];
  mode?: PermissionMode;
  fallback?: ReactNode;
  children: ReactNode;
}) {
  const contextPermissions = usePermissions();
  const permissions = permissionOverride ?? contextPermissions;
  return hasPermission(permissions, required, mode) ? children : fallback;
}
