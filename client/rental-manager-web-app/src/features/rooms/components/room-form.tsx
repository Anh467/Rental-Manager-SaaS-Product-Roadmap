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

const roomSchema = z.object({
  propertyId: requiredText("Khu nhà", 50),
  code: requiredText("Mã phòng", 50),
  floor: requiredNumber("Tầng", 0),
  capacity: requiredNumber("Sức chứa", 1),
  monthlyRent: requiredNumber("Giá thuê", 0),
  status: z.enum(["vacant", "occupied", "maintenance", "inactive"]),
  description: optionalText(1000),
  isActive: optionalBoolean,
});

export type RoomFormValues = z.infer<typeof roomSchema>;

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

const statusOptions: Array<{ value: RoomStatus; label: string }> = [
  { value: "vacant", label: "Phòng trống" },
  { value: "occupied", label: "Đang thuê" },
  { value: "maintenance", label: "Đang bảo trì" },
  { value: "inactive", label: "Tạm ngưng" },
];

export function RoomForm({ propertyOptions, initialValues, onSubmit, onCancel }: RoomFormProps) {
  return (
    <AppForm<RoomFormValues>
      schema={roomSchema}
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
          <FormSection
            title="Thông tin phòng"
            description="Baseline nghiệp vụ Room sử dụng toàn bộ field wrapper chung."
          >
            <FormGrid>
              <SelectFormField
                control={form.control}
                name="propertyId"
                label="Khu nhà"
                options={propertyOptions}
                placeholder="Chọn khu nhà"
                required
              />
              <TextFormField
                control={form.control}
                name="code"
                label="Mã phòng"
                placeholder="Ví dụ: P.201"
                required
              />
              <NumberFormField
                control={form.control}
                name="floor"
                label="Tầng"
                min={0}
                required
              />
              <NumberFormField
                control={form.control}
                name="capacity"
                label="Sức chứa"
                min={1}
                required
              />
              <NumberFormField
                control={form.control}
                name="monthlyRent"
                label="Giá thuê hàng tháng"
                min={0}
                required
              />
              <SelectFormField
                control={form.control}
                name="status"
                label="Trạng thái"
                options={statusOptions}
                required
              />
            </FormGrid>

            <TextareaFormField
              control={form.control}
              name="description"
              label="Mô tả"
              placeholder="Ghi chú về phòng, nội thất hoặc tình trạng hiện tại"
              rows={4}
            />

            <SwitchFormField
              control={form.control}
              name="isActive"
              label="Đang sử dụng"
              description="Tắt để ngừng sử dụng phòng nhưng vẫn giữ lịch sử hợp đồng và hóa đơn."
            />
          </FormSection>

          <FormActions>
            {onCancel ? (
              <Button type="button" variant="outline" onClick={onCancel}>Hủy</Button>
            ) : null}
            <FormSubmitButton>Lưu phòng</FormSubmitButton>
          </FormActions>
        </>
      )}
    </AppForm>
  );
}
