import { describe, expect, it } from "vitest";

import {
  createApiError,
  isApiErrorResponse,
  isApiSuccessResponse,
  parseSuccessResponse,
} from "./utils";

describe("isApiSuccessResponse", () => {
  it("accepts a well-formed success envelope", () => {
    const value = {
      success: true,
      messageKey: "SCS-001",
      data: { id: "1" },
      correlationId: "corr-1",
    };
    expect(isApiSuccessResponse(value)).toBe(true);
  });

  it("accepts an explicit null data payload", () => {
    const value = {
      success: true,
      messageKey: "SCS-003",
      data: null,
      correlationId: "corr-null",
    };
    expect(isApiSuccessResponse(value)).toBe(true);
    expect(parseSuccessResponse(value).data).toBeNull();
  });

  it.each([
    ["missing messageKey", { success: true, data: {}, correlationId: "corr-1" }],
    ["non-success messageKey", { success: true, messageKey: "ERR-001", data: {}, correlationId: "corr-1" }],
    ["unknown success key with valid prefix", { success: true, messageKey: "SCS-999", data: {}, correlationId: "corr-1" }],
    ["missing correlationId", { success: true, messageKey: "SCS-001", data: {} }],
    ["empty correlationId", { success: true, messageKey: "SCS-001", data: {}, correlationId: "" }],
    ["missing data", { success: true, messageKey: "SCS-001", correlationId: "corr-1" }],
    ["success flag false", { success: false, messageKey: "SCS-001", data: {}, correlationId: "corr-1" }],
    ["arbitrary JSON", { foo: "bar" }],
    ["null", null],
    ["array", []],
  ])("rejects %s", (_label, value) => {
    expect(isApiSuccessResponse(value)).toBe(false);
  });
});

describe("isApiErrorResponse", () => {
  it("accepts a well-formed error envelope", () => {
    const value = { success: false, messageKey: "ERR-001", correlationId: "corr-1" };
    expect(isApiErrorResponse(value)).toBe(true);
  });

  it("accepts a deprecated error key for backward-compatible reads", () => {
    const value = { success: false, messageKey: "ERR-034", correlationId: "corr-1" };
    expect(isApiErrorResponse(value)).toBe(true);
  });

  it.each([
    ["missing messageKey", { success: false, correlationId: "corr-1" }],
    ["non-error messageKey", { success: false, messageKey: "SCS-001", correlationId: "corr-1" }],
    ["unknown error key with valid prefix", { success: false, messageKey: "ERR-999", correlationId: "corr-1" }],
    ["malformed key", { success: false, messageKey: "ERROR-001", correlationId: "corr-1" }],
    ["missing correlationId", { success: false, messageKey: "ERR-001" }],
    ["empty correlationId", { success: false, messageKey: "ERR-001", correlationId: "" }],
    ["success flag true", { success: true, messageKey: "ERR-001", correlationId: "corr-1" }],
  ])("rejects %s", (_label, value) => {
    expect(isApiErrorResponse(value)).toBe(false);
  });
});

describe("parseSuccessResponse", () => {
  it("returns the envelope when it is valid", () => {
    const value = {
      success: true,
      messageKey: "SCS-005",
      data: { id: "1" },
      correlationId: "corr-1",
    };
    expect(parseSuccessResponse(value)).toBe(value);
  });

  it.each([
    ["arbitrary JSON", { id: "1", name: "not an envelope" }],
    ["a JSON error envelope", { success: false, messageKey: "ERR-001", correlationId: "corr-1" }],
    ["a plain string", "just a string"],
    ["undefined", undefined],
  ])("rejects %s with ERR-050", (_label, value) => {
    expect(() => parseSuccessResponse(value)).toThrowError();
    try {
      parseSuccessResponse(value);
      expect.fail("expected parseSuccessResponse to throw");
    } catch (error) {
      expect(isApiError(error) ? error.messageKey : undefined).toBe("ERR-050");
    }
  });
});

function isApiError(value: unknown): value is { messageKey: string } {
  return typeof value === "object" && value !== null && "messageKey" in value;
}

describe("createApiError", () => {
  it("builds an ApiError with the requested messageKey and status", () => {
    const error = createApiError(404, { messageKey: "ERR-002", correlationId: "corr-1" });
    expect(error.name).toBe("ApiError");
    expect(error.status).toBe(404);
    expect(error.messageKey).toBe("ERR-002");
    expect(error.correlationId).toBe("corr-1");
  });

  it("falls back to a status-appropriate messageKey when none is provided", () => {
    expect(createApiError(401).messageKey).toBe("ERR-003");
    expect(createApiError(403).messageKey).toBe("ERR-004");
    expect(createApiError(404).messageKey).toBe("ERR-002");
    expect(createApiError(429).messageKey).toBe("ERR-040");
    expect(createApiError(500).messageKey).toBe("ERR-050");
    expect(createApiError(0).messageKey).toBe("ERR-049");
  });
});
