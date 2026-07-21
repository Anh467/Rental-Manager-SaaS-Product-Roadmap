import { Outlet, createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/_authenticated")({
  component: AuthenticatedLayout,
});

function AuthenticatedLayout() {
  // Authentication validation belongs in beforeLoad once Identity is connected.
  return <Outlet />;
}
