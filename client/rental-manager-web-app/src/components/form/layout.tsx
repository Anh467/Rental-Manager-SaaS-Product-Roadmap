import * as React from "react";
import { Loader2 } from "lucide-react";
import { useFormContext } from "react-hook-form";
import { useTranslation } from "react-i18next";

import { Button, type ButtonProps } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export type FormGridProps = React.HTMLAttributes<HTMLDivElement> & {
  columns?: 1 | 2 | 3;
};

const gridColumns: Record<NonNullable<FormGridProps["columns"]>, string> = {
  1: "grid-cols-1",
  2: "grid-cols-1 md:grid-cols-2",
  3: "grid-cols-1 md:grid-cols-2 xl:grid-cols-3",
};

export function FormGrid({
  columns = 2,
  className,
  ...props
}: FormGridProps) {
  return (
    <div
      className={cn("grid min-w-0 gap-4 sm:gap-5", gridColumns[columns], className)}
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
    <section className={cn("min-w-0 space-y-4 sm:space-y-5", className)} {...props}>
      {title || description ? (
        <header className="min-w-0 space-y-1">
          {title ? <h2 className="break-words text-base font-semibold">{title}</h2> : null}
          {description ? (
            <p className="max-w-3xl break-words text-sm leading-6 text-muted-foreground">{description}</p>
          ) : null}
        </header>
      ) : null}
      {children}
    </section>
  );
}

export type FormActionsProps = React.HTMLAttributes<HTMLDivElement> & {
  stickyOnMobile?: boolean;
};

export function FormActions({
  stickyOnMobile = false,
  className,
  ...props
}: FormActionsProps) {
  return (
    <div
      className={cn(
        "flex flex-col-reverse gap-3 border-t pt-5 sm:flex-row sm:flex-wrap sm:justify-end [&>*]:w-full sm:[&>*]:w-auto",
        stickyOnMobile && "sticky bottom-0 z-20 -mx-4 bg-background/95 px-4 pb-[max(1rem,env(safe-area-inset-bottom))] pt-4 shadow-[0_-8px_24px_-16px_rgba(15,23,42,0.5)] backdrop-blur sm:static sm:mx-0 sm:bg-transparent sm:px-0 sm:pb-0 sm:shadow-none",
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
  children,
  submittingText,
  disabled,
  className,
  ...props
}: FormSubmitButtonProps) {
  const { t } = useTranslation("common");
  const { formState } = useFormContext();
  const isSubmitting = formState.isSubmitting;

  return (
    <Button
      type="submit"
      className={cn("min-h-11 sm:min-h-10", className)}
      disabled={disabled || isSubmitting}
      {...props}
    >
      {isSubmitting ? (
        <>
          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          {submittingText ?? t("actions.saving")}
        </>
      ) : (
        children ?? t("actions.save")
      )}
    </Button>
  );
}
