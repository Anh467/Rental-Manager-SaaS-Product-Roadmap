import { createFileRoute } from "@tanstack/react-router";
import { z } from "zod";

import { globalFieldQueries } from "@/api/routes/global-fields";
import { GlobalFieldListPage } from "@/features/global-fields/pages/global-field-list-page";

const searchSchema = z.object({
  page: z.coerce.number().int().positive().catch(1),
  pageSize: z.coerce.number().int().min(10).max(100).catch(20),
  search: z.string().catch(""),
  fieldTypeId: z.coerce
    .number()
    .pipe(z.union([z.literal(1), z.literal(2), z.literal(4), z.literal(6)]))
    .optional()
    .catch(undefined),
  isActive: z
    .union([z.boolean(), z.literal("true"), z.literal("false")])
    .transform((value) => value === true || value === "true")
    .optional()
    .catch(undefined),
  sortBy: z.string().catch(""),
  sortDirection: z.enum(["asc", "desc"]).optional().catch(undefined),
});

export const Route = createFileRoute("/_authenticated/_standard/global/fields/")({
  validateSearch: (search) => searchSchema.parse(search),
  loaderDeps: ({ search }) => search,
  loader: ({ context, deps }) => context.queryClient.ensureQueryData(globalFieldQueries.list({
    page: deps.page,
    pageNumber: deps.page,
    pageSize: deps.pageSize,
    search: deps.search || undefined,
    fieldTypeId: deps.fieldTypeId,
    isActive: deps.isActive,
    sortBy: deps.sortBy || undefined,
    sortDirection: deps.sortDirection,
  })),
  component: GlobalFieldListPage,
});
