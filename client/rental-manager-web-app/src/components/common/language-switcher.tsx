import { Languages } from "lucide-react";
import { useTranslation } from "react-i18next";

import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import type { AppLanguage } from "@/i18n";
import { cn } from "@/lib/utils";

export function LanguageSwitcher({ compact = false }: { compact?: boolean }) {
  const { t, i18n } = useTranslation("common");
  const language: AppLanguage = i18n.resolvedLanguage === "en" ? "en" : "vi";

  return (
    <div className="flex items-center gap-2">
      <Languages className={cn("h-4 w-4 text-muted-foreground", compact && "hidden sm:block")} aria-hidden="true" />
      <Select
        value={language}
        onValueChange={(value: AppLanguage) => void i18n.changeLanguage(value)}
      >
        <SelectTrigger
          className={cn("h-10", compact ? "w-[88px] sm:w-[116px]" : "w-[132px]")}
          aria-label={t("language.label")}
        >
          <SelectValue />
        </SelectTrigger>
        <SelectContent align="end">
          <SelectItem value="vi">{t("language.vi")}</SelectItem>
          <SelectItem value="en">{t("language.en")}</SelectItem>
        </SelectContent>
      </Select>
    </div>
  );
}
