import { useCallback, useMemo } from "react";
import { Link, getRouteApi } from "@tanstack/react-router";
import type { ColumnDef, PaginationState } from "@tanstack/react-table";
import { useTranslation } from "react-i18next";

import { usePropertiesQuery, type Property } from "@/api/routes/properties";
import { DataTable } from "@/components/common/data-table";
import { MobileDataCard } from "@/components/common/mobile-data-card";
import { ErrorState, PageContent, PageHeader, PageToolbar } from "@/components/common/page";
import { PermissionGuard } from "@/components/common/permission-guard";
import { SearchInput } from "@/components/common/search-input";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";

const routeApi = getRouteApi("/_authenticated/_standard/(tenant)/properties/");

export function PropertyListPage() {
  const { t } = useTranslation("property");
  const { t: commonT } = useTranslation("common");
  const search = routeApi.useSearch();
  const navigate = routeApi.useNavigate();
  const query = usePropertiesQuery({
    page: search.page,
    pageSize: search.pageSize,
    search: search.search || undefined,
    propertyTypeId: search.propertyTypeId || undefined,
  });
  const pagination: PaginationState = {
    pageIndex: search.page - 1,
    pageSize: search.pageSize,
  };

  const columns = useMemo<ColumnDef<Property>[]>(
    () => [
      { accessorKey: "name", header: t("columns.name") },
      { accessorKey: "code", header: t("columns.code") },
      { accessorKey: "address", header: t("columns.address") },
      { accessorKey: "totalFloors", header: t("columns.totalFloors") },
      {
        id: "actions",
        header: t("columns.actions"),
        cell: ({ row }) => (
          <Button asChild variant="ghost" size="sm">
            <Link to="/properties/$propertyId" params={{ propertyId: row.original.id }}>
              {commonT("actions.view")}
            </Link>
          </Button>
        ),
      },
    ],
    [commonT, t],
  );

  const handleSearch = useCallback(
    (value: string) => {
      void navigate({ search: (old) => ({ ...old, page: 1, search: value }) });
    },
    [navigate],
  );

  return (
    <PageContent>
      <PageHeader
        title={t("page.title")}
        description={t("page.description")}
        actions={(
          <PermissionGuard required="property.create">
            <Button>{t("page.add")}</Button>
          </PermissionGuard>
        )}
      />
      <PageToolbar>
        <SearchInput
          value={search.search}
          onSearch={handleSearch}
          placeholder={t("page.searchPlaceholder")}
          className="w-full sm:max-w-sm"
        />
      </PageToolbar>
      {query.isError ? (
        <ErrorState description={t("page.error")} onRetry={() => void query.refetch()} />
      ) : (
        <DataTable
          data={query.data?.items ?? []}
          columns={columns}
          loading={query.isPending}
          rowCount={query.data?.totalItems ?? 0}
          pagination={pagination}
          onPaginationChange={(updater) => {
            const next = typeof updater === "function" ? updater(pagination) : updater;
            void navigate({
              search: (old) => ({
                ...old,
                page: next.pageIndex + 1,
                pageSize: next.pageSize,
              }),
            });
          }}
          getRowId={(property) => property.id}
          emptyTitle={t("page.empty")}
          tableClassName="min-w-[760px]"
          renderMobileCard={(property) => (
            <MobileDataCard
              title={property.name}
              subtitle={property.code}
              status={(
                <Badge variant={property.isActive ? "success" : "muted"}>
                  {property.isActive ? t("detail.active") : t("detail.inactive")}
                </Badge>
              )}
              fields={[
                { label: t("columns.address"), value: property.address, fullWidth: true },
                { label: t("columns.totalFloors"), value: property.totalFloors },
                { label: t("detail.type"), value: property.propertyTypeName ?? property.propertyTypeId },
              ]}
              actions={(
                <Button asChild variant="outline">
                  <Link to="/properties/$propertyId" params={{ propertyId: property.id }}>
                    {commonT("actions.view")}
                  </Link>
                </Button>
              )}
            />
          )}
        />
      )}
    </PageContent>
  );
}
