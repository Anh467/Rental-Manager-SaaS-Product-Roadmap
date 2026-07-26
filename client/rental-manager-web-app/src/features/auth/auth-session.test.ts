import { QueryClient } from "@tanstack/react-query";
import { isRedirect } from "@tanstack/react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { clearCsrfToken, ensureCsrfToken, getCachedCsrfToken } from "@/api/client/csrf";
import { createApiError } from "@/api/client/utils";
import type { AuthUser } from "@/api/routes/auth";
import { authQueries } from "@/api/routes/auth/queries";
import {
  clearPendingOrganizationSelection,
  getPendingOrganizationSelection,
  setPendingOrganizationSelection,
} from "@/features/auth/organization-selection";
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

  it("rethrows HTTP 500 without clearing auth query cache", () => {
    const queryClient = new QueryClient();
    queryClient.setQueryData(AUTH_ME_QUERY_KEY, mockUser);
    const error = createApiError(500);
    expect(() => resolveAuthenticatedMeFailure(error, queryClient)).toThrow(error);
    expect(queryClient.getQueryData(AUTH_ME_QUERY_KEY)).toEqual(mockUser);
  });

  it("rethrows network errors without clearing auth query cache", () => {
    const queryClient = new QueryClient();
    queryClient.setQueryData(AUTH_ME_QUERY_KEY, mockUser);
    const error = createApiError(0);
    expect(() => resolveAuthenticatedMeFailure(error, queryClient)).toThrow(error);
    expect(queryClient.getQueryData(AUTH_ME_QUERY_KEY)).toEqual(mockUser);
  });
});

describe("ensureAuthenticatedUser", () => {
  beforeEach(() => {
    localStorage.clear();
    clearCsrfToken();
    clearPendingOrganizationSelection();
    vi.restoreAllMocks();
  });

  afterEach(() => {
    localStorage.clear();
    clearCsrfToken();
    clearPendingOrganizationSelection();
  });

  it("returns the authenticated user from /me", async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const fetchSpy = vi.fn().mockResolvedValue(mockUser);
    queryClient.ensureQueryData = fetchSpy;

    await expect(ensureAuthenticatedUser(queryClient)).resolves.toEqual(mockUser);
    expect(fetchSpy).toHaveBeenCalledOnce();
    expect(fetchSpy.mock.calls[0]?.[0]?.queryKey).toEqual(AUTH_ME_QUERY_KEY);
  });

  it("redirects to /login on 401 and clears auth cache", async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    queryClient.setQueryData(AUTH_ME_QUERY_KEY, mockUser);
    queryClient.ensureQueryData = vi.fn().mockRejectedValue(createApiError(401));

    await expect(ensureAuthenticatedUser(queryClient)).rejects.toSatisfy((error: unknown) => {
      expectRedirectTo(error, "/login");
      return true;
    });
    expect(queryClient.getQueryData(AUTH_ME_QUERY_KEY)).toBeUndefined();
  });

  it("redirects to /access-denied on 403", async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    queryClient.ensureQueryData = vi.fn().mockRejectedValue(createApiError(403));

    await expect(ensureAuthenticatedUser(queryClient)).rejects.toSatisfy((error: unknown) => {
      expectRedirectTo(error, "/access-denied");
      return true;
    });
  });

  it("rethrows on /me HTTP 500", async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const serverError = createApiError(500);
    queryClient.ensureQueryData = vi.fn().mockRejectedValue(serverError);

    await expect(ensureAuthenticatedUser(queryClient)).rejects.toBe(serverError);
  });

  it("rethrows on network errors", async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const networkError = createApiError(0);
    queryClient.ensureQueryData = vi.fn().mockRejectedValue(networkError);

    await expect(ensureAuthenticatedUser(queryClient)).rejects.toBe(networkError);
  });

  it("shares one cache entry / request across multiple loaders", async () => {
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
    clearCsrfToken();
  });

  it("does not redirect when /me is anonymous", async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    queryClient.ensureQueryData = vi.fn().mockRejectedValue(createApiError(401));

    await expect(redirectIfAuthenticated(queryClient)).resolves.toBeUndefined();
  });

  it("does not redirect to / when /me returns 500", async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    queryClient.ensureQueryData = vi.fn().mockRejectedValue(createApiError(500));

    await expect(redirectIfAuthenticated(queryClient)).resolves.toBeUndefined();
  });

  it("redirects to / when /me succeeds", async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    queryClient.ensureQueryData = vi.fn().mockResolvedValue(mockUser);

    await expect(redirectIfAuthenticated(queryClient)).rejects.toSatisfy((error: unknown) => {
      expectRedirectTo(error, "/");
      return true;
    });
  });

  it("does not redirect based on localStorage access_token", async () => {
    localStorage.setItem("access_token", "stale-token");
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    queryClient.ensureQueryData = vi.fn().mockRejectedValue(createApiError(401));

    await expect(redirectIfAuthenticated(queryClient)).resolves.toBeUndefined();
    expect(localStorage.getItem("access_token")).toBe("stale-token");
  });
});

describe("clearAuthSession", () => {
  it("clears CSRF memory, pending org selection, and auth query cache", async () => {
    await ensureCsrfToken(async () => "cached-csrf");
    setPendingOrganizationSelection({
      selectionTicket: "ticket",
      organizations: [{ id: "org-1", name: "Org 1" }],
    });
    const queryClient = new QueryClient();
    queryClient.setQueryData(AUTH_ME_QUERY_KEY, mockUser);

    clearAuthSession(queryClient);

    expect(queryClient.getQueryData(AUTH_ME_QUERY_KEY)).toBeUndefined();
    expect(getCachedCsrfToken()).toBeNull();
    expect(getPendingOrganizationSelection()).toBeNull();
  });
});
