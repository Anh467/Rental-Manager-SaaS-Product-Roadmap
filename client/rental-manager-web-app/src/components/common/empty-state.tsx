import type { LucideIcon } from "lucide-react";
import { Inbox } from "lucide-react";
import type { ReactNode } from "react";

import { cn } from "@/lib/utils";

export type EmptyStateProps = {
  title: string;
  description?: string;
  action?: ReactNode;
  icon?: LucideIcon;
  className?: string;
};

export function EmptyState({ title, description, action, icon: Icon = Inbox, className }: EmptyStateProps) {
  return (
    <div className={cn("flex min-h-44 min-w-0 flex-col items-center justify-center rounded-lg border border-dashed p-5 text-center sm:min-h-56 sm:p-8", className)}>
      <div className="mb-4 rounded-full bg-muted p-3"><Icon className="h-5 w-5 text-muted-foreground" /></div>
      <h3 className="max-w-full break-words font-semibold">{title}</h3>
      {description ? <p className="mt-1 max-w-md break-words text-sm leading-6 text-muted-foreground">{description}</p> : null}
      {action ? <div className="mt-4 w-full sm:w-auto [&>*]:w-full sm:[&>*]:w-auto">{action}</div> : null}
    </div>
  );
}
