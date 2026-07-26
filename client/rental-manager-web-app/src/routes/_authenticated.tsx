import { Outlet, createFileRoute, redirect } from "@tanstack/react-router";

import { ensureAuthenticatedUser } from "@/features/auth/auth-session";

export const Route = createFileRoute("/_authenticated")({
  beforeLoad: async ({ context, location }) => {
    if (!localStorage.getItem("access_token")) {
      throw redirect({ to: "/login", replace: true });
    }
    if (location.pathname === "/access-denied") return;

    await ensureAuthenticatedUser(context.queryClient);
  },
  component: AuthenticatedLayout,
});

function AuthenticatedLayout() {
  return <Outlet />;
}
