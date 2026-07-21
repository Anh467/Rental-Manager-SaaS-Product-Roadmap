import { Outlet, createFileRoute } from "@tanstack/react-router";

import { AppShell } from "@/components/common/app-shell";

export const Route = createFileRoute("/_authenticated/_standard")({
  component: StandardLayout,
});

function StandardLayout() {
  return (
    <AppShell>
      <Outlet />
    </AppShell>
  );
}
