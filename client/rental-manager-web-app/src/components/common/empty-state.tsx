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
    <div className={cn("flex min-h-56 flex-col items-center justify-center rounded-lg border border-dashed p-8 text-center", className)}>
      <div className="mb-4 rounded-full bg-muted p-3"><Icon className="h-5 w-5 text-muted-foreground" /></div>
      <h3 className="font-semibold">{title}</h3>
      {description ? <p className="mt-1 max-w-md text-sm text-muted-foreground">{description}</p> : null}
      {action ? <div className="mt-4">{action}</div> : null}
    </div>
  );
}
