import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

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

const localesRoot = resolve(
  dirname(fileURLToPath(import.meta.url)),
  "../../locales",
);

function sorted(values: readonly string[]) {
  return [...values].sort((a, b) => a.localeCompare(b));
}

/** Top-level JSON object keys in source order, before Object/JSON.parse collapse. */
function readJsonObjectKeysPreservingDuplicatesFromSource(raw: string): string[] {
  const keys: string[] = [];
  let depth = 0;
  let index = 0;

  while (index < raw.length) {
    const char = raw[index];
    if (char === "{") {
      depth += 1;
      index += 1;
      continue;
    }
    if (char === "}") {
      depth -= 1;
      index += 1;
      continue;
    }
    if (char === '"') {
      index += 1;
      let key = "";
      while (index < raw.length) {
        const current = raw[index];
        if (current === "\\") {
          key += current + (raw[index + 1] ?? "");
          index += 2;
          continue;
        }
        if (current === '"') {
          index += 1;
          break;
        }
        key += current;
        index += 1;
      }
      if (depth === 1) {
        let lookAhead = index;
        while (lookAhead < raw.length && /\s/.test(raw[lookAhead] ?? "")) {
          lookAhead += 1;
        }
        if (raw[lookAhead] === ":") {
          keys.push(key);
        }
      }
      continue;
    }
    index += 1;
  }

  return keys;
}

function readJsonObjectKeysPreservingDuplicates(path: string): string[] {
  return readJsonObjectKeysPreservingDuplicatesFromSource(readFileSync(path, "utf8"));
}

function assertNoDuplicateKeys(label: string, keys: readonly string[]) {
  const counts = new Map<string, number>();
  for (const key of keys) {
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }
  const duplicates = [...counts.entries()]
    .filter(([, count]) => count > 1)
    .map(([key, count]) => `${key} (x${count})`);
  if (duplicates.length > 0) {
    throw new Error(`${label} contains duplicates: [${duplicates.join(", ")}]`);
  }
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
    assertNoDuplicateKeys("ALL_ERROR_MESSAGE_KEYS", ALL_ERROR_MESSAGE_KEYS);
    expect(sorted(ALL_ERROR_MESSAGE_KEYS)).toEqual(sorted(expected));
    expect(new Set(ALL_ERROR_MESSAGE_KEYS).size).toBe(ALL_ERROR_MESSAGE_KEYS.length);
  });

  it("covers SCS-001 through SCS-020, all active", () => {
    const expected = Array.from({ length: 20 }, (_, index) =>
      `SCS-${String(index + 1).padStart(3, "0")}`);
    assertNoDuplicateKeys("ALL_SUCCESS_MESSAGE_KEYS", ALL_SUCCESS_MESSAGE_KEYS);
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
    it.each([
      ["en/success.json", enSuccess, ALL_SUCCESS_MESSAGE_KEYS],
      ["vi/success.json", viSuccess, ALL_SUCCESS_MESSAGE_KEYS],
      ["en/error.json", enError, ALL_ERROR_MESSAGE_KEYS],
      ["vi/error.json", viError, ALL_ERROR_MESSAGE_KEYS],
    ] as const)(
      "%s has exactly the catalog keys and no duplicates in raw JSON",
      (relativePath, imported, expectedKeys) => {
        const rawKeys = readJsonObjectKeysPreservingDuplicates(
          resolve(localesRoot, relativePath),
        );
        assertNoDuplicateKeys(relativePath, rawKeys);
        expect(sorted(rawKeys)).toEqual(sorted(expectedKeys));
        expect(sorted(Object.keys(imported))).toEqual(sorted(expectedKeys));
      },
    );

    it("detects duplicate keys in a locale fixture before object mapping", () => {
      const fixture = `{
  "ERR-001": "one",
  "ERR-002": "two",
  "ERR-001": "duplicate"
}`;
      const keys = readJsonObjectKeysPreservingDuplicatesFromSource(fixture);
      expect(keys).toEqual(["ERR-001", "ERR-002", "ERR-001"]);
      expect(() => assertNoDuplicateKeys("fixture/duplicate-locale.json", keys)).toThrow(
        /fixture\/duplicate-locale\.json contains duplicates/,
      );
    });
  });
});
