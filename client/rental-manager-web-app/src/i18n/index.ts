import i18n from "i18next";
import { initReactI18next } from "react-i18next";

import { namespaces, resources, type AppLanguage } from "./resources";

const LANGUAGE_STORAGE_KEY = "language";
let initialization: Promise<unknown> | undefined;
let languageListenerRegistered = false;

function isSupportedLanguage(value: string | null | undefined): value is AppLanguage {
  return value === "en" || value === "vi";
}

function resolveInitialLanguage(): AppLanguage {
  const stored = localStorage.getItem(LANGUAGE_STORAGE_KEY);
  if (isSupportedLanguage(stored)) return stored;
  return navigator.language.toLowerCase().startsWith("vi") ? "vi" : "en";
}

export function initializeI18n() {
  if (!initialization) {
    initialization = i18n.use(initReactI18next).init({
      resources,
      lng: resolveInitialLanguage(),
      fallbackLng: "en",
      supportedLngs: ["en", "vi"],
      ns: [...namespaces],
      defaultNS: "common",
      interpolation: { escapeValue: false },
      returnNull: false,
    });

    if (!languageListenerRegistered) {
      i18n.on("languageChanged", (language) => {
        if (isSupportedLanguage(language)) {
          localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
          document.documentElement.lang = language;
        }
      });
      languageListenerRegistered = true;
    }
  }

  return initialization;
}

export { i18n };
export type { AppLanguage } from "./resources";
