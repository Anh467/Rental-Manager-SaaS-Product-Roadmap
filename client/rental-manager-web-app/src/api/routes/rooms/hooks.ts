import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { createRoom, deleteRoom, updateRoom } from "@/api/routes/rooms/requests";
import { roomKeys, roomQueries } from "@/api/routes/rooms/queries";
import type {
  CreateRoomRequest,
  GetRoomsRequest,
  UpdateRoomRequest,
} from "@/api/routes/rooms/types";

export function useRoomsQuery(params: GetRoomsRequest) {
  return useQuery(roomQueries.list(params));
}

export function useRoomQuery(roomId: string) {
  return useQuery(roomQueries.detail(roomId));
}

export function useCreateRoomMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateRoomRequest) => createRoom(payload),
    onSuccess: async () => {
      toast.success("Tạo phòng thành công.");
      await queryClient.invalidateQueries({ queryKey: roomKeys.lists() });
    },
  });
}

export function useUpdateRoomMutation(roomId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: UpdateRoomRequest) => updateRoom(roomId, payload),
    onSuccess: async (room) => {
      toast.success("Cập nhật phòng thành công.");
      queryClient.setQueryData(roomKeys.detail(roomId), room);
      await queryClient.invalidateQueries({ queryKey: roomKeys.lists() });
    },
  });
}

export function useDeleteRoomMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (roomId: string) => deleteRoom(roomId),
    onSuccess: async (_result, roomId) => {
      toast.success("Xóa phòng thành công.");
      queryClient.removeQueries({ queryKey: roomKeys.detail(roomId) });
      await queryClient.invalidateQueries({ queryKey: roomKeys.lists() });
    },
  });
}
