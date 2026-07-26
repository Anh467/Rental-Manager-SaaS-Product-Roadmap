import { v1Client, type ApiRequestOptions } from "@/api/client";

import type { GlobalFieldType } from "./types";

const basePath = "/api/v1/global/field-types";

export function getGlobalFieldTypes(options?: ApiRequestOptions) {
  return v1Client.get<GlobalFieldType[]>(basePath, options);
}
