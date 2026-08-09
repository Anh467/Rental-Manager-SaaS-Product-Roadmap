import { afterEach, describe, expect, it, vi } from "vitest";

import {
  clearCsrfToken,
  ensureCsrfToken,
  getCachedCsrfToken,
  isUnsafeHttpMethod,
  refreshCsrfToken,
} from "./csrf";

describe("csrf token cache", () => {
  afterEach(() => {
    clearCsrfToken();
  });

  it("caches a single request token in memory", async () => {
    const fetcher = vi.fn().mockResolvedValue("token-1");

    await expect(ensureCsrfToken(fetcher)).resolves.toBe("token-1");
    await expect(ensureCsrfToken(fetcher)).resolves.toBe("token-1");
    expect(fetcher).toHaveBeenCalledOnce();
    expect(getCachedCsrfToken()).toBe("token-1");
  });

  it("deduplicates concurrent ensure calls", async () => {
    let resolveFetch: ((value: string) => void) | undefined;
    const fetcher = vi.fn(
      () =>
        new Promise<string>((resolve) => {
          resolveFetch = resolve;
        }),
    );

    const first = ensureCsrfToken(fetcher);
    const second = ensureCsrfToken(fetcher);
    resolveFetch?.("shared-token");

    await expect(Promise.all([first, second])).resolves.toEqual(["shared-token", "shared-token"]);
    expect(fetcher).toHaveBeenCalledOnce();
  });

  it("refresh replaces the cached token", async () => {
    await ensureCsrfToken(async () => "old-token");
    await expect(refreshCsrfToken(async () => "new-token")).resolves.toBe("new-token");
    expect(getCachedCsrfToken()).toBe("new-token");
  });

  it("clear removes the cached token", async () => {
    await ensureCsrfToken(async () => "token");
    clearCsrfToken();
    expect(getCachedCsrfToken()).toBeNull();
  });
});

describe("isUnsafeHttpMethod", () => {
  it("marks mutating methods as unsafe", () => {
    expect(isUnsafeHttpMethod("POST")).toBe(true);
    expect(isUnsafeHttpMethod("put")).toBe(true);
    expect(isUnsafeHttpMethod("PATCH")).toBe(true);
    expect(isUnsafeHttpMethod("DELETE")).toBe(true);
  });

  it("does not mark GET as unsafe", () => {
    expect(isUnsafeHttpMethod("GET")).toBe(false);
    expect(isUnsafeHttpMethod("get")).toBe(false);
    expect(isUnsafeHttpMethod(undefined)).toBe(false);
  });
});
