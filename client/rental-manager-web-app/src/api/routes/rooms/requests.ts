import { apiRequest } from "@/api/client";
import type { PageResult } from "@/api/types";
import type {
  CreateRoomRequest,
  GetRoomsRequest,
  Room,
  UpdateRoomRequest,
} from "@/api/routes/rooms/types";

export function getRooms(params: GetRoomsRequest, signal?: AbortSignal) {
  return apiRequest<PageResult<Room>>({
    url: "/api/rooms",
    method: "GET",
    params,
    signal,
  });
}

export function getRoom(roomId: string, signal?: AbortSignal) {
  return apiRequest<Room>({
    url: `/api/rooms/${roomId}`,
    method: "GET",
    signal,
  });
}

export function createRoom(payload: CreateRoomRequest) {
  return apiRequest<Room>({ url: "/api/rooms", method: "POST", data: payload });
}

export function updateRoom(roomId: string, payload: UpdateRoomRequest) {
  return apiRequest<Room>({ url: `/api/rooms/${roomId}`, method: "PUT", data: payload });
}

export function deleteRoom(roomId: string) {
  return apiRequest<void>({ url: `/api/rooms/${roomId}`, method: "DELETE" });
}
