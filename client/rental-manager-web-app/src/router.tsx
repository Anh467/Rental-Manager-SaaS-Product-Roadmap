import {
  Outlet,
  createRootRouteWithContext,
  createRoute,
  createRouter,
} from "@tanstack/react-router";
import type { QueryClient } from "@tanstack/react-query";
import { z } from "zod";

import App from "@/App";
import { propertyQueries } from "@/features/properties/api";
import { PropertyListPage } from "@/features/properties/pages/property-list-page";

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

const propertySearchSchema = z.object({
  page: z.coerce.number().int().positive().catch(1),
  pageSize: z.coerce.number().int().min(10).max(100).catch(20),
  search: z.string().catch(""),
});

const propertiesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/properties",
  validateSearch: propertySearchSchema,
  loaderDeps: ({ search }) => search,
  loader: ({ context, deps }) =>
    context.queryClient.ensureQueryData(
      propertyQueries.list({
        page: deps.page,
        pageSize: deps.pageSize,
        search: deps.search || undefined,
      }),
    ),
  component: PropertyListPage,
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

const routeTree = rootRoute.addChildren([indexRoute, propertiesRoute, propertyRoute]);

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
