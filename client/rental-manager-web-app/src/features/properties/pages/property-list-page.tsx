import { useQuery } from "@tanstack/react-query";
import { Link, useNavigate, useSearch } from "@tanstack/react-router";
import type { ColumnDef, PaginationState } from "@tanstack/react-table";

import { DataTable } from "@/components/common/data-table";
import { ErrorState, PageContent, PageHeader } from "@/components/common/page";
import { Button } from "@/components/ui/button";
import { propertyQueries, type Property } from "@/features/properties/api";

const columns: ColumnDef<Property>[] = [
  { accessorKey: "name", header: "Tên khu nhà" },
  { accessorKey: "code", header: "Mã" },
  { accessorKey: "address", header: "Địa chỉ" },
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
  const request = { page: search.page, pageSize: search.pageSize, search: search.search || undefined };
  const query = useQuery(propertyQueries.list(request));

  const pagination: PaginationState = {
    pageIndex: search.page - 1,
    pageSize: search.pageSize,
  };

  if (query.isError) {
    return <ErrorState description="Không thể tải danh sách khu nhà." onRetry={() => query.refetch()} />;
  }

  return (
    <PageContent className="p-6">
      <PageHeader title="Khu nhà" description="Baseline list: URL giữ pagination, TanStack Query giữ cache." actions={<Button>Thêm khu nhà</Button>} />
      <DataTable
        data={query.data?.items ?? []}
        columns={columns}
        loading={query.isPending}
        rowCount={query.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={(updater) => {
          const next = typeof updater === "function" ? updater(pagination) : updater;
          void navigate({ search: (old) => ({ ...old, page: next.pageIndex + 1, pageSize: next.pageSize }) });
        }}
        getRowId={(row) => row.id}
        emptyTitle="Chưa có khu nhà"
      />
    </PageContent>
  );
}
