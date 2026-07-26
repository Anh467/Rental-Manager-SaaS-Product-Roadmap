import { QueryClient } from "@tanstack/react-query";
import { isRedirect } from "@tanstack/react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { createApiError } from "@/api/client/utils";
import type { AuthUser } from "@/api/routes/auth";
import { authQueries } from "@/api/routes/auth/queries";
import {
  AUTH_ME_QUERY_KEY,
  clearAuthSession,
  ensureAuthenticatedUser,
  redirectIfAuthenticated,
  resolveAuthenticatedMeFailure,
} from "./auth-session";

const mockUser: AuthUser = {
  id: "user-1",
  name: "Mock User",
  email: "mock@example.com",
  scope: "organization",
  organizationId: "org-1",
  permissions: ["property.create", "room.create"],
};

function getRedirectTo(error: unknown): string | undefined {
  if (!error || typeof error !== "object") return undefined;
  const candidate = error as { to?: string; options?: { to?: string } };
  return candidate.options?.to ?? candidate.to;
}

function expectRedirectTo(error: unknown, to: string) {
  expect(isRedirect(error) || Boolean(getRedirectTo(error))).toBe(true);
  expect(getRedirectTo(error)).toBe(to);
}

describe("resolveAuthenticatedMeFailure", () => {
  it("redirects to /login on HTTP 401", () => {
    try {
      resolveAuthenticatedMeFailure(createApiError(401));
      expect.fail("expected redirect");
    } catch (error) {
      expectRedirectTo(error, "/login");
    }
  });

  it("redirects to /access-denied on HTTP 403", () => {
    try {
      resolveAuthenticatedMeFailure(createApiError(403));
      expect.fail("expected redirect");
    } catch (error) {
      expectRedirectTo(error, "/access-denied");
    }
  });

  it("rethrows HTTP 500 without clearing the session", () => {
    localStorage.setItem("access_token", "keep-me");
    const error = createApiError(500);
    expect(() => resolveAuthenticatedMeFailure(error)).toThrow(error);
    expect(localStorage.getItem("access_token")).toBe("keep-me");
  });

  it("rethrows network errors without clearing the session", () => {
    localStorage.setItem("access_token", "keep-me");
    const error = createApiError(0);
    expect(() => resolveAuthenticatedMeFailure(error)).toThrow(error);
    expect(localStorage.getItem("access_token")).toBe("keep-me");
  });
});

describe("ensureAuthenticatedUser", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it("requires a token before calling /me", async () => {
    const queryClient = new QueryClient();
    await expect(ensureAuthenticatedUser(queryClient)).rejects.toSatisfy(isRedirect);
  });

  it("returns the authenticated user for a valid token", async () => {
    localStorage.setItem("access_token", "valid-token");
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const fetchSpy = vi.fn().mockResolvedValue(mockUser);
    queryClient.ensureQueryData = fetchSpy;

    await expect(ensureAuthenticatedUser(queryClient)).resolves.toEqual(mockUser);
    expect(fetchSpy).toHaveBeenCalledOnce();
    expect(fetchSpy.mock.calls[0]?.[0]?.queryKey).toEqual(AUTH_ME_QUERY_KEY);
  });

  it("redirects to /login on 401 and clears the token", async () => {
    localStorage.setItem("access_token", "expired");
    localStorage.setItem("organization_id", "org-1");
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    queryClient.ensureQueryData = vi.fn().mockRejectedValue(createApiError(401));

    await expect(ensureAuthenticatedUser(queryClient)).rejects.toSatisfy((error: unknown) => {
      expectRedirectTo(error, "/login");
      return true;
    });
    expect(localStorage.getItem("access_token")).toBeNull();
    expect(localStorage.getItem("organization_id")).toBeNull();
  });

  it("redirects to /access-denied on 403", async () => {
    localStorage.setItem("access_token", "forbidden");
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    queryClient.ensureQueryData = vi.fn().mockRejectedValue(createApiError(403));

    await expect(ensureAuthenticatedUser(queryClient)).rejects.toSatisfy((error: unknown) => {
      expectRedirectTo(error, "/access-denied");
      return true;
    });
    expect(localStorage.getItem("access_token")).toBe("forbidden");
  });

  it("keeps the token and rethrows on /me HTTP 500", async () => {
    localStorage.setItem("access_token", "valid-token");
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const serverError = createApiError(500);
    queryClient.ensureQueryData = vi.fn().mockRejectedValue(serverError);

    await expect(ensureAuthenticatedUser(queryClient)).rejects.toBe(serverError);
    expect(localStorage.getItem("access_token")).toBe("valid-token");
  });

  it("keeps the token and rethrows on network errors", async () => {
    localStorage.setItem("access_token", "valid-token");
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const networkError = createApiError(0);
    queryClient.ensureQueryData = vi.fn().mockRejectedValue(networkError);

    await expect(ensureAuthenticatedUser(queryClient)).rejects.toBe(networkError);
    expect(localStorage.getItem("access_token")).toBe("valid-token");
  });

  it("shares one cache entry / request across multiple loaders", async () => {
    localStorage.setItem("access_token", "shared-token");
    let calls = 0;
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false, staleTime: 30_000 } },
    });

    const queryFn = vi.fn(async () => {
      calls += 1;
      return mockUser;
    });

    const options = {
      ...authQueries.me(),
      queryFn,
    };

    const [first, second] = await Promise.all([
      queryClient.ensureQueryData(options),
      queryClient.ensureQueryData(options),
    ]);

    expect(first).toEqual(mockUser);
    expect(second).toEqual(mockUser);
    expect(calls).toBe(1);
    expect(queryClient.getQueryData(AUTH_ME_QUERY_KEY)).toEqual(mockUser);
  });
});

describe("redirectIfAuthenticated", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("does not redirect when there is no token", async () => {
    const queryClient = new QueryClient();
    await expect(redirectIfAuthenticated(queryClient)).resolves.toBeUndefined();
  });

  it("does not redirect to / when the stored token fails /me (no loop)", async () => {
    localStorage.setItem("access_token", "stale-token");
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    queryClient.ensureQueryData = vi.fn().mockRejectedValue(createApiError(401));

    await expect(redirectIfAuthenticated(queryClient)).resolves.toBeUndefined();
    expect(localStorage.getItem("access_token")).toBeNull();
  });

  it("does not redirect to / when /me returns 500", async () => {
    localStorage.setItem("access_token", "valid-looking");
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    queryClient.ensureQueryData = vi.fn().mockRejectedValue(createApiError(500));

    await expect(redirectIfAuthenticated(queryClient)).resolves.toBeUndefined();
    expect(localStorage.getItem("access_token")).toBe("valid-looking");
  });

  it("redirects to / when /me succeeds", async () => {
    localStorage.setItem("access_token", "valid-token");
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    queryClient.ensureQueryData = vi.fn().mockResolvedValue(mockUser);

    await expect(redirectIfAuthenticated(queryClient)).rejects.toSatisfy((error: unknown) => {
      expectRedirectTo(error, "/");
      return true;
    });
  });
});

describe("clearAuthSession", () => {
  it("clears token, organization_id, and auth query cache", () => {
    localStorage.setItem("access_token", "token");
    localStorage.setItem("organization_id", "org-1");
    const queryClient = new QueryClient();
    queryClient.setQueryData(AUTH_ME_QUERY_KEY, mockUser);

    clearAuthSession(queryClient);

    expect(localStorage.getItem("access_token")).toBeNull();
    expect(localStorage.getItem("organization_id")).toBeNull();
    expect(queryClient.getQueryData(AUTH_ME_QUERY_KEY)).toBeUndefined();
  });
});
