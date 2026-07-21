import { createFileRoute } from "@tanstack/react-router";
import { ShieldX } from "lucide-react";
import { useTranslation } from "react-i18next";

import { EmptyState } from "@/components/common/page";

export const Route = createFileRoute("/_authenticated/access-denied")({
  component: AccessDeniedPage,
});

function AccessDeniedPage() {
  const { t } = useTranslation("common");

  return (
    <div className="p-6">
      <EmptyState
        title={t("accessDenied.title")}
        description={t("accessDenied.description")}
        action={<ShieldX className="h-6 w-6 text-destructive" />}
      />
    </div>
  );
}
