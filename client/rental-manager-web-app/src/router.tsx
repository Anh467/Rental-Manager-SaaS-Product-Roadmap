import {
  Outlet,
  createRootRouteWithContext,
  createRoute,
  createRouter,
} from "@tanstack/react-router";
import type { QueryClient } from "@tanstack/react-query";

import App from "@/App";
import { propertyQueries } from "@/features/properties/api";

export type RouterContext = { queryClient: QueryClient };

const rootRoute = createRootRouteWithContext<RouterContext>()({
  component: () => <Outlet />,
  notFoundComponent: () => <div className="p-8">Không tìm thấy trang.</div>,
});

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: App,
});

const propertyRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/properties/$propertyId",
  loader: ({ context, params }) =>
    context.queryClient.ensureQueryData(propertyQueries.detail(params.propertyId)),
  component: PropertyDetailPage,
});

function PropertyDetailPage() {
  return <div className="p-8">Property detail baseline loaded by route.</div>;
}

const routeTree = rootRoute.addChildren([indexRoute, propertyRoute]);

export const router = createRouter({
  routeTree,
  context: { queryClient: undefined! },
  defaultPreload: "intent",
  defaultPreloadStaleTime: 0,
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
