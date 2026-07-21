import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { createRoom, deleteRoom, updateRoom } from "./requests";
import { roomQueries, roomsQueryStore } from "./queries";
import type { CreateRoomRequest, GetRoomsRequest, UpdateRoomRequest } from "./types";

export function useRoomsQuery(params: GetRoomsRequest) {
  return useQuery(roomQueries.list(params));
}

export function useRoomQuery(roomId: string) {
  return useQuery(roomQueries.detail(roomId));
}

export function useCreateRoomMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateRoomRequest) => createRoom({ payload }),
    onSuccess: async (response) => {
      toast.success("Tạo phòng thành công.");
      await queryClient.invalidateQueries({ queryKey: roomsQueryStore.list._def });
      return response.data;
    },
  });
}

export function useUpdateRoomMutation(roomId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: UpdateRoomRequest) => updateRoom({ roomId }, { payload }),
    onSuccess: async (response) => {
      toast.success("Cập nhật phòng thành công.");
      queryClient.setQueryData(roomsQueryStore.detail(roomId).queryKey, response.data);
      await queryClient.invalidateQueries({ queryKey: roomsQueryStore.list._def });
      return response.data;
    },
  });
}

export function useDeleteRoomMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (roomId: string) => deleteRoom({ roomId }),
    onSuccess: async (_response, roomId) => {
      toast.success("Đã ngừng kích hoạt phòng thành công.");
      queryClient.removeQueries({ queryKey: roomsQueryStore.detail(roomId).queryKey });
      await queryClient.invalidateQueries({ queryKey: roomsQueryStore.list._def });
    },
  });
}
