import { createFileRoute, redirect, isRedirect } from "@tanstack/react-router";

import { hasPermission } from "@/components/common/permission-guard";
import { ensureAuthenticatedUser } from "@/features/auth/auth-session";

export const Route = createFileRoute("/")({
  beforeLoad: async ({ context }) => {
    try {
      const user = await ensureAuthenticatedUser(context.queryClient);

      if (user.scope === "global") {
        if (hasPermission(user.permissions, "global_field_view")) {
          throw redirect({
            to: "/global/fields",
            search: { page: 1, pageSize: 20, search: "", sortBy: "" },
            replace: true,
          });
        }

        throw redirect({ to: "/access-denied", replace: true });
      }

      throw redirect({
        to: "/properties",
        search: {
          page: 1,
          pageSize: 20,
          search: "",
          propertyTypeId: "",
        },
        replace: true,
      });
    } catch (error) {
      if (isRedirect(error)) throw error;
      throw error;
    }
  },
  component: () => null,
});
