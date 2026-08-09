import { useMutation, useQuery } from "@tanstack/react-query";

import { authQueries } from "./queries";
import { login, logout, selectOrganization } from "./requests";
import type { LoginRequest, SelectOrganizationRequest } from "./types";

export function useMeQuery(enabled = true) {
  return useQuery({ ...authQueries.me(), enabled });
}

export function useLoginMutation() {
  return useMutation({
    mutationFn: (payload: LoginRequest = {}) => login({ payload }),
  });
}

export function useSelectOrganizationMutation() {
  return useMutation({
    mutationFn: (payload: SelectOrganizationRequest) => selectOrganization({ payload }),
  });
}

export function useLogoutMutation() {
  return useMutation({
    mutationFn: () => logout(),
  });
}
