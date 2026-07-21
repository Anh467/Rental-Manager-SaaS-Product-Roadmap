import { v1Client, type ApiRequestOptions, type PageResult } from "@/api/client";
import type {
  CreateRoomRequest,
  GetRoomsRequest,
  Room,
  RoomPathParams,
  UpdateRoomRequest,
} from "./types";

export function getRooms(options: ApiRequestOptions<GetRoomsRequest>) {
  return v1Client.get<PageResult<Room>, GetRoomsRequest>("/api/rooms", options);
}

export function getRoom(path: RoomPathParams, options?: ApiRequestOptions) {
  return v1Client.get<Room>(`/api/rooms/${path.roomId}`, options);
}

export function createRoom(
  options: ApiRequestOptions<Record<string, never>, CreateRoomRequest>,
) {
  return v1Client.post<Room, CreateRoomRequest>("/api/rooms", options);
}

export function updateRoom(
  path: RoomPathParams,
  options: ApiRequestOptions<Record<string, never>, UpdateRoomRequest>,
) {
  return v1Client.put<Room, UpdateRoomRequest>(`/api/rooms/${path.roomId}`, options);
}

export function deleteRoom(path: RoomPathParams, options?: ApiRequestOptions) {
  return v1Client.delete(`/api/rooms/${path.roomId}`, options);
}
