import type { ReactNode } from "react";
import { AlertCircle, Inbox, Loader2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export function PageHeader({ title, description, actions }: { title: string; description?: string; actions?: ReactNode }) {
  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
        {description ? <p className="mt-1 text-sm text-muted-foreground">{description}</p> : null}
      </div>
      {actions ? <div className="flex shrink-0 gap-2">{actions}</div> : null}
    </div>
  );
}

export function PageContent({ children, className }: { children: ReactNode; className?: string }) {
  return <div className={cn("space-y-6", className)}>{children}</div>;
}

export function LoadingState({ label = "Đang tải dữ liệu..." }: { label?: string }) {
  return <State icon={<Loader2 className="h-6 w-6 animate-spin" />} title={label} />;
}

export function EmptyState({ title = "Chưa có dữ liệu", description, action }: { title?: string; description?: string; action?: ReactNode }) {
  return <State icon={<Inbox className="h-6 w-6" />} title={title} description={description} action={action} />;
}

export function ErrorState({ title = "Không thể tải dữ liệu", description, onRetry }: { title?: string; description?: string; onRetry?: () => void }) {
  return (
    <State
      icon={<AlertCircle className="h-6 w-6" />}
      title={title}
      description={description}
      action={onRetry ? <Button variant="outline" onClick={onRetry}>Thử lại</Button> : null}
    />
  );
}

function State({ icon, title, description, action }: { icon: ReactNode; title: string; description?: string; action?: ReactNode }) {
  return (
    <div className="flex min-h-48 flex-col items-center justify-center rounded-lg border border-dashed p-8 text-center">
      <div className="mb-3 text-muted-foreground">{icon}</div>
      <h3 className="font-medium">{title}</h3>
      {description ? <p className="mt-1 max-w-md text-sm text-muted-foreground">{description}</p> : null}
      {action ? <div className="mt-4">{action}</div> : null}
    </div>
  );
}
