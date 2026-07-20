import type {
  FieldPath,
  FieldValues,
  UseFormReturn,
} from "react-hook-form";

export type ApiErrorPayload = {
  code?: string;
  messageCode?: string;
  message?: string;
  title?: string;
  detail?: string;
  errors?: Record<string, string | string[]>;
  fieldErrors?: Record<string, string | string[]>;
};

type ErrorWithResponse = {
  response?: {
    data?: unknown;
  };
  data?: unknown;
  message?: string;
};

export type ServerErrorOptions<TValues extends FieldValues> = {
  fieldMap?: Partial<Record<string, FieldPath<TValues>>>;
  fallbackMessage?: string;
};

const DEFAULT_ERROR_MESSAGE =
  "Không thể xử lý yêu cầu. Vui lòng kiểm tra lại thông tin và thử lại.";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function lowerFirst(value: string) {
  return value.length === 0
    ? value
    : `${value.charAt(0).toLowerCase()}${value.slice(1)}`;
}

function normalizeFieldName(fieldName: string) {
  return fieldName
    .replace(/\[(\d+)\]/g, ".$1")
    .split(".")
    .map((part, index) => (index === 0 ? lowerFirst(part) : part))
    .join(".");
}

function toMessage(value: string | string[]) {
  return Array.isArray(value) ? value.filter(Boolean).join(" ") : value;
}

export function getApiErrorPayload(error: unknown): ApiErrorPayload {
  if (!isRecord(error)) {
    return {};
  }

  const typedError = error as ErrorWithResponse;
  const payload = typedError.response?.data ?? typedError.data ?? error;

  return isRecord(payload) ? (payload as ApiErrorPayload) : {};
}

export function getApiErrorMessage(
  error: unknown,
  fallbackMessage = DEFAULT_ERROR_MESSAGE,
) {
  const payload = getApiErrorPayload(error);

  return (
    payload.message ??
    payload.detail ??
    payload.title ??
    (isRecord(error) && typeof error.message === "string"
      ? error.message
      : fallbackMessage)
  );
}

export function applyServerErrors<TValues extends FieldValues>(
  form: UseFormReturn<TValues>,
  error: unknown,
  options: ServerErrorOptions<TValues> = {},
) {
  const payload = getApiErrorPayload(error);
  const fieldErrors = payload.errors ?? payload.fieldErrors;
  let appliedFieldError = false;

  if (fieldErrors) {
    for (const [serverFieldName, serverMessage] of Object.entries(fieldErrors)) {
      const normalizedName = normalizeFieldName(serverFieldName);
      const mappedName =
        options.fieldMap?.[serverFieldName] ??
        options.fieldMap?.[normalizedName] ??
        (normalizedName as FieldPath<TValues>);
      const message = toMessage(serverMessage);

      if (!message) {
        continue;
      }

      form.setError(mappedName, {
        type: "server",
        message,
      });
      appliedFieldError = true;
    }
  }

  const rootMessage = getApiErrorMessage(error, options.fallbackMessage);

  if (!appliedFieldError || payload.message || payload.detail || payload.title) {
    form.setError("root.server", {
      type: payload.code ?? payload.messageCode ?? "server",
      message: rootMessage,
    });
  }

  return {
    code: payload.code ?? payload.messageCode,
    message: rootMessage,
    appliedFieldError,
  };
}
