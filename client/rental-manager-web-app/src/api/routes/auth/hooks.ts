import { useMutation, useQuery } from "@tanstack/react-query";

import { authQueries } from "./queries";
import { login } from "./requests";
import type { LoginRequest } from "./types";

export function useMeQuery(enabled = true) {
  return useQuery({ ...authQueries.me(), enabled });
}

export function useLoginMutation() {
  return useMutation({
    mutationFn: (payload: LoginRequest) => login({ payload }),
  });
}
