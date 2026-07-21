import type { ReactNode } from "react";

import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { cn } from "@/lib/utils";

export type MobileDataCardField = {
  label: ReactNode;
  value: ReactNode;
  fullWidth?: boolean;
};

export type MobileDataCardProps = {
  title: ReactNode;
  subtitle?: ReactNode;
  status?: ReactNode;
  fields?: MobileDataCardField[];
  actions?: ReactNode;
  className?: string;
};

export function MobileDataCard({
  title,
  subtitle,
  status,
  fields = [],
  actions,
  className,
}: MobileDataCardProps) {
  return (
    <Card className={cn("min-w-0 overflow-hidden shadow-none", className)}>
      <CardHeader className="flex-row items-start justify-between gap-3 space-y-0 p-4">
        <div className="min-w-0 flex-1">
          <div className="break-words font-semibold leading-6">{title}</div>
          {subtitle ? <div className="mt-0.5 break-words text-sm text-muted-foreground">{subtitle}</div> : null}
        </div>
        {status ? <div className="shrink-0">{status}</div> : null}
      </CardHeader>

      {fields.length > 0 || actions ? (
        <CardContent className="space-y-4 p-4 pt-0">
          {fields.length > 0 ? (
            <dl className="grid grid-cols-2 gap-x-4 gap-y-3 text-sm">
              {fields.map((field, index) => (
                <div key={index} className={cn("min-w-0", field.fullWidth && "col-span-2")}>
                  <dt className="text-xs font-medium text-muted-foreground">{field.label}</dt>
                  <dd className="mt-1 break-words text-foreground">{field.value}</dd>
                </div>
              ))}
            </dl>
          ) : null}
          {actions ? (
            <div className="flex flex-col gap-2 border-t pt-3 [&>*]:w-full">
              {actions}
            </div>
          ) : null}
        </CardContent>
      ) : null}
    </Card>
  );
}
