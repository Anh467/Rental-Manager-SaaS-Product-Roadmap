import type { ReactNode } from "react";
import { AlertCircle, LoaderCircle } from "lucide-react";
import { useTranslation } from "react-i18next";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";

export type AsyncStateProps = {
  isLoading: boolean;
  error?: unknown;
  children: ReactNode;
  loadingLabel?: string;
  errorTitle?: string;
  fallbackErrorMessage?: string;
  onRetry?: () => void;
};

function getErrorMessage(error: unknown, fallback: string) {
  if (error && typeof error === "object") {
    const candidate = error as { message?: unknown; detail?: unknown; title?: unknown };
    if (typeof candidate.message === "string") return candidate.message;
    if (typeof candidate.detail === "string") return candidate.detail;
    if (typeof candidate.title === "string") return candidate.title;
  }
  return fallback;
}

export function AsyncState({
  isLoading,
  error,
  children,
  loadingLabel,
  errorTitle,
  fallbackErrorMessage,
  onRetry,
}: AsyncStateProps) {
  const { t } = useTranslation("common");

  if (isLoading) {
    return (
      <div className="flex min-h-40 flex-col items-center justify-center gap-2 px-4 text-center text-sm text-muted-foreground sm:flex-row" role="status">
        <LoaderCircle className="h-4 w-4 shrink-0 animate-spin" />
        <span className="break-words">{loadingLabel ?? t("state.loading")}</span>
      </div>
    );
  }

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertCircle className="h-4 w-4" />
        <AlertTitle>{errorTitle ?? t("state.loadError")}</AlertTitle>
        <AlertDescription className="flex flex-col items-stretch gap-3 sm:flex-row sm:items-center sm:justify-between">
          <span className="min-w-0 break-words">{getErrorMessage(error, fallbackErrorMessage ?? t("state.unknownError"))}</span>
          {onRetry ? <Button type="button" variant="outline" className="w-full sm:w-auto" onClick={onRetry}>{t("actions.retry")}</Button> : null}
        </AlertDescription>
      </Alert>
    );
  }

  return <>{children}</>;
}
