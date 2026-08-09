import { useEffect, useState } from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";

import { useAuth } from "@/features/auth/auth-provider";
import { redirectIfAuthenticated } from "@/features/auth/auth-session";

export const Route = createFileRoute("/login/callback")({
  beforeLoad: async ({ context }) => {
    await redirectIfAuthenticated(context.queryClient);
  },
  component: LoginCallbackPage,
});

function LoginCallbackPage() {
  const { t } = useTranslation("common");
  const { completeLogin } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      try {
        const result = await completeLogin({});
        if (cancelled) return;
        if (result.status === "organizationSelectionRequired") {
          await navigate({ to: "/select-organization", replace: true });
          return;
        }
        await navigate({ to: "/", replace: true });
      } catch {
        if (!cancelled) {
          setError(t("login.failed"));
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [completeLogin, navigate, t]);

  return (
    <main className="grid min-h-dvh place-items-center bg-muted/30 p-4">
      <div className="w-full max-w-md space-y-3 rounded-lg border bg-background p-6 shadow-sm">
        <h1 className="text-xl font-semibold">{t("login.callbackTitle")}</h1>
        {error ? (
          <p className="text-sm text-destructive" role="alert">
            {error}
          </p>
        ) : (
          <p className="text-sm text-muted-foreground">{t("login.callbackSubtitle")}</p>
        )}
      </div>
    </main>
  );
}
