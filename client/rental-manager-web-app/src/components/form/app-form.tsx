import * as React from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  useForm,
  type DefaultValues,
  type FieldValues,
  type SubmitErrorHandler,
  type UseFormProps,
  type UseFormReturn,
} from "react-hook-form";
import { z } from "zod";
import { toast } from "sonner";

import { isApiError } from "@/api/client";
import { Form } from "@/components/ui/form";
import { cn } from "@/lib/utils";
import {
  applyServerErrors,
  type ServerErrorOptions,
} from "@/components/form/server-errors";
import { FormErrorAlert } from "@/components/form/server-error-alert";

export type AppFormSubmitHandler<TValues extends FieldValues> = (
  values: TValues,
  form: UseFormReturn<TValues>,
) => void | Promise<void>;

export type AppFormProps<TValues extends FieldValues> = {
  schema: z.ZodType<TValues, z.ZodTypeDef, unknown>;
  defaultValues: DefaultValues<TValues>;
  onSubmit: AppFormSubmitHandler<TValues>;
  children: (form: UseFormReturn<TValues>) => React.ReactNode;
  formOptions?: Omit<UseFormProps<TValues>, "defaultValues" | "resolver">;
  onInvalid?: SubmitErrorHandler<TValues>;
  serverErrorOptions?: ServerErrorOptions<TValues>;
  handleServerErrors?: boolean;
  className?: string;
  id?: string;
};

export function AppForm<TValues extends FieldValues>({
  schema,
  defaultValues,
  onSubmit,
  children,
  formOptions,
  onInvalid,
  serverErrorOptions,
  handleServerErrors = true,
  className,
  id,
}: AppFormProps<TValues>) {
  const [correlationId, setCorrelationId] = React.useState<string>();
  const form = useForm<TValues>({
    mode: "onTouched",
    reValidateMode: "onChange",
    shouldFocusError: true,
    ...formOptions,
    resolver: zodResolver(schema),
    defaultValues,
  });

  const submit = form.handleSubmit(async (values) => {
    form.clearErrors("root.server");
    setCorrelationId(undefined);

    try {
      await onSubmit(values, form);
    } catch (error) {
      if (!handleServerErrors) {
        throw error;
      }

      const mapped = applyServerErrors(form, error, serverErrorOptions);
      if (isApiError(error)) {
        setCorrelationId(error.correlationId);
        if (!mapped.appliedFieldError && (error.status === 0 || error.status >= 500)) {
          toast.error(mapped.message);
        }
      }
    }
  }, onInvalid);

  const rootError = form.formState.errors.root?.server?.message;

  return (
    <Form {...form}>
      <form
        id={id}
        className={cn("space-y-6", className)}
        onSubmit={submit}
        noValidate
      >
        {rootError ? (
          <FormErrorAlert message={String(rootError)} correlationId={correlationId} />
        ) : null}

        {children(form)}
      </form>
    </Form>
  );
}
