import { queryClient } from "@/api/query-client";
import { router } from "@/router";

const ORGANIZATION_STORAGE_KEY = "organization_id";

export function getOrganizationId() {
  return localStorage.getItem(ORGANIZATION_STORAGE_KEY);
}

export async function setOrganizationId(organizationId: string | null) {
  if (organizationId) {
    localStorage.setItem(ORGANIZATION_STORAGE_KEY, organizationId);
  } else {
    localStorage.removeItem(ORGANIZATION_STORAGE_KEY);
  }

  queryClient.clear();
  await router.invalidate();
}
