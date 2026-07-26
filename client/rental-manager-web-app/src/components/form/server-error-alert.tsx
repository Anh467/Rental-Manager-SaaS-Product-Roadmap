import { useState } from "react";
import { AlertTriangle, Copy, RefreshCw } from "lucide-react";

import { Button } from "@/components/ui/button";

export function FormErrorAlert({
  message,
  correlationId,
}: {
  message: string;
  correlationId?: string;
}) {
  const [copied, setCopied] = useState(false);

  const copyCorrelationId = async () => {
    if (!correlationId) return;
    await navigator.clipboard.writeText(correlationId);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  };

  return (
    <div role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
      <p>{message}</p>
      {correlationId ? (
        <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
          <span>Correlation ID: {correlationId}</span>
          <Button type="button" variant="ghost" size="sm" className="h-7 px-2" onClick={() => void copyCorrelationId()}>
            <Copy className="mr-1 h-3.5 w-3.5" />
            {copied ? "Copied" : "Copy"}
          </Button>
        </div>
      ) : null}
    </div>
  );
}

export function RowVersionConflictAlert({ onReload }: { onReload: () => void }) {
  return (
    <div role="alert" className="flex flex-wrap items-center gap-3 rounded-md border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm">
      <AlertTriangle className="h-4 w-4 text-amber-700" />
      <span className="flex-1">This record was updated by another user.</span>
      <Button type="button" variant="outline" size="sm" onClick={onReload}>
        <RefreshCw className="mr-2 h-4 w-4" /> Reload
      </Button>
    </div>
  );
}
