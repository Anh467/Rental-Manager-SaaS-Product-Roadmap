import { useQuery } from "@tanstack/react-query";

import { globalFieldTypeQueries } from "./queries";

export function useGlobalFieldTypesQuery() {
  return useQuery(globalFieldTypeQueries.list());
}
