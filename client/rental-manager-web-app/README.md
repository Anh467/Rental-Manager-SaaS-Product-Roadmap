# Rental Manager Web App

Frontend foundation using React, TypeScript, shadcn/ui, React Hook Form and Zod.

## Run locally

```bash
cd client/rental-manager-web-app
npm install
npm run dev
npm run type-check
npm run build
```

## Architecture

```text
src/
├── components/
│   ├── ui/                 # shadcn primitives only; no business logic
│   └── form/               # reusable form layer used by all features
│       ├── app-form.tsx    # useForm + zodResolver + submit/error lifecycle
│       ├── fields.tsx      # typed Text/Number/Date/Textarea/Select/Switch fields
│       ├── layout.tsx      # FormGrid, FormSection, FormActions, submit button
│       ├── schema.ts       # common Zod validation helpers/messages
│       ├── server-errors.ts# maps .NET ProblemDetails errors to RHF fields
│       └── index.ts        # public API
└── features/
    └── properties/
        └── components/
            └── property-form.tsx
```

## Rules

1. `components/ui` stays close to shadcn and must not know rental business rules.
2. `components/form` contains generic reusable behavior only.
3. Every feature owns its own Zod schema, default values and submit request.
4. Validation messages come from schema; field components only render errors.
5. API validation errors are handled once by `AppForm` and `applyServerErrors`.
6. Never copy label/error/loading markup into feature forms.
7. Do not build one giant dynamic form renderer for every use case. Typed field components keep forms readable and safe while still removing repetition.

## Creating a feature form

```tsx
const schema = z.object({
  name: requiredText("Tên phòng", 100),
  monthlyRent: requiredNumber("Giá thuê", 0),
  statusId: requiredText("Trạng thái"),
  description: optionalText(500),
  isActive: optionalBoolean,
});

type Values = z.infer<typeof schema>;

export function RoomForm() {
  return (
    <AppForm<Values>
      schema={schema}
      defaultValues={{
        name: "",
        monthlyRent: 0,
        statusId: "",
        description: undefined,
        isActive: true,
      }}
      onSubmit={(values) => roomApi.create(values)}
    >
      {(form) => (
        <>
          <FormGrid>
            <TextFormField
              control={form.control}
              name="name"
              label="Tên phòng"
              required
            />
            <NumberFormField
              control={form.control}
              name="monthlyRent"
              label="Giá thuê"
              min={0}
              required
            />
          </FormGrid>

          <SelectFormField
            control={form.control}
            name="statusId"
            label="Trạng thái"
            options={statusOptions}
            required
          />

          <FormActions>
            <FormSubmitButton>Lưu phòng</FormSubmitButton>
          </FormActions>
        </>
      )}
    </AppForm>
  );
}
```

The `name` prop is type-safe. A typo or a field absent from the schema causes a TypeScript error.

## .NET API validation errors

Supported payloads:

```json
{
  "code": "ERR-VALIDATION",
  "message": "Dữ liệu không hợp lệ.",
  "errors": {
    "Name": ["Tên khu nhà đã tồn tại."],
    "TotalFloors": ["Số tầng phải lớn hơn 0."]
  }
}
```

`AppForm` automatically maps `Name` to `name`. Use `fieldMap` when backend and frontend names are different:

```tsx
<AppForm
  serverErrorOptions={{
    fieldMap: {
      PropertyTypeId: "propertyTypeId",
    },
  }}
/>
```

Errors without a field are displayed once at the top of the form. This prevents each feature from implementing its own try/catch and error UI.
