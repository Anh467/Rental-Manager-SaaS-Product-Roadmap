import { createMockProperties, createMockRooms } from "@/api/mocks/factories";

const properties = createMockProperties();
const rooms = createMockRooms(properties);

export const mockDatabase = {
  properties,
  rooms,
};

export function resetMockDatabase() {
  mockDatabase.properties.splice(0, mockDatabase.properties.length, ...createMockProperties());
  mockDatabase.rooms.splice(0, mockDatabase.rooms.length, ...createMockRooms(mockDatabase.properties));
}
