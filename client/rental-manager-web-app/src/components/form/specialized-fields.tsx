import type { FieldValues } from "react-hook-form";

import type {
  CommonFormFieldProps,
  NumberFormFieldProps,
  TextFormFieldProps,
} from "@/components/form/fields";
import {
  NumberFormField,
  TextFormField,
} from "@/components/form/fields";
import { Checkbox } from "@/components/ui/checkbox";
import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { cn } from "@/lib/utils";

export function EmailFormField<TValues extends FieldValues>(
  props: TextFormFieldProps<TValues>,
) {
  return <TextFormField {...props} type="email" inputMode="email" autoComplete="email" />;
}

export function PhoneFormField<TValues extends FieldValues>(
  props: TextFormFieldProps<TValues>,
) {
  return <TextFormField {...props} type="tel" inputMode="tel" autoComplete="tel" />;
}

export function PasswordFormField<TValues extends FieldValues>(
  props: TextFormFieldProps<TValues>,
) {
  return <TextFormField {...props} type="password" autoComplete="current-password" />;
}

export function MoneyFormField<TValues extends FieldValues>({
  min = 0,
  step = 1_000,
  ...props
}: NumberFormFieldProps<TValues>) {
  return (
    <NumberFormField
      {...props}
      min={min}
      step={step}
      inputMode="numeric"
    />
  );
}

export type CheckboxFormFieldProps<TValues extends FieldValues> =
  CommonFormFieldProps<TValues>;

export function CheckboxFormField<TValues extends FieldValues>({
  control,
  name,
  label,
  description,
  disabled,
  required,
  className,
}: CheckboxFormFieldProps<TValues>) {
  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem className={cn("space-y-2", className)}>
          <div className="flex items-start gap-3">
            <FormControl>
              <Checkbox
                checked={Boolean(field.value)}
                disabled={disabled}
                onCheckedChange={field.onChange}
              />
            </FormControl>
            <div className="space-y-1 leading-none">
              <FormLabel>
                {label}
                {required ? <span className="ml-1 text-destructive">*</span> : null}
              </FormLabel>
              {description ? <FormDescription>{description}</FormDescription> : null}
            </div>
          </div>
          <FormMessage />
        </FormItem>
      )}
    />
  );
}
