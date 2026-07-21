import type { ReactNode } from "react";
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

import { EmptyState, LoadingState } from "@/components/common/page";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

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
  renderMobileCard?: (row: TData) => ReactNode;
  tableClassName?: string;
  className?: string;
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
  renderMobileCard,
  tableClassName,
  className,
}: DataTableProps<TData>) {
  const { t } = useTranslation("common");
  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: Boolean(pagination),
    rowCount,
    state: {
      ...(pagination ? { pagination } : {}),
      ...(rowSelection ? { rowSelection } : {}),
    },
    onPaginationChange,
    onRowSelectionChange,
    getRowId,
  });

  if (loading) return <LoadingState />;

  const rows = table.getRowModel().rows;
  const emptyState = (
    <EmptyState
      title={emptyTitle ?? t("state.noData")}
      className="min-h-40 border-none sm:min-h-[200px]"
    />
  );

  return (
    <div className={cn("min-w-0 space-y-4", className)}>
      {renderMobileCard ? (
        <div
          className="grid gap-3 md:hidden"
          aria-label={t("state.mobileList")}
          data-testid="mobile-data-list"
        >
          {rows.length > 0 ? rows.map((row) => (
            <div key={row.id} className="min-w-0">
              {renderMobileCard(row.original)}
            </div>
          )) : emptyState}
        </div>
      ) : null}

      <div
        className={cn("min-w-0 overflow-hidden rounded-md border bg-background", renderMobileCard && "hidden md:block")}
        data-testid="desktop-data-table"
      >
        <div className="w-full overflow-x-auto overscroll-x-contain">
          <table className={cn("w-full min-w-max text-sm", tableClassName)}>
            <thead className="bg-muted/50">
              {table.getHeaderGroups().map((group) => (
                <tr key={group.id}>
                  {group.headers.map((header) => (
                    <th
                      key={header.id}
                      className="h-11 whitespace-nowrap px-4 text-left font-medium text-muted-foreground"
                    >
                      {header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext())}
                    </th>
                  ))}
                </tr>
              ))}
            </thead>
            <tbody>
              {rows.length > 0 ? (
                rows.map((row) => (
                  <tr
                    key={row.id}
                    data-state={row.getIsSelected() ? "selected" : undefined}
                    className="border-t hover:bg-muted/40 data-[state=selected]:bg-muted"
                  >
                    {row.getVisibleCells().map((cell) => (
                      <td key={cell.id} className="max-w-md px-4 py-3 align-middle">
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </td>
                    ))}
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={Math.max(table.getVisibleLeafColumns().length, 1)} className="p-4">
                    {emptyState}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {pagination ? (
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-center text-sm text-muted-foreground sm:text-left">
            {t("state.page", { current: pagination.pageIndex + 1, total: Math.max(table.getPageCount(), 1) })}
          </p>
          <div className="grid grid-cols-2 gap-2 sm:flex">
            <Button
              variant="outline"
              className="min-h-11 w-full sm:min-h-9 sm:w-auto"
              size="sm"
              disabled={!table.getCanPreviousPage()}
              onClick={() => table.previousPage()}
            >
              {t("state.previous")}
            </Button>
            <Button
              variant="outline"
              className="min-h-11 w-full sm:min-h-9 sm:w-auto"
              size="sm"
              disabled={!table.getCanNextPage()}
              onClick={() => table.nextPage()}
            >
              {t("state.next")}
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
