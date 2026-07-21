import { useCallback } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import type { ColumnDef, PaginationState } from "@tanstack/react-table";

import {
  useRoomsQuery,
  type Room,
  type RoomStatus,
} from "@/api/routes/rooms";
import { DataTable } from "@/components/common/data-table";
import { PageContent, PageHeader } from "@/components/common/page";
import { SearchInput } from "@/components/common/search-input";
import { StatusBadge, type StatusDefinition } from "@/components/common/status-badge";
import { Button } from "@/components/ui/button";

const roomStatusDefinitions: Record<RoomStatus, StatusDefinition> = {
  vacant: { label: "Phòng trống", variant: "success" },
  occupied: { label: "Đang thuê", variant: "info" },
  maintenance: { label: "Bảo trì", variant: "warning" },
  inactive: { label: "Tạm ngưng", variant: "muted" },
};

const currencyFormatter = new Intl.NumberFormat("vi-VN", {
  style: "currency",
  currency: "VND",
  maximumFractionDigits: 0,
});

const columns: ColumnDef<Room>[] = [
  { accessorKey: "code", header: "Phòng" },
  { accessorKey: "propertyName", header: "Khu nhà" },
  { accessorKey: "floor", header: "Tầng" },
  { accessorKey: "capacity", header: "Sức chứa" },
  {
    accessorKey: "monthlyRent",
    header: "Giá thuê",
    cell: ({ row }) => currencyFormatter.format(row.original.monthlyRent),
  },
  {
    accessorKey: "status",
    header: "Trạng thái",
    cell: ({ row }) => (
      <StatusBadge status={row.original.status} definitions={roomStatusDefinitions} />
    ),
  },
];

export function RoomListPage() {
  const search = useSearch({ from: "/rooms/" });
  const navigate = useNavigate({ from: "/rooms/" });
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

  const handleSearch = useCallback(
    (value: string) => {
      void navigate({ search: (old) => ({ ...old, page: 1, search: value }) });
    },
    [navigate],
  );

  return (
    <PageContent className="p-6">
      <PageHeader
        title="Phòng"
        description="URL giữ filter/pagination; TanStack Query giữ server cache."
        actions={<Button>Thêm phòng</Button>}
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <SearchInput
          value={search.search}
          onSearch={handleSearch}
          placeholder="Tìm mã phòng hoặc khu nhà..."
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
        emptyTitle="Chưa có phòng"
      />
    </PageContent>
  );
}
