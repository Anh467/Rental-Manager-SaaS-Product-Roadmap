import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";

import {
  AppForm,
  FormActions,
  FormGrid,
  FormSection,
  FormSubmitButton,
  NumberFormField,
  SelectFormField,
  SwitchFormField,
  TextareaFormField,
  TextFormField,
  optionalBoolean,
  optionalText,
  requiredNumber,
  requiredText,
} from "@/components/form";
import { Button } from "@/components/ui/button";

export type PropertyFormValues = {
  name: string;
  code: string;
  propertyTypeId: string;
  address: string;
  totalFloors: number;
  description?: string;
  isActive: boolean;
};

export type PropertyFormProps = {
  initialValues?: Partial<PropertyFormValues>;
  onSubmit: (values: PropertyFormValues) => void | Promise<void>;
  onCancel?: () => void;
};

const defaultValues: PropertyFormValues = {
  name: "",
  code: "",
  propertyTypeId: "",
  address: "",
  totalFloors: 1,
  description: undefined,
  isActive: true,
};

export function PropertyForm({ initialValues, onSubmit, onCancel }: PropertyFormProps) {
  const { t } = useTranslation("property");
  const { t: commonT } = useTranslation("common");

  const schema = useMemo(
    () => z.object({
      name: requiredText(t("form.fields.name.label"), 150),
      code: requiredText(t("form.fields.code.label"), 50),
      propertyTypeId: requiredText(t("form.fields.propertyTypeId.label"), 50),
      address: requiredText(t("form.fields.address.label"), 500),
      totalFloors: requiredNumber(t("form.fields.totalFloors.label"), 1),
      description: optionalText(t("form.fields.description.label"), 1000),
      isActive: optionalBoolean,
    }),
    [t],
  );

  const propertyTypeOptions = useMemo(
    () => [
      { value: "boarding-house", label: t("form.types.boardingHouse") },
      { value: "apartment", label: t("form.types.apartment") },
      { value: "shared-house", label: t("form.types.sharedHouse") },
    ],
    [t],
  );

  return (
    <AppForm<PropertyFormValues>
      schema={schema}
      defaultValues={{ ...defaultValues, ...initialValues }}
      onSubmit={onSubmit}
      serverErrorOptions={{
        fieldMap: {
          PropertyTypeId: "propertyTypeId",
          TotalFloors: "totalFloors",
        },
      }}
    >
      {(form) => (
        <>
          <FormSection title={t("form.sectionTitle")} description={t("form.sectionDescription")}>
            <FormGrid>
              <TextFormField
                control={form.control}
                name="name"
                label={t("form.fields.name.label")}
                placeholder={t("form.fields.name.placeholder")}
                required
              />
              <TextFormField
                control={form.control}
                name="code"
                label={t("form.fields.code.label")}
                placeholder={t("form.fields.code.placeholder")}
                required
              />
              <SelectFormField
                control={form.control}
                name="propertyTypeId"
                label={t("form.fields.propertyTypeId.label")}
                placeholder={t("form.fields.propertyTypeId.placeholder")}
                required
                options={propertyTypeOptions}
              />
              <NumberFormField
                control={form.control}
                name="totalFloors"
                label={t("form.fields.totalFloors.label")}
                min={1}
                required
              />
            </FormGrid>

            <TextFormField
              control={form.control}
              name="address"
              label={t("form.fields.address.label")}
              placeholder={t("form.fields.address.placeholder")}
              required
            />
            <TextareaFormField
              control={form.control}
              name="description"
              label={t("form.fields.description.label")}
              placeholder={t("form.fields.description.placeholder")}
              rows={4}
            />
            <SwitchFormField
              control={form.control}
              name="isActive"
              label={t("form.fields.isActive.label")}
              description={t("form.fields.isActive.description")}
            />
          </FormSection>

          <FormActions>
            {onCancel ? (
              <Button type="button" variant="outline" onClick={onCancel}>
                {commonT("actions.cancel")}
              </Button>
            ) : null}
            <FormSubmitButton>{t("form.save")}</FormSubmitButton>
          </FormActions>
        </>
      )}
    </AppForm>
  );
}
