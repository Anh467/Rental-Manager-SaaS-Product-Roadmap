import { queryClient } from "@/api/query-client";
import { router } from "@/router";

/**
 * Organization context is established by the authenticated cookie session (/me).
 * Clients must not persist or send organization_id from localStorage.
 */
export async function refreshTenantContext() {
  queryClient.clear();
  await router.invalidate();
}
