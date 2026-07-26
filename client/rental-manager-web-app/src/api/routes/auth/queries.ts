import { queryOptions } from "@tanstack/react-query";

import { isApiError } from "@/api/client";

import { getMe } from "./requests";

export const authQueries = {
  me: () => queryOptions({
    queryKey: ["auth", "me"] as const,
    queryFn: async ({ signal }) => (await getMe({ signal })).data,
    retry: (failureCount, error) => {
      if (isApiError(error) && (error.status === 401 || error.status === 403)) {
        return false;
      }
      return failureCount < 2;
    },
  }),
};
