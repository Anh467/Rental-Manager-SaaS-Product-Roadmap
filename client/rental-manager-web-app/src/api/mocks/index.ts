import MockAdapter from "axios-mock-adapter";

import { v1Client } from "@/api/client";
import { registerMockHandlers } from "@/api/mocks/register-handlers";

let mockAdapter: MockAdapter | undefined;

export function isMockApiEnabled() {
  const configured = import.meta.env.VITE_ENABLE_MOCK_API;
  if (configured === "true") return true;
  if (configured === "false") return false;
  return import.meta.env.DEV;
}

export function startMockApi() {
  if (mockAdapter) return mockAdapter;

  mockAdapter = new MockAdapter(v1Client.axios, {
    delayResponse: Number(import.meta.env.VITE_MOCK_API_DELAY ?? 250),
  });
  registerMockHandlers(mockAdapter);
  return mockAdapter;
}

export function stopMockApi() {
  mockAdapter?.restore();
  mockAdapter = undefined;
}
