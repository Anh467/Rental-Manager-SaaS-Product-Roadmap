import type { ReactNode } from "react";
import { AlertCircle, Loader2 } from "lucide-react";
import { useTranslation } from "react-i18next";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export { EmptyState } from "./empty-state";
export { PageHeader } from "./page-header";

export function PageContent({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <div
      className={cn(
        "safe-area-px space-y-5 px-4 py-5 sm:space-y-6 sm:px-6 sm:py-6 lg:p-8",
        className,
      )}
    >
      {children}
    </div>
  );
}

export function PageToolbar({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex min-w-0 flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center sm:justify-between [&>*]:min-w-0",
        className,
      )}
    >
      {children}
    </div>
  );
}

export function LoadingState({ label }: { label?: string }) {
  const { t } = useTranslation("common");
  return <State icon={<Loader2 className="h-6 w-6 animate-spin" />} title={label ?? t("state.loading")} />;
}

export function ErrorState({
  title,
  description,
  onRetry,
}: {
  title?: string;
  description?: string;
  onRetry?: () => void;
}) {
  const { t } = useTranslation("common");

  return (
    <State
      icon={<AlertCircle className="h-6 w-6" />}
      title={title ?? t("state.loadError")}
      description={description}
      action={onRetry ? <Button variant="outline" className="w-full sm:w-auto" onClick={onRetry}>{t("actions.retry")}</Button> : null}
    />
  );
}

function State({
  icon,
  title,
  description,
  action,
}: {
  icon: ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <div className="flex min-h-40 flex-col items-center justify-center rounded-lg border border-dashed p-5 text-center sm:min-h-48 sm:p-8">
      <div className="mb-3 text-muted-foreground">{icon}</div>
      <h3 className="max-w-full break-words font-medium">{title}</h3>
      {description ? <p className="mt-1 max-w-md break-words text-sm text-muted-foreground">{description}</p> : null}
      {action ? <div className="mt-4 w-full sm:w-auto">{action}</div> : null}
    </div>
  );
}
