import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";

import { useGlobalFieldTypesQuery } from "@/api/routes/global-field-types";
import type { CreateGlobalFieldRequest, GlobalField, UpdateGlobalFieldRequest } from "@/api/routes/global-fields";
import {
  AppForm,
  FormActions,
  FormGrid,
  FormSection,
  FormSubmitButton,
  RowVersionConflictAlert,
  SelectFormField,
  SwitchFormField,
  TextareaFormField,
  TextFormField,
  optionalBoolean,
  optionalText,
  requiredText,
  validationMessages,
} from "@/components/form";
import { ErrorState } from "@/components/common/page";
import { Button } from "@/components/ui/button";
import {
  fieldTypeRequiresOptions,
  toFieldTypeSelectOptions,
} from "../lib/field-type-options";
import { FieldOptionEditor } from "./field-option-editor";

export type GlobalFieldFormValues = {
  name: string;
  key: string;
  description?: string;
  fieldTypeId: string;
  isActive: boolean;
  options: Array<{ key: string; name: string; description?: string; isActive: boolean }>;
};

export type GlobalFieldFormProps = {
  field?: GlobalField;
  readOnly?: boolean;
  onCancel?: () => void;
  onSubmit: (values: CreateGlobalFieldRequest | UpdateGlobalFieldRequest) => Promise<void>;
  hasRowVersionConflict?: boolean;
  onReload?: () => void;
};

export function GlobalFieldForm({
  field,
  readOnly = false,
  onCancel,
  onSubmit,
  hasRowVersionConflict,
  onReload,
}: GlobalFieldFormProps) {
  const { t } = useTranslation("global-field");
  const { t: commonT } = useTranslation("common");
  const isEdit = Boolean(field);
  const fieldTypesQuery = useGlobalFieldTypesQuery();
  const fieldTypes = fieldTypesQuery.data ?? [];
  const fieldTypeOptions = useMemo(() => {
    const options = toFieldTypeSelectOptions(fieldTypes);
    if (
      field &&
      !options.some((option) => option.value === String(field.fieldTypeId))
    ) {
      options.push({
        value: String(field.fieldTypeId),
        label: String(field.fieldTypeId),
      });
    }
    return options;
  }, [field, fieldTypes]);

  const schema = useMemo(
    () =>
      z
        .object({
          name: requiredText(t("form.fields.name"), 256),
          key: requiredText(t("form.fields.key"), 256).regex(
            /^[a-z0-9_]+$/,
            t("form.validation.key"),
          ),
          description: optionalText(t("form.fields.description"), 1028),
          fieldTypeId: z
            .string({ required_error: validationMessages.required(t("form.fields.fieldType")) })
            .min(1, validationMessages.required(t("form.fields.fieldType")))
            .refine(
              (value) => fieldTypeOptions.some((option) => option.value === value),
              validationMessages.required(t("form.fields.fieldType")),
            ),
          isActive: optionalBoolean,
          options: z.array(
            z.object({
              name: requiredText(t("options.fields.name"), 256),
              key: requiredText(t("options.fields.key"), 256).regex(
                /^[a-z0-9_]+$/,
                t("form.validation.key"),
              ),
              description: optionalText(t("options.fields.description"), 1028),
              isActive: optionalBoolean,
            }),
          ),
        })
        .superRefine((values, context) => {
          if (
            fieldTypeRequiresOptions(fieldTypes, values.fieldTypeId) &&
            values.options.length === 0
          ) {
            context.addIssue({
              code: z.ZodIssueCode.custom,
              path: ["options"],
              message: t("options.required"),
            });
          }
        }),
    [fieldTypeOptions, fieldTypes, t],
  );

  const initialValues: GlobalFieldFormValues = {
    name: field?.name ?? "",
    key: field?.key ?? "",
    description: field?.description ?? "",
    fieldTypeId: field ? String(field.fieldTypeId) : (fieldTypeOptions[0]?.value ?? ""),
    isActive: field?.isActive ?? true,
    options:
      field?.options.map(({ key, name, description, isActive }) => ({
        key,
        name,
        description,
        isActive,
      })) ?? [],
  };

  if (fieldTypesQuery.isError) {
    return (
      <ErrorState
        description={t("page.fieldTypesError")}
        onRetry={() => void fieldTypesQuery.refetch()}
      />
    );
  }

  const typesLoading = fieldTypesQuery.isPending;
  const typeSelectDisabled = readOnly || isEdit || typesLoading || fieldTypeOptions.length === 0;

  return (
    <AppForm<GlobalFieldFormValues>
      key={`${field?.id ?? "new"}-${fieldTypeOptions.map((option) => option.value).join(",")}`}
      schema={schema}
      defaultValues={initialValues}
      onSubmit={async (values) => {
        const fieldTypeId = Number(values.fieldTypeId);
        const payload = {
          ...values,
          fieldTypeId,
          options: fieldTypeRequiresOptions(fieldTypes, fieldTypeId)
            ? values.options.map((option, displayOrder) => ({ ...option, displayOrder }))
            : [],
        };
        await onSubmit(isEdit ? { ...payload, rowVersion: field!.rowVersion } : payload);
      }}
      serverErrorOptions={{ fieldMap: { FieldTypeId: "fieldTypeId", IsActive: "isActive" } }}
    >
      {(form) => (
        <>
          {hasRowVersionConflict && onReload ? (
            <RowVersionConflictAlert onReload={onReload} />
          ) : null}
          <FormSection title={t("form.title")} description={t("form.description")}>
            <FormGrid>
              <TextFormField
                control={form.control}
                name="name"
                label={t("form.fields.name")}
                required
                disabled={readOnly}
                maxLength={256}
              />
              <TextFormField
                control={form.control}
                name="key"
                label={t("form.fields.key")}
                required
                disabled={readOnly || isEdit}
                maxLength={256}
              />
              <SelectFormField
                control={form.control}
                name="fieldTypeId"
                label={t("form.fields.fieldType")}
                required
                disabled={typeSelectDisabled}
                options={fieldTypeOptions}
                placeholder={typesLoading ? commonT("state.loading") : undefined}
              />
            </FormGrid>
            <TextareaFormField
              control={form.control}
              name="description"
              label={t("form.fields.description")}
              disabled={readOnly}
              rows={3}
              maxLength={1028}
            />
            {!readOnly ? (
              <SwitchFormField
                control={form.control}
                name="isActive"
                label={t("form.fields.isActive")}
              />
            ) : null}
            {fieldTypeRequiresOptions(fieldTypes, form.watch("fieldTypeId")) ? (
              <FieldOptionEditor control={form.control} disabled={readOnly} />
            ) : null}
          </FormSection>
          <FormActions>
            {onCancel ? (
              <Button type="button" variant="outline" onClick={onCancel}>
                {readOnly ? commonT("actions.close") : commonT("actions.cancel")}
              </Button>
            ) : null}
            {!readOnly ? (
              <FormSubmitButton disabled={typesLoading || fieldTypeOptions.length === 0}>
                {commonT("actions.save")}
              </FormSubmitButton>
            ) : null}
          </FormActions>
        </>
      )}
    </AppForm>
  );
}
