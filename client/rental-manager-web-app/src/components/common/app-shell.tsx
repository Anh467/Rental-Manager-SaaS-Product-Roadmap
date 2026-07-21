import { useState, type ReactNode } from "react";
import { Building2, DoorOpen, LayoutDashboard, Menu } from "lucide-react";
import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";

import { LanguageSwitcher } from "@/components/common/language-switcher";
import { Button } from "@/components/ui/button";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet";
import { cn } from "@/lib/utils";

const navigation = [
  { to: "/", labelKey: "navigation.baseline", icon: LayoutDashboard },
  { to: "/properties", labelKey: "navigation.properties", icon: Building2 },
  { to: "/rooms", labelKey: "navigation.rooms", icon: DoorOpen },
] as const;

function NavigationLinks({ onNavigate }: { onNavigate?: () => void }) {
  const { t } = useTranslation("common");

  return (
    <nav className="flex flex-col gap-1" aria-label={t("navigation.menuTitle")}>
      {navigation.map(({ to, labelKey, icon: Icon }) => (
        <Link
          key={to}
          to={to}
          activeOptions={{ exact: to === "/" }}
          onClick={onNavigate}
          className="flex min-h-11 items-center gap-3 rounded-md px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          activeProps={{ className: cn("bg-primary/10 font-medium text-primary") }}
        >
          <Icon className="h-5 w-5 shrink-0" aria-hidden="true" />
          <span className="min-w-0 truncate">{t(labelKey)}</span>
        </Link>
      ))}
    </nav>
  );
}

export function AppShell({ children }: { children: ReactNode }) {
  const { t } = useTranslation("common");
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  return (
    <div className="min-h-dvh overflow-x-hidden bg-muted/30">
      <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/80">
        <div className="safe-area-px mx-auto flex h-16 max-w-screen-2xl items-center gap-2 px-3 sm:gap-4 sm:px-6">
          <Sheet open={mobileMenuOpen} onOpenChange={setMobileMenuOpen}>
            <SheetTrigger asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="h-11 w-11 shrink-0 lg:hidden"
                aria-label={t("navigation.openMenu")}
                data-testid="mobile-menu-trigger"
              >
                <Menu className="h-5 w-5" />
              </Button>
            </SheetTrigger>
            <SheetContent side="left" className="w-[min(88vw,20rem)] p-4" data-testid="mobile-navigation-drawer">
              <SheetHeader className="mb-4">
                <SheetTitle>{t("navigation.menuTitle")}</SheetTitle>
                <SheetDescription>{t("brand.organization")}</SheetDescription>
              </SheetHeader>
              <NavigationLinks onNavigate={() => setMobileMenuOpen(false)} />
            </SheetContent>
          </Sheet>

          <div className="flex min-w-0 flex-1 items-center gap-2 sm:gap-3">
            <div className="hidden shrink-0 rounded-md bg-primary p-2 text-primary-foreground sm:block">
              <Building2 className="h-4 w-4" />
            </div>
            <div className="min-w-0">
              <p className="truncate text-sm font-semibold">{t("brand.name")}</p>
              <p className="truncate text-xs text-muted-foreground">{t("brand.organization")}</p>
            </div>
          </div>

          <div className="flex shrink-0 items-center gap-2 sm:gap-4">
            <span className="hidden text-xs text-muted-foreground xl:inline">
              {t("brand.organizationContext")}
            </span>
            <LanguageSwitcher compact />
          </div>
        </div>
      </header>

      <div className="mx-auto grid min-h-[calc(100dvh-4rem)] max-w-screen-2xl lg:grid-cols-[240px_minmax(0,1fr)]">
        <aside
          className="sticky top-16 hidden h-[calc(100dvh-4rem)] overflow-y-auto border-r bg-background p-3 lg:block"
          data-testid="desktop-sidebar"
        >
          <NavigationLinks />
        </aside>
        <main className="min-w-0 overflow-x-hidden">{children}</main>
      </div>
    </div>
  );
}
