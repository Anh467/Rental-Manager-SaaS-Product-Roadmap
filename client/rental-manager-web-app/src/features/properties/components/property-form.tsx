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

const propertySchema = z.object({
  name: requiredText("Tên khu nhà", 150),
  code: requiredText("Mã khu nhà", 50),
  propertyTypeId: requiredText("Loại khu nhà", 50),
  address: requiredText("Địa chỉ", 500),
  totalFloors: requiredNumber("Số tầng", 1),
  description: optionalText(1000),
  isActive: optionalBoolean,
});

export type PropertyFormValues = z.infer<typeof propertySchema>;

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

export function PropertyForm({
  initialValues,
  onSubmit,
  onCancel,
}: PropertyFormProps) {
  return (
    <AppForm<PropertyFormValues>
      schema={propertySchema}
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
          <FormSection
            title="Thông tin khu nhà"
            description="Các field bên dưới chỉ sử dụng component chung; schema chịu trách nhiệm validation."
          >
            <FormGrid>
              <TextFormField
                control={form.control}
                name="name"
                label="Tên khu nhà"
                placeholder="Ví dụ: Nhà trọ Minh Anh"
                required
              />

              <TextFormField
                control={form.control}
                name="code"
                label="Mã khu nhà"
                placeholder="Ví dụ: NHA-A"
                required
              />

              <SelectFormField
                control={form.control}
                name="propertyTypeId"
                label="Loại khu nhà"
                placeholder="Chọn loại khu nhà"
                required
                options={[
                  { value: "boarding-house", label: "Nhà trọ" },
                  { value: "apartment", label: "Căn hộ" },
                  { value: "shared-house", label: "Nhà nguyên căn" },
                ]}
              />

              <NumberFormField
                control={form.control}
                name="totalFloors"
                label="Số tầng"
                min={1}
                required
              />
            </FormGrid>

            <TextFormField
              control={form.control}
              name="address"
              label="Địa chỉ"
              placeholder="Nhập địa chỉ đầy đủ"
              required
            />

            <TextareaFormField
              control={form.control}
              name="description"
              label="Mô tả"
              placeholder="Thông tin bổ sung"
              rows={4}
            />

            <SwitchFormField
              control={form.control}
              name="isActive"
              label="Đang hoạt động"
              description="Tắt trạng thái này để ngừng sử dụng khu nhà nhưng vẫn giữ dữ liệu lịch sử."
            />
          </FormSection>

          <FormActions>
            {onCancel ? (
              <Button type="button" variant="outline" onClick={onCancel}>
                Hủy
              </Button>
            ) : null}
            <FormSubmitButton>Lưu khu nhà</FormSubmitButton>
          </FormActions>
        </>
      )}
    </AppForm>
  );
}
