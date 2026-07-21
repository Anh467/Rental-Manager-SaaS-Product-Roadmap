import type { QueryClient } from "@tanstack/react-query";
import {
  Outlet,
  createRootRouteWithContext,
  createRoute,
  createRouter,
} from "@tanstack/react-router";
import { z } from "zod";

import App from "@/App";
import { propertyQueries } from "@/api/routes/properties";
import { roomQueries } from "@/api/routes/rooms";
import { AppShell } from "@/components/common/app-shell";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { PropertyListPage } from "@/features/properties/pages/property-list-page";
import { RoomListPage } from "@/features/rooms/pages/room-list-page";

export type RouterContext = { queryClient: QueryClient };

const rootRoute = createRootRouteWithContext<RouterContext>()({
  component: RootLayout,
  notFoundComponent: () => <div className="p-8">Không tìm thấy trang.</div>,
});

function RootLayout() {
  return (
    <AppShell>
      <Outlet />
    </AppShell>
  );
}

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
  const property = propertyRoute.useLoaderData();

  return (
    <div className="p-6">
      <Card>
        <CardHeader>
          <CardTitle>{property.name}</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-2 text-sm sm:grid-cols-2">
          <p><span className="text-muted-foreground">Mã:</span> {property.code}</p>
          <p><span className="text-muted-foreground">Số tầng:</span> {property.totalFloors}</p>
          <p className="sm:col-span-2"><span className="text-muted-foreground">Địa chỉ:</span> {property.address}</p>
        </CardContent>
      </Card>
    </div>
  );
}

const roomSearchSchema = z.object({
  page: z.coerce.number().int().positive().catch(1),
  pageSize: z.coerce.number().int().min(10).max(100).catch(20),
  search: z.string().catch(""),
  propertyId: z.string().catch(""),
  status: z.enum(["vacant", "occupied", "maintenance", "inactive"]).optional().catch(undefined),
});

const roomsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/rooms",
  validateSearch: roomSearchSchema,
  loaderDeps: ({ search }) => search,
  loader: ({ context, deps }) =>
    context.queryClient.ensureQueryData(
      roomQueries.list({
        page: deps.page,
        pageSize: deps.pageSize,
        search: deps.search || undefined,
        propertyId: deps.propertyId || undefined,
        status: deps.status,
      }),
    ),
  component: RoomListPage,
});

const routeTree = rootRoute.addChildren([
  indexRoute,
  propertiesRoute,
  propertyRoute,
  roomsRoute,
]);

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
