import { ArrowDown, ArrowUp, Plus, Trash2 } from "lucide-react";
import { useFieldArray, type Control, useWatch } from "react-hook-form";
import { useTranslation } from "react-i18next";

import { StatusBadge } from "@/components/common/status-badge";
import { SwitchFormField, TextareaFormField, TextFormField } from "@/components/form";
import { Button } from "@/components/ui/button";
import type { GlobalFieldFormValues } from "./global-field-form";

export function FieldOptionEditor({
  control,
  disabled,
}: {
  control: Control<GlobalFieldFormValues>;
  disabled?: boolean;
}) {
  const { t } = useTranslation("global-field");
  const { t: commonT } = useTranslation("common");
  const { fields, append, remove, move } = useFieldArray({ control, name: "options" });
  const options = useWatch({ control, name: "options" }) ?? [];
  const statusDefinitions = {
    active: { label: commonT("states.active"), variant: "success" as const },
    inactive: { label: commonT("states.inactive"), variant: "muted" as const },
  };

  return (
    <section className="space-y-3 rounded-md border p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h3 className="font-medium">{t("options.title")}</h3>
          <p className="text-sm text-muted-foreground">{t("options.description")}</p>
        </div>
        {!disabled ? (
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => append({ key: "", name: "", description: "", isActive: true })}
          >
            <Plus className="mr-2 h-4 w-4" />
            {t("options.add")}
          </Button>
        ) : null}
      </div>
      {fields.map((field, index) => (
        <div key={field.id} className="space-y-3 rounded-md border bg-muted/20 p-3">
          <div className="flex items-center justify-between gap-2">
            <p className="text-sm font-medium">{t("options.item", { number: index + 1 })}</p>
            <div className="flex items-center gap-2">
              {disabled ? (
                <StatusBadge
                  status={options[index]?.isActive ? "active" : "inactive"}
                  definitions={statusDefinitions}
                />
              ) : null}
              {!disabled ? (
                <div className="flex gap-1">
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    disabled={index === 0}
                    onClick={() => move(index, index - 1)}
                    aria-label={t("options.moveUp")}
                  >
                    <ArrowUp className="h-4 w-4" />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    disabled={index === fields.length - 1}
                    onClick={() => move(index, index + 1)}
                    aria-label={t("options.moveDown")}
                  >
                    <ArrowDown className="h-4 w-4" />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => remove(index)}
                    aria-label={t("options.remove")}
                  >
                    <Trash2 className="h-4 w-4 text-destructive" />
                  </Button>
                </div>
              ) : null}
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <TextFormField
              control={control}
              name={`options.${index}.name`}
              label={t("options.fields.name")}
              required
              disabled={disabled}
              maxLength={256}
            />
            <TextFormField
              control={control}
              name={`options.${index}.key`}
              label={t("options.fields.key")}
              required
              disabled={disabled}
              maxLength={256}
            />
          </div>
          <TextareaFormField
            control={control}
            name={`options.${index}.description`}
            label={t("options.fields.description")}
            disabled={disabled}
            rows={2}
            maxLength={1028}
          />
          {!disabled ? (
            <SwitchFormField
              control={control}
              name={`options.${index}.isActive`}
              label={t("options.fields.isActive")}
            />
          ) : null}
        </div>
      ))}
    </section>
  );
}
