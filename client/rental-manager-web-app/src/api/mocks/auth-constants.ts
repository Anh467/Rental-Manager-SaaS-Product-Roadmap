import type { AuthUser } from "@/api/routes/auth";

export const MOCK_ACCESS_TOKEN = "mock-access-token";
export const MOCK_ORGANIZATION_ID = "org-mock-1";

export const mockAuthUser: AuthUser = {
  id: "user-mock-1",
  name: "Mock Organization Admin",
  email: "mock.admin@example.com",
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
