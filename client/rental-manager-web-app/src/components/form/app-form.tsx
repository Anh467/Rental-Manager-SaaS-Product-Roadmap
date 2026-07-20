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

import { Form } from "@/components/ui/form";
import { cn } from "@/lib/utils";
import {
  applyServerErrors,
  type ServerErrorOptions,
} from "@/components/form/server-errors";

export type AppFormSubmitHandler<TValues extends FieldValues> = (
  values: TValues,
  form: UseFormReturn<TValues>,
) => void | Promise<void>;

export type AppFormProps<TValues extends FieldValues> = {
  schema: z.ZodType<TValues, z.ZodTypeDef, TValues>;
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

    try {
      await onSubmit(values, form);
    } catch (error) {
      if (!handleServerErrors) {
        throw error;
      }

      applyServerErrors(form, error, serverErrorOptions);
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
          <div
            role="alert"
            className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive"
          >
            {String(rootError)}
          </div>
        ) : null}

        {children(form)}
      </form>
    </Form>
  );
}
