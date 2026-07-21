import { useCallback, useMemo } from "react";
import { getRouteApi } from "@tanstack/react-router";
import type { ColumnDef, PaginationState } from "@tanstack/react-table";
import { useTranslation } from "react-i18next";

import { useRoomsQuery, type Room, type RoomStatus } from "@/api/routes/rooms";
import { DataTable } from "@/components/common/data-table";
import { ErrorState, PageContent, PageHeader } from "@/components/common/page";
import { SearchInput } from "@/components/common/search-input";
import { StatusBadge, type StatusDefinition } from "@/components/common/status-badge";
import { Button } from "@/components/ui/button";

const routeApi = getRouteApi("/_authenticated/_standard/(tenant)/rooms/");

export function RoomListPage() {
  const { t, i18n } = useTranslation("room");
  const search = routeApi.useSearch();
  const navigate = routeApi.useNavigate();
  const query = useRoomsQuery({
    page: search.page,
    pageSize: search.pageSize,
    search: search.search || undefined,
    propertyId: search.propertyId || undefined,
    status: search.status || undefined,
  });
  const pagination: PaginationState = {
    pageIndex: search.page - 1,
    pageSize: search.pageSize,
  };

  const statusDefinitions = useMemo<Record<RoomStatus, StatusDefinition>>(
    () => ({
      vacant: { label: t("status.vacant"), variant: "success" },
      occupied: { label: t("status.occupied"), variant: "info" },
      maintenance: { label: t("status.maintenance"), variant: "warning" },
      inactive: { label: t("status.inactive"), variant: "muted" },
    }),
    [t],
  );

  const currencyFormatter = useMemo(
    () => new Intl.NumberFormat(i18n.resolvedLanguage === "vi" ? "vi-VN" : "en-US", {
      style: "currency",
      currency: "VND",
      maximumFractionDigits: 0,
    }),
    [i18n.resolvedLanguage],
  );

  const columns = useMemo<ColumnDef<Room>[]>(
    () => [
      { accessorKey: "code", header: t("columns.code") },
      { accessorKey: "propertyName", header: t("columns.propertyName") },
      { accessorKey: "floor", header: t("columns.floor") },
      { accessorKey: "capacity", header: t("columns.capacity") },
      {
        accessorKey: "monthlyRent",
        header: t("columns.monthlyRent"),
        cell: ({ row }) => currencyFormatter.format(row.original.monthlyRent),
      },
      {
        accessorKey: "status",
        header: t("columns.status"),
        cell: ({ row }) => (
          <StatusBadge status={row.original.status} definitions={statusDefinitions} />
        ),
      },
    ],
    [currencyFormatter, statusDefinitions, t],
  );

  const handleSearch = useCallback(
    (value: string) => {
      void navigate({ search: (old) => ({ ...old, page: 1, search: value }) });
    },
    [navigate],
  );

  if (query.isError) {
    return <ErrorState description={t("page.error")} onRetry={() => void query.refetch()} />;
  }

  return (
    <PageContent className="p-6">
      <PageHeader
        title={t("page.title")}
        description={t("page.description")}
        actions={<Button>{t("page.add")}</Button>}
      />
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <SearchInput
          value={search.search}
          onSearch={handleSearch}
          placeholder={t("page.searchPlaceholder")}
          className="w-full sm:max-w-sm"
        />
      </div>
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
        getRowId={(room) => room.id}
        emptyTitle={t("page.empty")}
      />
    </PageContent>
  );
}
