import MockAdapter from "axios-mock-adapter";
import { afterEach, describe, expect, it } from "vitest";

import { clearCsrfToken } from "./csrf";
import { createApiClient } from "./factory";

function mockCsrfEndpoint(mock: MockAdapter) {
  mock.onGet("/api/v1/auth/csrf").reply(200, {
    success: true,
    messageKey: "SCS-005",
    data: { requestToken: "csrf-token" },
    correlationId: "corr-csrf",
  });
}

describe("createApiClient envelope handling", () => {
  let mock: MockAdapter | undefined;

  afterEach(() => {
    mock?.restore();
    mock = undefined;
    clearCsrfToken();
  });

  it("returns the parsed success envelope for a normal JSON response", async () => {
    const client = createApiClient({ baseUrl: "", withCredentials: true });
    mock = new MockAdapter(client.axios);
    mock.onGet("/api/v1/things/1").reply(200, {
      success: true,
      messageKey: "SCS-005",
      data: { id: "1" },
      correlationId: "corr-abc",
    });

    const result = await client.get<{ id: string }>("/api/v1/things/1");

    expect(result).toEqual({
      success: true,
      messageKey: "SCS-005",
      data: { id: "1" },
      correlationId: "corr-abc",
    });
  });

  it("rejects a non-envelope JSON success body", async () => {
    const client = createApiClient({ baseUrl: "", withCredentials: true });
    mock = new MockAdapter(client.axios);
    mock.onGet("/api/v1/things/1").reply(200, { id: "1", name: "not an envelope" });

    await expect(client.get("/api/v1/things/1")).rejects.toMatchObject({
      messageKey: "ERR-050",
    });
  });

  it("synthesizes a success envelope for 204 No Content, using the correlation id header", async () => {
    const client = createApiClient({ baseUrl: "", withCredentials: true });
    mock = new MockAdapter(client.axios);
    mockCsrfEndpoint(mock);
    mock.onDelete("/api/v1/things/1").reply(204, undefined, {
      "x-correlation-id": "corr-204",
    });

    const result = await client.delete("/api/v1/things/1");

    expect(result).toEqual({
      success: true,
      messageKey: "SCS-005",
      data: undefined,
      correlationId: "corr-204",
    });
  });

  it("falls back to 'unknown' correlation id when the header is missing on 204", async () => {
    const client = createApiClient({ baseUrl: "", withCredentials: true });
    mock = new MockAdapter(client.axios);
    mockCsrfEndpoint(mock);
    mock.onDelete("/api/v1/things/1").reply(204);

    const result = await client.delete("/api/v1/things/1");

    expect(result.correlationId).toBe("unknown");
  });

  it("returns raw binary data without envelope parsing for blob responseType", async () => {
    const client = createApiClient({ baseUrl: "", withCredentials: true });
    mock = new MockAdapter(client.axios);
    const blob = new Blob(["file-bytes"], { type: "application/pdf" });
    mock.onGet("/api/v1/files/1").reply(200, blob, {
      "x-correlation-id": "corr-blob",
    });

    const result = await client.get("/api/v1/files/1", { responseType: "blob" });

    expect(result.success).toBe(true);
    expect(result.correlationId).toBe("corr-blob");
    expect(result.data).toBeInstanceOf(Blob);
  });

  it("parses a JSON error envelope even when the request used responseType blob", async () => {
    const client = createApiClient({ baseUrl: "", withCredentials: true });
    mock = new MockAdapter(client.axios);
    const errorJson = new Blob(
      [JSON.stringify({ success: false, messageKey: "ERR-002", correlationId: "corr-err" })],
      { type: "application/json" },
    );
    mock.onGet("/api/v1/files/missing").reply(404, errorJson);

    await expect(
      client.get("/api/v1/files/missing", { responseType: "blob" }),
    ).rejects.toMatchObject({
      status: 404,
      messageKey: "ERR-002",
      correlationId: "corr-err",
    });
  });
});
