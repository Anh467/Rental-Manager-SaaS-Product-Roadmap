import { useMemo } from "react";
import { createFileRoute, redirect, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { z } from "zod";

import { useAuth } from "@/features/auth/auth-provider";
import {
  AppForm,
  EmailFormField,
  FormSubmitButton,
  PasswordFormField,
  requiredEmail,
  requiredText,
} from "@/components/form";

export const Route = createFileRoute("/login")({
  beforeLoad: () => {
    if (localStorage.getItem("access_token")) throw redirect({ to: "/", replace: true });
  },
  component: LoginPage,
});

type LoginFormValues = {
  email: string;
  password: string;
};

function LoginPage() {
  const { t } = useTranslation("common");
  const { login } = useAuth();
  const navigate = useNavigate();

  const schema = useMemo(
    () =>
      z.object({
        email: requiredEmail(t("login.email")),
        password: requiredText(t("login.password"), 256),
      }),
    [t],
  );

  return (
    <main className="grid min-h-dvh place-items-center bg-muted/30 p-4">
      <div className="w-full max-w-md space-y-5 rounded-lg border bg-background p-6 shadow-sm">
        <div>
          <h1 className="text-xl font-semibold">{t("login.title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("login.subtitle")}</p>
        </div>
        <AppForm<LoginFormValues>
          schema={schema}
          defaultValues={{ email: "", password: "" }}
          onSubmit={async (values) => {
            await login(values);
            await navigate({ to: "/" });
          }}
          serverErrorOptions={{
            fieldMap: { Email: "email", Password: "password" },
            fallbackMessage: t("login.failed"),
          }}
          className="space-y-5"
        >
          {(form) => (
            <>
              <EmailFormField
                control={form.control}
                name="email"
                label={t("login.email")}
                required
                autoComplete="email"
              />
              <PasswordFormField
                control={form.control}
                name="password"
                label={t("login.password")}
                required
                autoComplete="current-password"
              />
              <FormSubmitButton className="w-full" submittingText={t("login.submitting")}>
                {t("login.submit")}
              </FormSubmitButton>
            </>
          )}
        </AppForm>
      </div>
    </main>
  );
}
