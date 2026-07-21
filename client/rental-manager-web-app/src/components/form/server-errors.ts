import type { FieldPath, FieldValues, UseFormReturn } from "react-hook-form";

import {
  isApiError,
  type ApiErrorResponse,
  type ApiFieldError,
  type ApiMessageParameters,
  type ErrorMessageKey,
} from "@/api/client";
import { translateApiMessage } from "@/i18n/api-message";

export type LegacyApiErrorPayload = {
  code?: string;
  messageCode?: string;
  message?: string;
  title?: string;
  detail?: string;
  messageKey?: ErrorMessageKey;
  parameters?: ApiMessageParameters;
  errors?: Record<string, string | string[]>;
  fieldErrors?: ApiFieldError[] | Record<string, string | string[]>;
};

type ErrorWithResponse = {
  response?: { data?: unknown };
  data?: unknown;
  message?: string;
};

export type ServerErrorOptions<TValues extends FieldValues> = {
  fieldMap?: Partial<Record<string, FieldPath<TValues>>>;
  fallbackMessage?: string;
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function lowerFirst(value: string) {
  return value.length === 0 ? value : `${value.charAt(0).toLowerCase()}${value.slice(1)}`;
}

function normalizeFieldName(fieldName: string) {
  return fieldName
    .replace(/\[(\d+)\]/g, ".$1")
    .split(".")
    .map((part) => lowerFirst(part))
    .join(".");
}

function toLegacyMessage(value: string | string[]) {
  return Array.isArray(value) ? value.filter(Boolean).join(" ") : value;
}

function getCandidate(error: unknown): unknown {
  if (!isRecord(error)) return error;
  const typedError = error as ErrorWithResponse;
  return typedError.response?.data ?? typedError.data ?? error;
}

export function getApiErrorPayload(error: unknown): LegacyApiErrorPayload {
  if (isApiError(error)) {
    return {
      messageKey: error.messageKey,
      parameters: error.parameters,
      fieldErrors: error.fieldErrors,
      message: error.message,
    };
  }

  const candidate = getCandidate(error);
  return isRecord(candidate) ? (candidate as LegacyApiErrorPayload) : {};
}

function isStructuredFieldError(value: unknown): value is ApiFieldError {
  return (
    isRecord(value) &&
    typeof value.fieldKey === "string" &&
    typeof value.messageKey === "string"
  );
}

function getStructuredFieldErrors(payload: LegacyApiErrorPayload) {
  return Array.isArray(payload.fieldErrors)
    ? payload.fieldErrors.filter(isStructuredFieldError)
    : [];
}

export function getApiErrorMessage(error: unknown, fallbackMessage?: string) {
  const payload = getApiErrorPayload(error);
  if (payload.messageKey) {
    return translateApiMessage(payload.messageKey, payload.parameters);
  }

  return (
    payload.message ??
    payload.detail ??
    payload.title ??
    (isRecord(error) && typeof error.message === "string" ? error.message : undefined) ??
    fallbackMessage ??
    translateApiMessage("ERR-050")
  );
}

export function applyServerErrors<TValues extends FieldValues>(
  form: UseFormReturn<TValues>,
  error: unknown,
  options: ServerErrorOptions<TValues> = {},
) {
  const payload = getApiErrorPayload(error);
  let appliedFieldError = false;

  for (const fieldError of getStructuredFieldErrors(payload)) {
    const normalizedName = normalizeFieldName(fieldError.fieldKey);
    const mappedName =
      options.fieldMap?.[fieldError.fieldKey] ??
      options.fieldMap?.[normalizedName] ??
      (normalizedName as FieldPath<TValues>);

    form.setError(mappedName, {
      type: fieldError.messageKey,
      message: translateApiMessage(fieldError.messageKey, fieldError.parameters),
    });
    appliedFieldError = true;
  }

  const legacyFieldErrors = payload.errors ??
    (!Array.isArray(payload.fieldErrors) ? payload.fieldErrors : undefined);

  if (legacyFieldErrors) {
    for (const [serverFieldName, serverMessage] of Object.entries(legacyFieldErrors)) {
      const normalizedName = normalizeFieldName(serverFieldName);
      const mappedName =
        options.fieldMap?.[serverFieldName] ??
        options.fieldMap?.[normalizedName] ??
        (normalizedName as FieldPath<TValues>);
      const message = toLegacyMessage(serverMessage);
      if (!message) continue;

      form.setError(mappedName, { type: "server", message });
      appliedFieldError = true;
    }
  }

  const rootMessage = getApiErrorMessage(error, options.fallbackMessage);
  if (!appliedFieldError || payload.messageKey !== "ERR-001") {
    form.setError("root.server", {
      type: payload.messageKey ?? payload.code ?? payload.messageCode ?? "server",
      message: rootMessage,
    });
  }

  return {
    code: payload.messageKey ?? payload.code ?? payload.messageCode,
    message: rootMessage,
    appliedFieldError,
  };
}

export type { ApiErrorResponse };
