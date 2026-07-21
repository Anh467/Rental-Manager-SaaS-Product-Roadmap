import type { ApiPathParams, PageRequest } from "@/api/client";

export type RoomStatus = "vacant" | "occupied" | "maintenance" | "inactive";

export type Room = {
  id: string;
  propertyId: string;
  propertyName?: string;
  code: string;
  floor: number;
  capacity: number;
  monthlyRent: number;
  status: RoomStatus;
  description?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
};

export type GetRoomsRequest = PageRequest & {
  propertyId?: string;
  status?: RoomStatus;
};

export type RoomPathParams = ApiPathParams<"roomId">;

export type CreateRoomRequest = Pick<
  Room,
  "propertyId" | "code" | "floor" | "capacity" | "monthlyRent" | "status" | "description" | "isActive"
>;

export type UpdateRoomRequest = CreateRoomRequest;
