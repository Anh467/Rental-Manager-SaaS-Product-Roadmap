import { Badge, type BadgeProps } from "@/components/ui/badge";

export type StatusDefinition = {
  label: string;
  variant: NonNullable<BadgeProps["variant"]>;
};

export type StatusBadgeProps<TStatus extends string | number> = {
  status: TStatus;
  definitions: Partial<Record<TStatus, StatusDefinition>>;
};

export function StatusBadge<TStatus extends string | number>({ status, definitions }: StatusBadgeProps<TStatus>) {
  const definition = definitions[status];
  if (!definition) {
    return <Badge variant="secondary">{String(status)}</Badge>;
  }
  return <Badge variant={definition.variant}>{definition.label}</Badge>;
}
