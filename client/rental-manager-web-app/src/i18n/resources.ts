import enCommon from "@/locales/en/common.json";
import enError from "@/locales/en/error.json";
import enProperty from "@/locales/en/property.json";
import enRoom from "@/locales/en/room.json";
import enSuccess from "@/locales/en/success.json";
import viCommon from "@/locales/vi/common.json";
import viError from "@/locales/vi/error.json";
import viProperty from "@/locales/vi/property.json";
import viRoom from "@/locales/vi/room.json";
import viSuccess from "@/locales/vi/success.json";

export const namespaces = ["common", "property", "room", "success", "error"] as const;
export type AppNamespace = (typeof namespaces)[number];

export const resources = {
  en: {
    common: enCommon,
    property: enProperty,
    room: enRoom,
    success: enSuccess,
    error: enError,
  },
  vi: {
    common: viCommon,
    property: viProperty,
    room: viRoom,
    success: viSuccess,
    error: viError,
  },
} as const;

export type AppLanguage = keyof typeof resources;
