import { useState } from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";

import {
  MOCK_LOGIN_DISPLAY_NAME,
  MOCK_LOGIN_EMAIL,
  MOCK_LOGIN_PROVIDER,
  MOCK_LOGIN_SUBJECT,
} from "@/api/mocks/auth-constants";
import { useAuth } from "@/features/auth/auth-provider";
import { redirectIfAuthenticated } from "@/features/auth/auth-session";
import { Button } from "@/components/ui/button";

export const Route = createFileRoute("/login")({
  beforeLoad: async ({ context }) => {
    await redirectIfAuthenticated(context.queryClient);
  },
  component: LoginPage,
});

function shouldUseMockLoginExchange() {
  const configured = import.meta.env.VITE_USE_MOCK_API ?? import.meta.env.VITE_ENABLE_MOCK_API;
  return configured === "true";
}

function LoginPage() {
  const { t } = useTranslation("common");
  const { completeLogin, startExternalLogin } = useAuth();
  const navigate = useNavigate();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSignIn() {
    setError(null);
    setSubmitting(true);

    try {
      if (shouldUseMockLoginExchange()) {
        const result = await completeLogin({
          provider: MOCK_LOGIN_PROVIDER,
          subject: MOCK_LOGIN_SUBJECT,
          email: MOCK_LOGIN_EMAIL,
          displayName: MOCK_LOGIN_DISPLAY_NAME,
        });
        if (result.status === "organizationSelectionRequired") {
          await navigate({ to: "/select-organization" });
          return;
        }
        await navigate({ to: "/" });
        return;
      }

      startExternalLogin("/login/callback");
    } catch {
      setError(t("login.failed"));
      setSubmitting(false);
    }
  }

  return (
    <main className="grid min-h-dvh place-items-center bg-muted/30 p-4">
      <div className="w-full max-w-md space-y-5 rounded-lg border bg-background p-6 shadow-sm">
        <div>
          <h1 className="text-xl font-semibold">{t("login.title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("login.subtitle")}</p>
        </div>
        {error ? (
          <p className="text-sm text-destructive" role="alert">
            {error}
          </p>
        ) : null}
        <Button
          type="button"
          className="w-full"
          disabled={submitting}
          onClick={() => {
            void handleSignIn();
          }}
        >
          {submitting ? t("login.submitting") : t("login.submit")}
        </Button>
      </div>
    </main>
  );
}
