import React from "react";
import ReactDOM from "react-dom/client";
import { QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { RouterProvider } from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";

import { queryClient } from "@/api/query-client";
import { router } from "@/router";
import "@/index.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} context={{ queryClient }} />
      {import.meta.env.DEV ? (
        <>
          <ReactQueryDevtools initialIsOpen={false} />
          <TanStackRouterDevtools router={router} position="bottom-right" />
        </>
      ) : null}
    </QueryClientProvider>
  </React.StrictMode>,
);
