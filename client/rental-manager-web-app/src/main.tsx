import React from "react";
import ReactDOM from "react-dom/client";
import { QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { RouterProvider } from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";
import { Toaster } from "sonner";

import { queryClient } from "@/api/query-client";
import { PermissionProvider } from "@/components/common/permission-guard";
import { TooltipProvider } from "@/components/ui/tooltip";
import { initializeI18n } from "@/i18n";
import { router } from "@/router";
import "@/index.css";

const baselinePermissions = [
  "property.view",
  "property.create",
  "property.edit",
  "room.view",
  "room.create",
  "room.edit",
] as const;

function shouldEnableMockApi() {
  const configured = import.meta.env.VITE_ENABLE_MOCK_API;
  if (configured === "true") return true;
  if (configured === "false") return false;
  return import.meta.env.DEV;
}

async function bootstrap() {
  await initializeI18n();

  // Route loaders run as soon as RouterProvider mounts. Register the mock
  // adapter first so the initial loader never falls through to the real API.
  if (shouldEnableMockApi()) {
    const { startMockApi } = await import("@/api/mocks");
    startMockApi();
  }

  ReactDOM.createRoot(document.getElementById("root")!).render(
    <React.StrictMode>
      <QueryClientProvider client={queryClient}>
        <PermissionProvider permissions={baselinePermissions}>
          <TooltipProvider delayDuration={300}>
            <RouterProvider router={router} context={{ queryClient }} />
            <Toaster richColors position="top-right" />
            {import.meta.env.DEV ? (
              <>
                <ReactQueryDevtools initialIsOpen={false} />
                <TanStackRouterDevtools router={router} position="bottom-right" />
              </>
            ) : null}
          </TooltipProvider>
        </PermissionProvider>
      </QueryClientProvider>
    </React.StrictMode>,
  );
}

void bootstrap();
