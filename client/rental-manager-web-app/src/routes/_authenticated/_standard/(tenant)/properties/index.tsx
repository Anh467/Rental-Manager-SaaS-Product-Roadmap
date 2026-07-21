import { createFileRoute } from "@tanstack/react-router";
import { z } from "zod";

import { propertyQueries } from "@/api/routes/properties";
import { PropertyListPage } from "@/features/properties/pages/property-list-page";

const searchSchema = z.object({
  page: z.coerce.number().int().positive().catch(1),
  pageSize: z.coerce.number().int().min(10).max(100).catch(20),
  search: z.string().catch(""),
  propertyTypeId: z.string().catch(""),
});

export const Route = createFileRoute(
  "/_authenticated/_standard/(tenant)/properties/",
)({
  validateSearch: (search) => searchSchema.parse(search),
  loaderDeps: ({ search }) => search,
  loader: ({ context, deps }) =>
    context.queryClient.ensureQueryData(
      propertyQueries.list({
        page: deps.page,
        pageSize: deps.pageSize,
        search: deps.search || undefined,
        propertyTypeId: deps.propertyTypeId || undefined,
      }),
    ),
  component: PropertyListPage,
});
