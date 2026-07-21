import { useCallback } from "react";
import { Link, useNavigate, useSearch } from "@tanstack/react-router";
import type { ColumnDef, PaginationState } from "@tanstack/react-table";

import {
  usePropertiesQuery,
  type Property,
} from "@/api/routes/properties";
import { DataTable } from "@/components/common/data-table";
import { ErrorState, PageContent, PageHeader } from "@/components/common/page";
import { PermissionGuard } from "@/components/common/permission-guard";
import { SearchInput } from "@/components/common/search-input";
import { Button } from "@/components/ui/button";

const columns: ColumnDef<Property>[] = [
  { accessorKey: "name", header: "Tên khu nhà" },
  { accessorKey: "code", header: "Mã" },
  { accessorKey: "address", header: "Địa chỉ" },
  { accessorKey: "totalFloors", header: "Số tầng" },
  {
    id: "actions",
    header: "",
    cell: ({ row }) => (
      <Button asChild variant="ghost" size="sm">
        <Link to="/properties/$propertyId" params={{ propertyId: row.original.id }}>Xem</Link>
      </Button>
    ),
  },
];

export function PropertyListPage() {
  const search = useSearch({ from: "/properties" });
  const navigate = useNavigate({ from: "/properties" });
  const query = usePropertiesQuery({
    page: search.page,
    pageSize: search.pageSize,
    search: search.search || undefined,
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

  if (query.isError) {
    return <ErrorState description="Không thể tải danh sách khu nhà." onRetry={() => void query.refetch()} />;
  }

  return (
    <PageContent className="p-6">
      <PageHeader
        title="Khu nhà"
        description="URL giữ filter/pagination; TanStack Query giữ server cache."
        actions={(
          <PermissionGuard required="property.create">
            <Button>Thêm khu nhà</Button>
          </PermissionGuard>
        )}
      />

      <SearchInput
        value={search.search}
        onSearch={handleSearch}
        placeholder="Tìm tên, mã hoặc địa chỉ khu nhà..."
        className="w-full sm:max-w-sm"
      />

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
        emptyTitle="Chưa có khu nhà"
      />
    </PageContent>
  );
}
