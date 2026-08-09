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

export type LoginRequest = {
  email: string;
  password: string;
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
