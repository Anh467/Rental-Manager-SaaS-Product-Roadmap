import type { ReactNode } from "react";
import { Building2, DoorOpen, LayoutDashboard } from "lucide-react";
import { Link } from "@tanstack/react-router";

import { cn } from "@/lib/utils";

const navigation = [
  { to: "/", label: "Baseline", icon: LayoutDashboard },
  { to: "/properties", label: "Khu nhà", icon: Building2 },
  { to: "/rooms", label: "Phòng", icon: DoorOpen },
] as const;

export function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen bg-muted/30">
      <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur">
        <div className="mx-auto flex h-16 max-w-screen-2xl items-center justify-between px-4 sm:px-6">
          <div className="flex items-center gap-3">
            <div className="rounded-md bg-primary p-2 text-primary-foreground">
              <Building2 className="h-4 w-4" />
            </div>
            <div>
              <p className="text-sm font-semibold">Rental Manager</p>
              <p className="text-xs text-muted-foreground">Nhà trọ Minh Anh</p>
            </div>
          </div>
          <span className="text-xs text-muted-foreground">Organization context</span>
        </div>
      </header>

      <div className="mx-auto grid max-w-screen-2xl md:grid-cols-[220px_minmax(0,1fr)]">
        <aside className="border-r bg-background p-3 md:min-h-[calc(100vh-4rem)]">
          <nav className="flex gap-2 overflow-auto md:flex-col">
            {navigation.map(({ to, label, icon: Icon }) => (
              <Link
                key={to}
                to={to}
                activeOptions={{ exact: to === "/" }}
                className="flex shrink-0 items-center gap-2 rounded-md px-3 py-2 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
                activeProps={{ className: cn("bg-primary/10 text-primary font-medium") }}
              >
                <Icon className="h-4 w-4" />
                {label}
              </Link>
            ))}
          </nav>
        </aside>
        <main className="min-w-0">{children}</main>
      </div>
    </div>
  );
}
