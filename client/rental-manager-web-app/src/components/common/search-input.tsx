import { Search, X } from "lucide-react";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { cn } from "@/lib/utils";

export type SearchInputProps = {
  value?: string;
  onSearch: (value: string) => void;
  placeholder?: string;
  delay?: number;
  className?: string;
  clearLabel?: string;
};

export function SearchInput({
  value = "",
  onSearch,
  placeholder,
  delay = 350,
  className,
  clearLabel,
}: SearchInputProps) {
  const { t } = useTranslation("common");
  const [inputValue, setInputValue] = useState(value);
  const debouncedValue = useDebouncedValue(inputValue, delay);

  useEffect(() => setInputValue(value), [value]);
  useEffect(() => onSearch(debouncedValue.trim()), [debouncedValue, onSearch]);

  return (
    <div className={cn("relative min-w-0", className)}>
      <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
      <Input
        type="search"
        value={inputValue}
        onChange={(event) => setInputValue(event.target.value)}
        placeholder={placeholder ?? t("actions.search")}
        className="h-11 pl-9 pr-11 text-base sm:h-10 sm:text-sm [&::-webkit-search-cancel-button]:hidden"
      />
      {inputValue ? (
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="absolute right-0 top-1/2 h-11 w-11 -translate-y-1/2 sm:h-10 sm:w-10"
          onClick={() => setInputValue("")}
          aria-label={clearLabel ?? t("actions.clearSearch")}
        >
          <X className="h-4 w-4" />
        </Button>
      ) : null}
    </div>
  );
}
