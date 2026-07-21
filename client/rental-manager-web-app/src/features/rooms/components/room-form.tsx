import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";

import type { CreateRoomRequest, RoomStatus } from "@/api/routes/rooms";
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

export type RoomFormValues = CreateRoomRequest;

export type RoomFormProps = {
  propertyOptions: Array<{ value: string; label: string }>;
  initialValues?: Partial<RoomFormValues>;
  onSubmit: (values: CreateRoomRequest) => void | Promise<void>;
  onCancel?: () => void;
};

const defaultValues: RoomFormValues = {
  propertyId: "",
  code: "",
  floor: 1,
  capacity: 1,
  monthlyRent: 0,
  status: "vacant",
  description: undefined,
  isActive: true,
};

export function RoomForm({ propertyOptions, initialValues, onSubmit, onCancel }: RoomFormProps) {
  const { t } = useTranslation("room");
  const { t: commonT } = useTranslation("common");

  const schema = useMemo(
    () => z.object({
      propertyId: requiredText(t("form.fields.propertyId.label"), 50),
      code: requiredText(t("form.fields.code.label"), 50),
      floor: requiredNumber(t("form.fields.floor.label"), 0),
      capacity: requiredNumber(t("form.fields.capacity.label"), 1),
      monthlyRent: requiredNumber(t("form.fields.monthlyRent.label"), 0),
      status: z.enum(["vacant", "occupied", "maintenance", "inactive"]),
      description: optionalText(t("form.fields.description.label"), 1000),
      isActive: optionalBoolean,
    }),
    [t],
  );

  const statusOptions = useMemo<Array<{ value: RoomStatus; label: string }>>(
    () => [
      { value: "vacant", label: t("status.vacant") },
      { value: "occupied", label: t("status.occupied") },
      { value: "maintenance", label: t("status.maintenance") },
      { value: "inactive", label: t("status.inactive") },
    ],
    [t],
  );

  return (
    <AppForm<RoomFormValues>
      schema={schema}
      defaultValues={{ ...defaultValues, ...initialValues }}
      onSubmit={(values) => onSubmit(values)}
      serverErrorOptions={{
        fieldMap: {
          PropertyId: "propertyId",
          MonthlyRent: "monthlyRent",
          IsActive: "isActive",
        },
      }}
    >
      {(form) => (
        <>
          <FormSection title={t("form.sectionTitle")} description={t("form.sectionDescription")}>
            <FormGrid>
              <SelectFormField
                control={form.control}
                name="propertyId"
                label={t("form.fields.propertyId.label")}
                options={propertyOptions}
                placeholder={t("form.fields.propertyId.placeholder")}
                required
              />
              <TextFormField
                control={form.control}
                name="code"
                label={t("form.fields.code.label")}
                placeholder={t("form.fields.code.placeholder")}
                required
              />
              <NumberFormField
                control={form.control}
                name="floor"
                label={t("form.fields.floor.label")}
                min={0}
                required
              />
              <NumberFormField
                control={form.control}
                name="capacity"
                label={t("form.fields.capacity.label")}
                min={1}
                required
              />
              <NumberFormField
                control={form.control}
                name="monthlyRent"
                label={t("form.fields.monthlyRent.label")}
                min={0}
                required
              />
              <SelectFormField
                control={form.control}
                name="status"
                label={t("form.fields.status.label")}
                options={statusOptions}
                required
              />
            </FormGrid>

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
