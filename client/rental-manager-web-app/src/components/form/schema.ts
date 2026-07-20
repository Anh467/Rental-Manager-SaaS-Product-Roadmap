import { z } from "zod";

export const validationMessages = {
  required: (field: string) => `${field} là bắt buộc.`,
  invalidEmail: "Email không đúng định dạng.",
  invalidNumber: (field: string) => `${field} phải là số hợp lệ.`,
  minLength: (field: string, min: number) =>
    `${field} phải có ít nhất ${min} ký tự.`,
  maxLength: (field: string, max: number) =>
    `${field} không được vượt quá ${max} ký tự.`,
  minNumber: (field: string, min: number) =>
    `${field} phải lớn hơn hoặc bằng ${min}.`,
  invalidDate: (field: string) => `${field} không phải ngày hợp lệ.`,
} as const;

export function requiredText(field: string, max = 256) {
  return z
    .string({ required_error: validationMessages.required(field) })
    .trim()
    .min(1, validationMessages.required(field))
    .max(max, validationMessages.maxLength(field, max));
}

export function optionalText(max = 1000) {
  return z.preprocess(
    (value) => (typeof value === "string" && value.trim() === "" ? undefined : value),
    z.string().trim().max(max).optional(),
  );
}

export function requiredEmail(field = "Email") {
  return requiredText(field, 320).email(validationMessages.invalidEmail);
}

export function requiredNumber(field: string, min?: number) {
  let schema = z.number({
    required_error: validationMessages.required(field),
    invalid_type_error: validationMessages.invalidNumber(field),
  });

  if (min !== undefined) {
    schema = schema.min(min, validationMessages.minNumber(field, min));
  }

  return z.preprocess(
    (value) => {
      if (value === "" || value === null || value === undefined) {
        return undefined;
      }

      if (typeof value === "string") {
        const parsed = Number(value);
        return Number.isNaN(parsed) ? value : parsed;
      }

      return value;
    },
    schema,
  );
}

export function optionalNumber(field: string, min?: number) {
  let schema = z.number({
    invalid_type_error: validationMessages.invalidNumber(field),
  });

  if (min !== undefined) {
    schema = schema.min(min, validationMessages.minNumber(field, min));
  }

  return z.preprocess(
    (value) => {
      if (value === "" || value === null || value === undefined) {
        return undefined;
      }

      if (typeof value === "string") {
        const parsed = Number(value);
        return Number.isNaN(parsed) ? value : parsed;
      }

      return value;
    },
    schema.optional(),
  );
}

export function requiredDateString(field: string) {
  return requiredText(field, 10).refine((value) => {
    const date = new Date(`${value}T00:00:00`);
    return /^\d{4}-\d{2}-\d{2}$/.test(value) && !Number.isNaN(date.getTime());
  }, validationMessages.invalidDate(field));
}

export const optionalBoolean = z.boolean().default(false);
