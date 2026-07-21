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

bootstrap().catch((error: unknown) => {
  console.error("Failed to bootstrap application", error);
  const root = document.getElementById("root");
  if (root) {
    root.innerHTML = [
      '<main style="min-height:100vh;display:grid;place-items:center;padding:24px;font-family:system-ui,sans-serif">',
      '<div style="max-width:560px;border:1px solid #e2e8f0;border-radius:8px;padding:24px">',
      '<h1 style="font-size:20px;margin:0 0 8px">Unable to start the application</h1>',
      '<p style="margin:0;color:#475569">Không thể khởi động ứng dụng. Vui lòng tải lại trang hoặc kiểm tra cấu hình môi trường.</p>',
      "</div></main>",
    ].join("");
  }
});
