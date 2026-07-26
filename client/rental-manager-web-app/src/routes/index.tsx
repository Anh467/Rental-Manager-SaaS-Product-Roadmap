import { createFileRoute, redirect, isRedirect } from "@tanstack/react-router";
import { getMe } from "@/api/routes/auth";
import { hasPermission } from "@/components/common/permission-guard";

export const Route = createFileRoute("/")({
  beforeLoad: async () => {
    if (!localStorage.getItem("access_token")) {
      throw redirect({ to: "/login", replace: true });
    }

    try {
      const user = (await getMe()).data;
      if (user.scope === "global" && hasPermission(user.permissions, "global_field_view")) {
        throw redirect({
          to: "/global/fields",
          search: { page: 1, pageSize: 20, search: "", sortBy: "" },
          replace: true,
        });
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
      throw redirect({ to: "/login", replace: true });
    }
  },
  component: () => null,
});
