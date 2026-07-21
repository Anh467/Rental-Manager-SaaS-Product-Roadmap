import type { ReactNode } from "react";

export type PermissionMode = "all" | "any";

export function PermissionGuard({
  required,
  permissions,
  mode = "all",
  fallback = null,
  children,
}: {
  required: string | string[];
  permissions: readonly string[];
  mode?: PermissionMode;
  fallback?: ReactNode;
  children: ReactNode;
}) {
  const expected = Array.isArray(required) ? required : [required];
  const allowed = mode === "all"
    ? expected.every((permission) => permissions.includes(permission))
    : expected.some((permission) => permissions.includes(permission));

  return allowed ? children : fallback;
}
