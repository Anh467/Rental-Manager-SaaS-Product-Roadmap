import { useState } from "react";
import { createFileRoute, redirect, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";

import { Button } from "@/components/ui/button";
import { useAuth } from "@/features/auth/auth-provider";
import { getPendingOrganizationSelection } from "@/features/auth/organization-selection";

export const Route = createFileRoute("/select-organization")({
  beforeLoad: () => {
    if (!getPendingOrganizationSelection()) {
      throw redirect({ to: "/login", replace: true });
    }
  },
  component: SelectOrganizationPage,
});

function SelectOrganizationPage() {
  const { t } = useTranslation("common");
  const { completeOrganizationSelection } = useAuth();
  const navigate = useNavigate();
  const pending = getPendingOrganizationSelection();
  const [submittingId, setSubmittingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  if (!pending) return null;

  return (
    <main className="grid min-h-dvh place-items-center bg-muted/30 p-4">
      <div className="w-full max-w-md space-y-5 rounded-lg border bg-background p-6 shadow-sm">
        <div>
          <h1 className="text-xl font-semibold">{t("selectOrganization.title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("selectOrganization.subtitle")}</p>
        </div>

        <ul className="space-y-2">
          {pending.organizations.map((organization) => (
            <li key={organization.id}>
              <Button
                type="button"
                variant="outline"
                className="h-auto w-full justify-start px-4 py-3 text-left"
                disabled={submittingId !== null}
                onClick={async () => {
                  setError(null);
                  setSubmittingId(organization.id);
                  try {
                    await completeOrganizationSelection(organization.id);
                    await navigate({ to: "/" });
                  } catch {
                    setError(t("selectOrganization.failed"));
                    setSubmittingId(null);
                  }
                }}
              >
                {submittingId === organization.id
                  ? t("selectOrganization.submitting")
                  : organization.name}
              </Button>
            </li>
          ))}
        </ul>

        {error ? <p className="text-sm text-destructive">{error}</p> : null}
      </div>
    </main>
  );
}
