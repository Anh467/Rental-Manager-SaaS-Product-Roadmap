import { keepPreviousData, queryOptions } from "@tanstack/react-query";

import { createEntityQueryKeys } from "@/api/query-store";
import { getRoom, getRooms } from "@/api/routes/rooms/requests";
import type { GetRoomsRequest } from "@/api/routes/rooms/types";

export const roomKeys = createEntityQueryKeys("rooms");

export const roomQueries = {
  list: (params: GetRoomsRequest) =>
    queryOptions({
      queryKey: roomKeys.list(params),
      queryFn: ({ signal }) => getRooms(params, signal),
      placeholderData: keepPreviousData,
    }),
  detail: (roomId: string) =>
    queryOptions({
      queryKey: roomKeys.detail(roomId),
      queryFn: ({ signal }) => getRoom(roomId, signal),
      enabled: Boolean(roomId),
    }),
};
