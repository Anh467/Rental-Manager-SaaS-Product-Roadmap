import { Outlet, createFileRoute, redirect } from "@tanstack/react-router";

import { getMe } from "@/api/routes/auth";
import { isApiError } from "@/api/client";

export const Route = createFileRoute("/_authenticated")({
  beforeLoad: async ({ location }) => {
    if (!localStorage.getItem("access_token")) {
      throw redirect({ to: "/login", replace: true });
    }
    if (location.pathname === "/access-denied") return;
    try {
      await getMe();
    } catch (error) {
      if (isApiError(error) && error.status === 403) {
        throw redirect({ to: "/access-denied", replace: true });
      }
      throw redirect({ to: "/login", replace: true });
    }
  },
  component: AuthenticatedLayout,
});

function AuthenticatedLayout() {
  return <Outlet />;
}
