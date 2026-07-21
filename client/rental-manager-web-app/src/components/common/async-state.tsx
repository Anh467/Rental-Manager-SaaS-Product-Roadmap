import type { ReactNode } from "react";
import { AlertCircle, LoaderCircle } from "lucide-react";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";

export type AsyncStateProps = {
  isLoading: boolean;
  error?: unknown;
  children: ReactNode;
  loadingLabel?: string;
  onRetry?: () => void;
};

function getErrorMessage(error: unknown) {
  if (error && typeof error === "object") {
    const candidate = error as { message?: unknown; detail?: unknown; title?: unknown };
    if (typeof candidate.message === "string") return candidate.message;
    if (typeof candidate.detail === "string") return candidate.detail;
    if (typeof candidate.title === "string") return candidate.title;
  }
  return "Không thể tải dữ liệu. Vui lòng thử lại.";
}

export function AsyncState({ isLoading, error, children, loadingLabel = "Đang tải dữ liệu...", onRetry }: AsyncStateProps) {
  if (isLoading) {
    return (
      <div className="flex min-h-40 items-center justify-center gap-2 text-sm text-muted-foreground" role="status">
        <LoaderCircle className="h-4 w-4 animate-spin" />
        {loadingLabel}
      </div>
    );
  }

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertCircle className="h-4 w-4" />
        <AlertTitle>Đã xảy ra lỗi</AlertTitle>
        <AlertDescription className="flex items-center justify-between gap-4">
          <span>{getErrorMessage(error)}</span>
          {onRetry ? <Button type="button" variant="outline" size="sm" onClick={onRetry}>Thử lại</Button> : null}
        </AlertDescription>
      </Alert>
    );
  }

  return <>{children}</>;
}
