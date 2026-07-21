import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
  type OnChangeFn,
  type PaginationState,
  type RowSelectionState,
} from "@tanstack/react-table";
import { useTranslation } from "react-i18next";

import { Button } from "@/components/ui/button";
import { EmptyState, LoadingState } from "@/components/common/page";

export type DataTableProps<TData> = {
  data: TData[];
  columns: ColumnDef<TData, unknown>[];
  loading?: boolean;
  rowCount?: number;
  pagination?: PaginationState;
  onPaginationChange?: OnChangeFn<PaginationState>;
  rowSelection?: RowSelectionState;
  onRowSelectionChange?: OnChangeFn<RowSelectionState>;
  getRowId?: (row: TData) => string;
  emptyTitle?: string;
};

export function DataTable<TData>({
  data,
  columns,
  loading,
  rowCount,
  pagination,
  onPaginationChange,
  rowSelection,
  onRowSelectionChange,
  getRowId,
  emptyTitle,
}: DataTableProps<TData>) {
  const { t } = useTranslation("common");
  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: Boolean(pagination),
    rowCount,
    state: { pagination, rowSelection },
    onPaginationChange,
    onRowSelectionChange,
    getRowId,
  });

  if (loading) return <LoadingState />;
  if (!data.length) return <EmptyState title={emptyTitle ?? t("state.noData")} />;

  return (
    <div className="space-y-4">
      <div className="overflow-hidden rounded-md border">
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            {table.getHeaderGroups().map((group) => (
              <tr key={group.id}>
                {group.headers.map((header) => (
                  <th key={header.id} className="h-11 px-4 text-left font-medium text-muted-foreground">
                    {header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext())}
                  </th>
                ))}
              </tr>
            ))}
          </thead>
          <tbody>
            {table.getRowModel().rows.map((row) => (
              <tr key={row.id} data-state={row.getIsSelected() ? "selected" : undefined} className="border-t hover:bg-muted/40 data-[state=selected]:bg-muted">
                {row.getVisibleCells().map((cell) => (
                  <td key={cell.id} className="px-4 py-3 align-middle">
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {pagination ? (
        <div className="flex items-center justify-between gap-4">
          <p className="text-sm text-muted-foreground">
            {t("state.page", { current: pagination.pageIndex + 1, total: Math.max(table.getPageCount(), 1) })}
          </p>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" disabled={!table.getCanPreviousPage()} onClick={() => table.previousPage()}>
              {t("state.previous")}
            </Button>
            <Button variant="outline" size="sm" disabled={!table.getCanNextPage()} onClick={() => table.nextPage()}>
              {t("state.next")}
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
