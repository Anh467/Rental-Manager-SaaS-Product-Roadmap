import * as React from "react";
import type { Control, FieldPath, FieldValues } from "react-hook-form";

import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
import { cn } from "@/lib/utils";

export type CommonFormFieldProps<TValues extends FieldValues> = {
  control: Control<TValues>;
  name: FieldPath<TValues>;
  label: React.ReactNode;
  description?: React.ReactNode;
  required?: boolean;
  disabled?: boolean;
  className?: string;
};

function FieldLabel({
  label,
  required,
}: Pick<CommonFormFieldProps<FieldValues>, "label" | "required">) {
  return (
    <FormLabel>
      {label}
      {required ? (
        <span className="ml-1 text-destructive" aria-hidden="true">
          *
        </span>
      ) : null}
    </FormLabel>
  );
}

function FieldDescription({ children }: { children?: React.ReactNode }) {
  return children ? <FormDescription>{children}</FormDescription> : null;
}

type InputPrimitiveProps = React.ComponentPropsWithoutRef<typeof Input>;

export type TextFormFieldProps<TValues extends FieldValues> =
  CommonFormFieldProps<TValues> &
    Omit<
      InputPrimitiveProps,
      "name" | "value" | "defaultValue" | "onChange" | "onBlur" | "disabled"
    >;

export function TextFormField<TValues extends FieldValues>({
  control,
  name,
  label,
  description,
  required,
  disabled,
  className,
  type = "text",
  ...inputProps
}: TextFormFieldProps<TValues>) {
  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem className={className}>
          <FieldLabel label={label} required={required} />
          <FormControl>
            <Input
              {...inputProps}
              {...field}
              type={type}
              disabled={disabled}
              value={field.value == null ? "" : String(field.value)}
            />
          </FormControl>
          <FieldDescription>{description}</FieldDescription>
          <FormMessage />
        </FormItem>
      )}
    />
  );
}

export type NumberFormFieldProps<TValues extends FieldValues> =
  CommonFormFieldProps<TValues> &
    Omit<
      InputPrimitiveProps,
      | "name"
      | "type"
      | "value"
      | "defaultValue"
      | "onChange"
      | "onBlur"
      | "disabled"
    >;

export function NumberFormField<TValues extends FieldValues>({
  control,
  name,
  label,
  description,
  required,
  disabled,
  className,
  ...inputProps
}: NumberFormFieldProps<TValues>) {
  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem className={className}>
          <FieldLabel label={label} required={required} />
          <FormControl>
            <Input
              {...inputProps}
              ref={field.ref}
              name={field.name}
              type="number"
              disabled={disabled}
              value={field.value == null ? "" : String(field.value)}
              onBlur={field.onBlur}
              onChange={(event) => {
                const value = event.currentTarget.value;
                field.onChange(value === "" ? undefined : Number(value));
              }}
            />
          </FormControl>
          <FieldDescription>{description}</FieldDescription>
          <FormMessage />
        </FormItem>
      )}
    />
  );
}

export type DateFormFieldProps<TValues extends FieldValues> =
  CommonFormFieldProps<TValues> &
    Omit<
      InputPrimitiveProps,
      | "name"
      | "type"
      | "value"
      | "defaultValue"
      | "onChange"
      | "onBlur"
      | "disabled"
    >;

export function DateFormField<TValues extends FieldValues>({
  control,
  name,
  label,
  description,
  required,
  disabled,
  className,
  ...inputProps
}: DateFormFieldProps<TValues>) {
  return (
    <TextFormField
      {...inputProps}
      control={control}
      name={name}
      label={label}
      description={description}
      required={required}
      disabled={disabled}
      className={className}
      type="date"
    />
  );
}

type TextareaPrimitiveProps = React.ComponentPropsWithoutRef<typeof Textarea>;

export type TextareaFormFieldProps<TValues extends FieldValues> =
  CommonFormFieldProps<TValues> &
    Omit<
      TextareaPrimitiveProps,
      "name" | "value" | "defaultValue" | "onChange" | "onBlur" | "disabled"
    >;

export function TextareaFormField<TValues extends FieldValues>({
  control,
  name,
  label,
  description,
  required,
  disabled,
  className,
  ...textareaProps
}: TextareaFormFieldProps<TValues>) {
  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem className={className}>
          <FieldLabel label={label} required={required} />
          <FormControl>
            <Textarea
              {...textareaProps}
              {...field}
              disabled={disabled}
              value={field.value == null ? "" : String(field.value)}
            />
          </FormControl>
          <FieldDescription>{description}</FieldDescription>
          <FormMessage />
        </FormItem>
      )}
    />
  );
}

export type SelectOption = {
  value: string;
  label: React.ReactNode;
  disabled?: boolean;
};

export type SelectFormFieldProps<TValues extends FieldValues> =
  CommonFormFieldProps<TValues> & {
    options: SelectOption[];
    placeholder?: string;
    emptyText?: string;
  };

export function SelectFormField<TValues extends FieldValues>({
  control,
  name,
  label,
  description,
  required,
  disabled,
  className,
  options,
  placeholder = "Chọn một giá trị",
  emptyText = "Không có dữ liệu",
}: SelectFormFieldProps<TValues>) {
  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem className={className}>
          <FieldLabel label={label} required={required} />
          <Select
            disabled={disabled}
            value={field.value == null ? undefined : String(field.value)}
            onValueChange={field.onChange}
          >
            <FormControl>
              <SelectTrigger>
                <SelectValue placeholder={placeholder} />
              </SelectTrigger>
            </FormControl>
            <SelectContent>
              {options.length === 0 ? (
                <div className="px-2 py-1.5 text-sm text-muted-foreground">
                  {emptyText}
                </div>
              ) : (
                options.map((option) => (
                  <SelectItem
                    key={option.value}
                    value={option.value}
                    disabled={option.disabled}
                  >
                    {option.label}
                  </SelectItem>
                ))
              )}
            </SelectContent>
          </Select>
          <FieldDescription>{description}</FieldDescription>
          <FormMessage />
        </FormItem>
      )}
    />
  );
}

export type SwitchFormFieldProps<TValues extends FieldValues> =
  CommonFormFieldProps<TValues>;

export function SwitchFormField<TValues extends FieldValues>({
  control,
  name,
  label,
  description,
  disabled,
  className,
}: SwitchFormFieldProps<TValues>) {
  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem
          className={cn(
            "rounded-md border bg-card p-4",
            className,
          )}
        >
          <div className="flex items-start justify-between gap-4">
            <div className="space-y-1">
              <FormLabel>{label}</FormLabel>
              <FieldDescription>{description}</FieldDescription>
            </div>
            <FormControl>
              <Switch
                checked={Boolean(field.value)}
                disabled={disabled}
                onCheckedChange={field.onChange}
              />
            </FormControl>
          </div>
          <FormMessage />
        </FormItem>
      )}
    />
  );
}
