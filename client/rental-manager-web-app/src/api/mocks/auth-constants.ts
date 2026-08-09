import type { AuthUser, OrganizationOption } from "@/api/routes/auth";

export const MOCK_ORGANIZATION_ID = "org-mock-1";
export const MOCK_ORGANIZATION_ID_B = "org-mock-2";
export const MOCK_SESSION_COOKIE = "rm_mock_session";

/** Default single-membership mock credentials for Playwright / local demos. */
export const MOCK_LOGIN_EMAIL = "mock.admin@example.com";
export const MOCK_LOGIN_PASSWORD = "Password123!";

/** Multi-membership mock credentials that require organization selection. */
export const MOCK_MULTI_ORG_EMAIL = "multi.admin@example.com";
export const MOCK_MULTI_ORG_PASSWORD = "Password123!";

export const mockOrganizations: OrganizationOption[] = [
  { id: MOCK_ORGANIZATION_ID, name: "Minh Anh Boarding House" },
  { id: MOCK_ORGANIZATION_ID_B, name: "Second Organization" },
];

export const mockAuthUser: AuthUser = {
  id: "user-mock-1",
  name: "Mock Organization Admin",
  email: MOCK_LOGIN_EMAIL,
  scope: "organization",
  organizationId: MOCK_ORGANIZATION_ID,
  role: { key: "org_admin", name: "Organization Admin" },
  permissions: [
    "property.create",
    "property.edit",
    "property.delete",
    "property.view",
    "room.create",
    "room.edit",
    "room.delete",
    "room.view",
  ],
};

export function buildMockAuthUser(organizationId: string, email = MOCK_LOGIN_EMAIL): AuthUser {
  const organization = mockOrganizations.find((item) => item.id === organizationId);
  return {
    ...mockAuthUser,
    email,
    organizationId,
    name: organization
      ? `Mock Admin (${organization.name})`
      : mockAuthUser.name,
  };
}
