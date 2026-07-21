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
