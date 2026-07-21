import type {
  ApiError,
  ApiMessageParameter,
  ApiMessageParameters,
  MessageKey,
} from "@/api/client";
import { i18n } from "./index";

const PARAMETER_NAMESPACES: Record<string, string> = {
  object: "objects",
  resource: "objects",
  dependency: "objects",
  action: "actions",
  field: "fields",
  fieldKey: "fields",
  service: "services",
  currentState: "states",
  requestedState: "states",
};

function translateParameterValue(
  parameterName: string,
  value: ApiMessageParameter,
): ApiMessageParameter {
  if (Array.isArray(value)) {
    return value.map((item) => translateParameterValue(parameterName, item)).join(", ");
  }

  if (typeof value !== "string") return value;

  const namespace = PARAMETER_NAMESPACES[parameterName];
  if (!namespace) return value;

  const key = `common:${namespace}.${value}`;
  return i18n.exists(key) ? String(i18n.t(key)) : value;
}

export function translateMessageParameters(
  parameters: ApiMessageParameters = {},
): ApiMessageParameters {
  return Object.fromEntries(
    Object.entries(parameters).map(([name, value]) => [
      name,
      translateParameterValue(name, value),
    ]),
  );
}

export function translateApiMessage(
  messageKey: MessageKey,
  parameters: ApiMessageParameters = {},
) {
  const namespace = messageKey.startsWith("SCS-") ? "success" : "error";
  return String(
    i18n.t(`${namespace}:${messageKey}`, {
      ...translateMessageParameters(parameters),
      defaultValue: messageKey,
    }),
  );
}

export function translateApiError(error: ApiError) {
  return translateApiMessage(error.messageKey, error.parameters);
}
