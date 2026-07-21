import { createFileRoute } from "@tanstack/react-router";
import { z } from "zod";

import { roomQueries } from "@/api/routes/rooms";
import { RoomListPage } from "@/features/rooms/pages/room-list-page";

const searchSchema = z.object({
  page: z.coerce.number().int().positive().catch(1),
  pageSize: z.coerce.number().int().min(10).max(100).catch(20),
  search: z.string().catch(""),
  propertyId: z.string().catch(""),
  status: z.enum(["vacant", "occupied", "maintenance", "inactive"]).optional().catch(undefined),
});

export const Route = createFileRoute(
  "/_authenticated/_standard/(tenant)/rooms/",
)({
  validateSearch: (search) => searchSchema.parse(search),
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
