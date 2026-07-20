import * as React from "react";
import { Loader2 } from "lucide-react";
import { useFormContext } from "react-hook-form";

import { Button, type ButtonProps } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export function FormGrid({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("grid grid-cols-1 gap-5 md:grid-cols-2", className)}
      {...props}
    />
  );
}

export type FormSectionProps = React.HTMLAttributes<HTMLElement> & {
  title?: React.ReactNode;
  description?: React.ReactNode;
};

export function FormSection({
  title,
  description,
  className,
  children,
  ...props
}: FormSectionProps) {
  return (
    <section className={cn("space-y-5", className)} {...props}>
      {title || description ? (
        <header className="space-y-1">
          {title ? <h2 className="text-base font-semibold">{title}</h2> : null}
          {description ? (
            <p className="text-sm text-muted-foreground">{description}</p>
          ) : null}
        </header>
      ) : null}
      {children}
    </section>
  );
}

export function FormActions({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "flex flex-col-reverse gap-3 border-t pt-5 sm:flex-row sm:justify-end",
        className,
      )}
      {...props}
    />
  );
}

export type FormSubmitButtonProps = ButtonProps & {
  submittingText?: React.ReactNode;
};

export function FormSubmitButton({
  children = "Lưu",
  submittingText = "Đang lưu...",
  disabled,
  ...props
}: FormSubmitButtonProps) {
  const { formState } = useFormContext();
  const isSubmitting = formState.isSubmitting;

  return (
    <Button type="submit" disabled={disabled || isSubmitting} {...props}>
      {isSubmitting ? (
        <>
          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          {submittingText}
        </>
      ) : (
        children
      )}
    </Button>
  );
}
