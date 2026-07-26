import type { GlobalFieldType } from "@/api/routes/global-field-types";

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

export function getMultiSelectFieldTypeId(types: readonly GlobalFieldType[]) {
  return types.find((type) => type.key === "multi_select")?.id;
}

export function isMultiSelectFieldTypeId(
  types: readonly GlobalFieldType[],
  fieldTypeId: number | string,
) {
  const multiSelectId = getMultiSelectFieldTypeId(types);
  if (multiSelectId === undefined) return false;
  return Number(fieldTypeId) === multiSelectId;
}
