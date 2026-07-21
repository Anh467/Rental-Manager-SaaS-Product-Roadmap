/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL?: string;
  readonly VITE_ENABLE_MOCK_API?: "true" | "false";
  readonly VITE_MOCK_API_DELAY?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
