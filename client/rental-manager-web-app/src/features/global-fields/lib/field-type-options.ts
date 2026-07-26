import type { GlobalFieldType } from "@/api/routes/global-field-types";

const OPTION_FIELD_TYPE_KEYS = new Set(["selection", "multi_select"]);

export function toFieldTypeSelectOptions(types: readonly GlobalFieldType[]) {
  return types.map((type) => ({
    value: String(type.id),
    label: type.name,
  }));
}

export function getFieldTypeName(
  types: readonly GlobalFieldType[],
  fieldTypeId: number,
  fallback?: string,
) {
  return types.find((type) => type.id === fieldTypeId)?.name ?? fallback ?? String(fieldTypeId);
}

export function fieldTypeRequiresOptions(
  types: readonly GlobalFieldType[],
  fieldTypeId: number | string,
) {
  const type = types.find((item) => item.id === Number(fieldTypeId));
  return type !== undefined && OPTION_FIELD_TYPE_KEYS.has(type.key);
}
