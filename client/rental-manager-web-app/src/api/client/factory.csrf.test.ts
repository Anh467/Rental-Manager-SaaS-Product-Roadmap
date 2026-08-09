import MockAdapter from "axios-mock-adapter";
import { afterEach, describe, expect, it } from "vitest";

import { clearCsrfToken, getCachedCsrfToken } from "./csrf";
import { createApiClient } from "./factory";

function success(data: unknown) {
  return { success: true, messageKey: "SCS-005", data, correlationId: "test-correlation-id" };
}

function failure(messageKey: string) {
  return { success: false, messageKey, correlationId: "test-correlation-id" };
}

describe("createApiClient CSRF behavior", () => {
  afterEach(() => {
    clearCsrfToken();
  });

  it("attaches X-CSRF-TOKEN on unsafe methods and never on GET", async () => {
    const client = createApiClient({ baseUrl: "", withCredentials: true });
    const mock = new MockAdapter(client.axios);

    mock.onGet("/api/v1/auth/csrf").reply(200, success({ requestToken: "csrf-abc" }));
    mock.onGet("/api/v1/auth/me").reply((config) => {
      expect(config.headers?.["X-CSRF-TOKEN"]).toBeUndefined();
      return [200, success({ id: "u1" })];
    });
    mock.onPost("/api/v1/auth/login").reply((config) => {
      expect(config.headers?.["X-CSRF-TOKEN"]).toBe("csrf-abc");
      return [200, success({ id: "u1" })];
    });

    await client.get("/api/v1/auth/me");
    await client.post("/api/v1/auth/login", {
      payload: { provider: "oidc", subject: "test-subject" },
    });
    expect(getCachedCsrfToken()).toBe("csrf-abc");

    mock.restore();
  });

  it("refreshes CSRF once and retries an unsafe request after ERR-001", async () => {
    const client = createApiClient({ baseUrl: "", withCredentials: true });
    const mock = new MockAdapter(client.axios);
    let csrfCalls = 0;
    let loginCalls = 0;

    mock.onGet("/api/v1/auth/csrf").reply(() => {
      csrfCalls += 1;
      return [200, success({ requestToken: `token-${csrfCalls}` })];
    });

    mock.onPost("/api/items").reply((config) => {
      loginCalls += 1;
      const token = config.headers?.["X-CSRF-TOKEN"];
      if (loginCalls === 1) {
        expect(token).toBe("token-1");
        return [400, failure("ERR-001")];
      }
      expect(token).toBe("token-2");
      return [200, success({ ok: true })];
    });

    await expect(client.post("/api/items", { payload: { name: "x" } })).resolves.toMatchObject({
      data: { ok: true },
    });
    expect(csrfCalls).toBe(2);
    expect(loginCalls).toBe(2);

    mock.restore();
  });

  it("does not send Authorization Bearer headers", async () => {
    localStorage.setItem("access_token", "should-not-be-used");
    const client = createApiClient({ baseUrl: "", withCredentials: true });
    const mock = new MockAdapter(client.axios);

    mock.onGet("/api/v1/auth/me").reply((config) => {
      const header = config.headers?.Authorization ?? config.headers?.authorization;
      expect(header).toBeUndefined();
      return [200, success({ id: "u1" })];
    });

    await client.get("/api/v1/auth/me");
    localStorage.removeItem("access_token");
    mock.restore();
  });
});
