import { Search, X } from "lucide-react";
import { useEffect, useState } from "react";

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
};

export function SearchInput({ value = "", onSearch, placeholder = "Tìm kiếm...", delay = 350, className }: SearchInputProps) {
  const [inputValue, setInputValue] = useState(value);
  const debouncedValue = useDebouncedValue(inputValue, delay);

  useEffect(() => setInputValue(value), [value]);
  useEffect(() => onSearch(debouncedValue.trim()), [debouncedValue, onSearch]);

  return (
    <div className={cn("relative", className)}>
      <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
      <Input
        value={inputValue}
        onChange={(event) => setInputValue(event.target.value)}
        placeholder={placeholder}
        className="pl-9 pr-9"
      />
      {inputValue ? (
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="absolute right-1 top-1/2 h-7 w-7 -translate-y-1/2"
          onClick={() => setInputValue("")}
          aria-label="Xóa tìm kiếm"
        >
          <X className="h-4 w-4" />
        </Button>
      ) : null}
    </div>
  );
}
