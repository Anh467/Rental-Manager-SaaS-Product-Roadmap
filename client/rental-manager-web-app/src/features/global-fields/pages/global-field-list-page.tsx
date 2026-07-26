import { useCallback, useMemo, useState } from "react";
import { getRouteApi } from "@tanstack/react-router";
import type { ColumnDef, PaginationState } from "@tanstack/react-table";
import { useTranslation } from "react-i18next";

import { useGlobalFieldTypesQuery } from "@/api/routes/global-field-types";
import {
  useChangeGlobalFieldStatusMutation,
  useCreateGlobalFieldMutation,
  useDeleteGlobalFieldMutation,
  useGlobalFieldsQuery,
  useUpdateGlobalFieldMutation,
  type GlobalField,
} from "@/api/routes/global-fields";
import { isApiError } from "@/api/client";
import { ConfirmDialog } from "@/components/common/confirm-dialog";
import { DataTable } from "@/components/common/data-table";
import { MobileDataCard } from "@/components/common/mobile-data-card";
import { ErrorState, PageContent, PageHeader, PageToolbar } from "@/components/common/page";
import { PermissionGuard } from "@/components/common/permission-guard";
import { SearchInput } from "@/components/common/search-input";
import { StatusBadge } from "@/components/common/status-badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { GlobalFieldForm } from "../components/global-field-form";
import { getFieldTypeName } from "../lib/field-type-options";

const routeApi = getRouteApi("/_authenticated/_standard/global/fields/");
type DialogMode = "create" | "view" | "edit";

