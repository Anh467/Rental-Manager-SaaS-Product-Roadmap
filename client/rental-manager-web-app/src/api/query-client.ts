import { QueryClient } from "@tanstack/react-query";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      gcTime: 5 * 60_000,
      retry: (failureCount, error) => {
        const status = (error as { status?: number })?.status;
        if (status && status >= 400 && status < 500) return false;
        return failureCount < 2;
      },
      refetchOnWindowFocus: false,
    },
    mutations: { retry: false },
  },
});

export const queryKeys = {
  all: ["rental-manager"] as const,
  properties: {
    all: () => [...queryKeys.all, "properties"] as const,
    lists: () => [...queryKeys.properties.all(), "list"] as const,
    list: (params: unknown) => [...queryKeys.properties.lists(), params] as const,
    details: () => [...queryKeys.properties.all(), "detail"] as const,
    detail: (id: string) => [...queryKeys.properties.details(), id] as const,
  },
  rooms: {
    all: () => [...queryKeys.all, "rooms"] as const,
    list: (params: unknown) => [...queryKeys.rooms.all(), "list", params] as const,
    detail: (id: string) => [...queryKeys.rooms.all(), "detail", id] as const,
  },
  tenants: {
    all: () => [...queryKeys.all, "tenants"] as const,
    list: (params: unknown) => [...queryKeys.tenants.all(), "list", params] as const,
    detail: (id: string) => [...queryKeys.tenants.all(), "detail", id] as const,
  },
} as const;
