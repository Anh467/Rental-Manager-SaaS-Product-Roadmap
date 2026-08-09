import { Outlet, createFileRoute } from "@tanstack/react-router";

import { ensureAuthenticatedUser } from "@/features/auth/auth-session";

export const Route = createFileRoute("/_authenticated")({
  beforeLoad: async ({ context, location }) => {
    if (location.pathname === "/access-denied") return;

    await ensureAuthenticatedUser(context.queryClient);
  },
  component: AuthenticatedLayout,
});

function AuthenticatedLayout() {
  return <Outlet />;
}