export function GlobalFieldListPage() {
  const { t, i18n } = useTranslation("global-field");
  const { t: commonT } = useTranslation("common");
  const search = routeApi.useSearch();
  const navigate = routeApi.useNavigate();
  const [selected, setSelected] = useState<GlobalField | undefined>();
  const [mode, setMode] = useState<DialogMode>("view");
  const [hasRowVersionConflict, setHasRowVersionConflict] = useState(false);
  const fieldTypesQuery = useGlobalFieldTypesQuery();
  const fieldTypes = fieldTypesQuery.data ?? [];
  const query = useGlobalFieldsQuery({
    page: search.page,
    pageNumber: search.page,
    pageSize: search.pageSize,
    search: search.search || undefined,
    fieldTypeId: search.fieldTypeId || undefined,
    isActive: search.isActive,
    sortBy: search.sortBy || undefined,
    sortDirection: search.sortDirection || undefined,
  });
  const createMutation = useCreateGlobalFieldMutation();
  const statusMutation = useChangeGlobalFieldStatusMutation();
  const deleteMutation = useDeleteGlobalFieldMutation();
  const updateMutation = useUpdateGlobalFieldMutation(selected?.id ?? "");
  const pagination: PaginationState = { pageIndex: search.page - 1, pageSize: search.pageSize };
  const dateFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat(i18n.resolvedLanguage === "vi" ? "vi-VN" : "en-US", {
        dateStyle: "medium",
      }),
    [i18n.resolvedLanguage],
  );

  const open = (nextMode: DialogMode, field?: GlobalField) => {
    setMode(nextMode);
    setSelected(field);
    setHasRowVersionConflict(false);
  };
  const close = () => {
    setSelected(undefined);
    setMode("view");
    setHasRowVersionConflict(false);
  };
  const statusDefinitions = useMemo(
    () => ({
      active: { label: commonT("states.active"), variant: "success" as const },
      inactive: { label: commonT("states.inactive"), variant: "muted" as const },
    }),
    [commonT],
  );

  const resolveFieldTypeName = useCallback(
    (fieldTypeId: number) => getFieldTypeName(fieldTypes, fieldTypeId),
    [fieldTypes],
  );

  const columns = useMemo<ColumnDef<GlobalField>[]>(
    () => [
      { accessorKey: "name", header: t("columns.name") },
      { accessorKey: "key", header: t("columns.key") },
      {
        id: "fieldType",
        header: t("columns.fieldType"),
        cell: ({ row }) => resolveFieldTypeName(row.original.fieldTypeId),
      },
      {
        id: "optionCount",
        header: t("columns.optionCount"),
        cell: ({ row }) => row.original.options.length,
      },
      {
        id: "status",
        header: t("columns.status"),
        cell: ({ row }) => (
          <StatusBadge
            status={row.original.isActive ? "active" : "inactive"}
            definitions={statusDefinitions}
          />
        ),
      },
      {
        id: "updatedAt",
        header: t("columns.updatedAt"),
        cell: ({ row }) => dateFormatter.format(new Date(row.original.updatedAt)),
      },
      {
        id: "actions",
        header: t("columns.actions"),
        cell: ({ row }) => {
          const field = row.original;
          return (
            <div className="flex flex-wrap gap-1">
              <Button variant="ghost" size="sm" onClick={() => open("view", field)}>
                {commonT("actions.view")}
              </Button>
              <PermissionGuard required="global_field_edit">
                <Button variant="ghost" size="sm" onClick={() => open("edit", field)}>
                  {commonT("actions.edit")}
                </Button>
              </PermissionGuard>
              <PermissionGuard required="global_field_edit">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() =>
                    statusMutation.mutate({
                      fieldId: field.id,
                      isActive: !field.isActive,
                      rowVersion: field.rowVersion,
                    })
                  }
                >
                  {field.isActive ? commonT("actions.deactivate") : commonT("actions.reactivate")}
                </Button>
              </PermissionGuard>
              <PermissionGuard required="global_field_delete">
                <ConfirmDialog
                  trigger={
                    <Button variant="ghost" size="sm" className="text-destructive">
                      {commonT("actions.delete")}
                    </Button>
                  }
                  title={t("delete.title")}
                  description={t("delete.description", { name: field.name })}
                  destructive
                  onConfirm={async () => {
                    await deleteMutation.mutateAsync({
                      fieldId: field.id,
                      rowVersion: field.rowVersion,
                    });
                  }}
                />
              </PermissionGuard>
            </div>
          );
        },
      },
    ],
    [commonT, dateFormatter, deleteMutation, resolveFieldTypeName, statusDefinitions, statusMutation, t],
  );

  const updateSearch = useCallback(
    (next: Partial<typeof search>) => {
      void navigate({ search: (old) => ({ ...old, ...next, page: 1 }) });
    },
    [navigate],
  );

  const typeFilterDisabled = fieldTypesQuery.isPending || fieldTypesQuery.isError || fieldTypes.length === 0;

  return (
    <PageContent>
      <PageHeader
        title={t("page.title")}
        description={t("page.description")}
        actions={
          <PermissionGuard required="global_field_add">
            <Button onClick={() => open("create")} disabled={fieldTypesQuery.isError}>
              {t("page.add")}
            </Button>
          </PermissionGuard>
        }
      />
      {fieldTypesQuery.isError ? (
        <ErrorState
          description={t("page.fieldTypesError")}
          onRetry={() => void fieldTypesQuery.refetch()}
        />
      ) : null}
      <PageToolbar>
        <SearchInput
          value={search.search}
          onSearch={(value) => updateSearch({ search: value })}
          placeholder={t("page.searchPlaceholder")}
          className="w-full sm:max-w-sm"
        />
        <div className="flex flex-wrap gap-2">
          <Select
            value={search.fieldTypeId ? String(search.fieldTypeId) : "all"}
            onValueChange={(value) =>
              updateSearch({
                fieldTypeId: value === "all" ? undefined : Number(value),
              })
            }
            disabled={typeFilterDisabled}
          >
            <SelectTrigger className="w-40">
              <SelectValue placeholder={t("filters.fieldType")} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">{t("filters.allTypes")}</SelectItem>
              {fieldTypes.map((type) => (
                <SelectItem key={type.id} value={String(type.id)}>
                  {type.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select
            value={search.isActive === undefined ? "all" : String(search.isActive)}
            onValueChange={(value) =>
              updateSearch({ isActive: value === "all" ? undefined : value === "true" })
            }
          >
            <SelectTrigger className="w-36">
              <SelectValue placeholder={t("filters.status")} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">{t("filters.allStatuses")}</SelectItem>
              <SelectItem value="true">{commonT("states.active")}</SelectItem>
              <SelectItem value="false">{commonT("states.inactive")}</SelectItem>
            </SelectContent>
          </Select>
          <Select
            value={search.sortBy || "updatedAt"}
            onValueChange={(value) =>
              updateSearch({ sortBy: value, sortDirection: search.sortDirection ?? "desc" })
            }
          >
            <SelectTrigger className="w-36">
              <SelectValue placeholder={t("filters.sortBy")} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="name">{t("columns.name")}</SelectItem>
              <SelectItem value="key">{t("columns.key")}</SelectItem>
              <SelectItem value="updatedAt">{t("columns.updatedAt")}</SelectItem>
            </SelectContent>
          </Select>
          <Select
            value={search.sortDirection ?? "desc"}
            onValueChange={(value) => updateSearch({ sortDirection: value as "asc" | "desc" })}
          >
            <SelectTrigger className="w-28">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="asc">{t("filters.ascending")}</SelectItem>
              <SelectItem value="desc">{t("filters.descending")}</SelectItem>
            </SelectContent>
          </Select>
        </div>
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
          getRowId={(field) => field.id}
          emptyTitle={t("page.empty")}
          tableClassName="min-w-[980px]"
          renderMobileCard={(field) => (
            <MobileDataCard
              title={field.name}
              subtitle={field.key}
              status={
                <StatusBadge
                  status={field.isActive ? "active" : "inactive"}
                  definitions={statusDefinitions}
                />
              }
              fields={[
                {
                  label: t("columns.fieldType"),
                  value: resolveFieldTypeName(field.fieldTypeId),
                },
                { label: t("columns.optionCount"), value: field.options.length },
              ]}
              actions={
                <div className="flex gap-2">
                  <Button variant="outline" onClick={() => open("view", field)}>
                    {commonT("actions.view")}
                  </Button>
                  <PermissionGuard required="global_field_edit">
                    <Button onClick={() => open("edit", field)}>{commonT("actions.edit")}</Button>
                  </PermissionGuard>
                </div>
              }
            />
          )}
        />
      )}
      <Dialog open={mode === "create" || Boolean(selected)} onOpenChange={(isOpen) => !isOpen && close()}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>{t(`dialog.${mode}`)}</DialogTitle>
            <DialogDescription>{t("form.description")}</DialogDescription>
          </DialogHeader>
          <GlobalFieldForm
            key={`${mode}-${selected?.id ?? "new"}`}
            field={selected}
            readOnly={mode === "view"}
            onCancel={close}
            hasRowVersionConflict={hasRowVersionConflict}
            onReload={() => {
              void query.refetch();
              setHasRowVersionConflict(false);
            }}
            onSubmit={async (payload) => {
              try {
                if (mode === "create") await createMutation.mutateAsync(payload);
                else if (selected) {
                  await updateMutation.mutateAsync(
                    payload as Parameters<typeof updateMutation.mutateAsync>[0],
                  );
                }
                close();
              } catch (error) {
                if (isApiError(error) && error.messageKey === "ERR-010") {
                  setHasRowVersionConflict(true);
                }
                throw error;
              }
            }}
          />
        </DialogContent>
      </Dialog>
    </PageContent>
  );
}
