export type AuthScope = "global" | "organization";

export type AuthUser = {
  id: string;
  name: string;
  email: string;
  scope: AuthScope;
  organizationId?: string | null;
  role?: { key: string; name: string } | null;
  permissions: string[];
};

/** Completes sign-in for a verified external identity (mock/dev exchange). */
export type LoginRequest = {
  provider?: string;
  subject?: string;
  email?: string;
  displayName?: string;
};

export type OrganizationOption = {
  id: string;
  name: string;
};

export type OrganizationSelectionRequiredResponse = {
  status: "organizationSelectionRequired";
  organizations: OrganizationOption[];
  selectionTicket: string;
};

export type LoginResponse = AuthUser | OrganizationSelectionRequiredResponse;

export type SelectOrganizationRequest = {
  selectionTicket: string;
  organizationId: string;
};

export type CsrfResponse = {
  requestToken: string;
};

export function isOrganizationSelectionRequired(
  data: LoginResponse,
): data is OrganizationSelectionRequiredResponse {
  return (
    typeof data === "object"
    && data !== null
    && "status" in data
    && data.status === "organizationSelectionRequired"
  );
}

/** Browser navigation target that starts the configured OIDC challenge. */
export function getExternalLoginStartUrl(returnUrl = "/login/callback"): string {
  const base = (import.meta.env.VITE_API_URL ?? "").replace(/\/$/, "");
  const path = `/api/v1/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`;
  return `${base}${path}`;
}
