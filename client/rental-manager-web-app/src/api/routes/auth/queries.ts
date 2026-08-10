import { queryOptions } from "@tanstack/react-query";

import { isApiError, requireResponseData } from "@/api/client";

import { getMe } from "./requests";

export const authQueries = {
  me: () => queryOptions({
    queryKey: ["auth", "me"] as const,
    queryFn: async ({ signal }) => requireResponseData(await getMe({ signal })),
    retry: (failureCount, error) => {
      if (isApiError(error) && (error.status === 401 || error.status === 403)) {
        return false;
      }
      return failureCount < 2;
    },
  }),
};
