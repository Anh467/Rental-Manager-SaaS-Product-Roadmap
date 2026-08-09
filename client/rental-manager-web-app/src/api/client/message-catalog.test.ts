import { describe, expect, it } from "vitest";

import enError from "@/locales/en/error.json";
import enSuccess from "@/locales/en/success.json";
import viError from "@/locales/vi/error.json";
import viSuccess from "@/locales/vi/success.json";

import {
  ACTIVE_ERROR_MESSAGE_KEYS,
  ACTIVE_SUCCESS_MESSAGE_KEYS,
  ALL_ERROR_MESSAGE_KEYS,
  ALL_SUCCESS_MESSAGE_KEYS,
  DEPRECATED_ERROR_MESSAGE_KEYS,
  isActiveErrorMessageKey,
  isActiveSuccessMessageKey,
  isDeprecatedErrorMessageKey,
} from "./message-catalog";

function sorted(values: readonly string[]) {
  return [...values].sort((a, b) => a.localeCompare(b));
}

describe("message catalog", () => {
  it("has no overlap between active and deprecated error keys", () => {
    const active = new Set<string>(ACTIVE_ERROR_MESSAGE_KEYS);
    for (const key of DEPRECATED_ERROR_MESSAGE_KEYS) {
      expect(active.has(key)).toBe(false);
    }
  });

  it("covers ERR-001 through ERR-050 with no gaps and no duplicates", () => {
    const expected = Array.from({ length: 50 }, (_, index) =>
      `ERR-${String(index + 1).padStart(3, "0")}`);
    expect(sorted(ALL_ERROR_MESSAGE_KEYS)).toEqual(sorted(expected));
    expect(new Set(ALL_ERROR_MESSAGE_KEYS).size).toBe(ALL_ERROR_MESSAGE_KEYS.length);
  });

  it("covers SCS-001 through SCS-020, all active", () => {
    const expected = Array.from({ length: 20 }, (_, index) =>
      `SCS-${String(index + 1).padStart(3, "0")}`);
    expect(sorted(ALL_SUCCESS_MESSAGE_KEYS)).toEqual(sorted(expected));
    expect(sorted(ACTIVE_SUCCESS_MESSAGE_KEYS)).toEqual(sorted(expected));
  });

  it("has exactly 37 active and 13 deprecated error keys", () => {
    expect(ACTIVE_ERROR_MESSAGE_KEYS).toHaveLength(37);
    expect(DEPRECATED_ERROR_MESSAGE_KEYS).toHaveLength(13);
  });

  it.each(["ERR-027", "ERR-034", "ERR-035", "ERR-036", "ERR-037", "ERR-038", "ERR-039",
    "ERR-042", "ERR-043", "ERR-044", "ERR-045", "ERR-046", "ERR-047"])(
    "marks %s as deprecated and not active",
    (key) => {
      expect(isDeprecatedErrorMessageKey(key)).toBe(true);
      expect(isActiveErrorMessageKey(key)).toBe(false);
    },
  );

  it.each(["ERR-040", "ERR-041", "ERR-048", "ERR-049", "ERR-050"])(
    "marks %s as active and not deprecated",
    (key) => {
      expect(isActiveErrorMessageKey(key)).toBe(true);
      expect(isDeprecatedErrorMessageKey(key)).toBe(false);
    },
  );

  it("recognizes active success keys", () => {
    expect(isActiveSuccessMessageKey("SCS-001")).toBe(true);
    expect(isActiveSuccessMessageKey("SCS-999")).toBe(false);
  });

  describe("locale parity", () => {
    it("en/success.json has exactly the catalog's success keys", () => {
      expect(sorted(Object.keys(enSuccess))).toEqual(sorted(ALL_SUCCESS_MESSAGE_KEYS));
    });

    it("vi/success.json has exactly the catalog's success keys", () => {
      expect(sorted(Object.keys(viSuccess))).toEqual(sorted(ALL_SUCCESS_MESSAGE_KEYS));
    });

    it("en/error.json has exactly the catalog's error keys", () => {
      expect(sorted(Object.keys(enError))).toEqual(sorted(ALL_ERROR_MESSAGE_KEYS));
    });

    it("vi/error.json has exactly the catalog's error keys", () => {
      expect(sorted(Object.keys(viError))).toEqual(sorted(ALL_ERROR_MESSAGE_KEYS));
    });
  });
});
