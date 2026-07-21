import type { ReactNode } from "react";
import { Building2, DoorOpen, LayoutDashboard } from "lucide-react";
import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";

import { LanguageSwitcher } from "@/components/common/language-switcher";
import { cn } from "@/lib/utils";

const navigation = [
  { to: "/", labelKey: "navigation.baseline", icon: LayoutDashboard },
  { to: "/properties", labelKey: "navigation.properties", icon: Building2 },
  { to: "/rooms", labelKey: "navigation.rooms", icon: DoorOpen },
] as const;

export function AppShell({ children }: { children: ReactNode }) {
  const { t } = useTranslation("common");

  return (
    <div className="min-h-screen bg-muted/30">
      <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur">
        <div className="mx-auto flex h-16 max-w-screen-2xl items-center justify-between gap-4 px-4 sm:px-6">
          <div className="flex items-center gap-3">
            <div className="rounded-md bg-primary p-2 text-primary-foreground">
              <Building2 className="h-4 w-4" />
            </div>
            <div>
              <p className="text-sm font-semibold">{t("brand.name")}</p>
              <p className="text-xs text-muted-foreground">{t("brand.organization")}</p>
            </div>
          </div>
          <div className="flex items-center gap-4">
            <span className="hidden text-xs text-muted-foreground lg:inline">
              {t("brand.organizationContext")}
            </span>
            <LanguageSwitcher />
          </div>
        </div>
      </header>

      <div className="mx-auto grid max-w-screen-2xl md:grid-cols-[220px_minmax(0,1fr)]">
        <aside className="border-r bg-background p-3 md:min-h-[calc(100vh-4rem)]">
          <nav className="flex gap-2 overflow-auto md:flex-col">
            {navigation.map(({ to, labelKey, icon: Icon }) => (
              <Link
                key={to}
                to={to}
                activeOptions={{ exact: to === "/" }}
                className="flex shrink-0 items-center gap-2 rounded-md px-3 py-2 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
                activeProps={{ className: cn("bg-primary/10 text-primary font-medium") }}
              >
                <Icon className="h-4 w-4" />
                {t(labelKey)}
              </Link>
            ))}
          </nav>
        </aside>
        <main className="min-w-0">{children}</main>
      </div>
    </div>
  );
}
