import { createQueryKeys, type inferQueryKeys } from "@lukemorales/query-key-factory";
import { keepPreviousData, queryOptions } from "@tanstack/react-query";

import { getRoom, getRooms } from "./requests";
import type { GetRoomsRequest } from "./types";

export const roomsQueryStore = createQueryKeys("rooms", {
  list: (query: GetRoomsRequest) => ({
    queryKey: [{ query }],
    queryFn: async ({ signal }) => (await getRooms({ query, signal })).data,
  }),
  detail: (roomId: string) => ({
    queryKey: [roomId],
    queryFn: async ({ signal }) => (await getRoom({ roomId }, { signal })).data,
  }),
});

export type RoomsQueryKeys = inferQueryKeys<typeof roomsQueryStore>;

export const roomQueries = {
  list: (params: GetRoomsRequest) =>
    queryOptions({
      ...roomsQueryStore.list(params),
      placeholderData: keepPreviousData,
    }),
  detail: (roomId: string) =>
    queryOptions({
      ...roomsQueryStore.detail(roomId),
      enabled: Boolean(roomId),
    }),
};
