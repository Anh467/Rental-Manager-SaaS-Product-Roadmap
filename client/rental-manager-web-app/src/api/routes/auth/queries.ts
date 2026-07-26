import { queryOptions } from "@tanstack/react-query";

import { getMe } from "./requests";

export const authQueries = {
  me: () => queryOptions({
    queryKey: ["auth", "me"],
    queryFn: async ({ signal }) => (await getMe({ signal })).data,
  }),
};
