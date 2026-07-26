import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";

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
} from "@/components/form";
import { Button } from "@/components/ui/button";
import { FieldOptionEditor } from "./field-option-editor";

export type GlobalFieldFormValues = {
  name: string;
  key: string;
  description?: string;
  fieldTypeId: "1" | "2" | "4" | "6";
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

export function GlobalFieldForm({ field, readOnly = false, onCancel, onSubmit, hasRowVersionConflict, onReload }: GlobalFieldFormProps) {
  const { t } = useTranslation("global-field");
  const { t: commonT } = useTranslation("common");
  const isEdit = Boolean(field);
  const schema = useMemo(() => z.object({
    name: requiredText(t("form.fields.name"), 256),
    key: requiredText(t("form.fields.key"), 256).regex(/^[a-z0-9_]+$/, t("form.validation.key")),
    description: optionalText(t("form.fields.description"), 1028),
    fieldTypeId: z.enum(["1", "2", "4", "6"]),
    isActive: optionalBoolean,
    options: z.array(z.object({
      name: requiredText(t("options.fields.name"), 256),
      key: requiredText(t("options.fields.key"), 256).regex(/^[a-z0-9_]+$/, t("form.validation.key")),
      description: optionalText(t("options.fields.description"), 1028),
      isActive: optionalBoolean,
    })),
  }).superRefine((values, context) => {
    if (values.fieldTypeId === "6" && values.options.length === 0) {
      context.addIssue({ code: z.ZodIssueCode.custom, path: ["options"], message: t("options.required") });
    }
  }), [t]);

  const initialValues: GlobalFieldFormValues = {
    name: field?.name ?? "",
    key: field?.key ?? "",
    description: field?.description ?? "",
    fieldTypeId: String(field?.fieldTypeId ?? 1) as GlobalFieldFormValues["fieldTypeId"],
    isActive: field?.isActive ?? true,
    options: field?.options.map(({ key, name, description, isActive }) => ({ key, name, description, isActive })) ?? [],
  };

  return (
    <AppForm<GlobalFieldFormValues>
      schema={schema}
      defaultValues={initialValues}
      onSubmit={async (values) => {
        const payload = {
          ...values,
          fieldTypeId: Number(values.fieldTypeId) as 1 | 2 | 4 | 6,
          options: values.fieldTypeId === "6"
            ? values.options.map((option, displayOrder) => ({ ...option, displayOrder }))
            : [],
        };
        await onSubmit(isEdit ? { ...payload, rowVersion: field!.rowVersion } : payload);
      }}
      serverErrorOptions={{ fieldMap: { FieldTypeId: "fieldTypeId", IsActive: "isActive" } }}
    >
      {(form) => (
        <>
          {hasRowVersionConflict && onReload ? <RowVersionConflictAlert onReload={onReload} /> : null}
          <FormSection title={t("form.title")} description={t("form.description")}>
            <FormGrid>
              <TextFormField control={form.control} name="name" label={t("form.fields.name")} required disabled={readOnly} maxLength={256} />
              <TextFormField control={form.control} name="key" label={t("form.fields.key")} required disabled={readOnly || isEdit} maxLength={256} />
              <SelectFormField
                control={form.control}
                name="fieldTypeId"
                label={t("form.fields.fieldType")}
                required
                disabled={readOnly || isEdit}
                options={["1", "2", "4", "6"].map((value) => ({ value, label: t(`types.${value}`) }))}
              />
            </FormGrid>
            <TextareaFormField control={form.control} name="description" label={t("form.fields.description")} disabled={readOnly} rows={3} maxLength={1028} />
            {!readOnly ? <SwitchFormField control={form.control} name="isActive" label={t("form.fields.isActive")} /> : null}
            {form.watch("fieldTypeId") === "6" ? <FieldOptionEditor control={form.control} disabled={readOnly} /> : null}
          </FormSection>
          <FormActions>
            {onCancel ? <Button type="button" variant="outline" onClick={onCancel}>{readOnly ? commonT("actions.close") : commonT("actions.cancel")}</Button> : null}
            {!readOnly ? <FormSubmitButton>{commonT("actions.save")}</FormSubmitButton> : null}
          </FormActions>
        </>
      )}
    </AppForm>
  );
}
