import type { AxiosRequestConfig } from "axios";
import type MockAdapter from "axios-mock-adapter";

import type {
  CreatePropertyRequest,
  GetPropertiesRequest,
  Property,
  UpdatePropertyRequest,
} from "@/api/routes/properties";
import type {
  CreateRoomRequest,
  GetRoomsRequest,
  Room,
  UpdateRoomRequest,
} from "@/api/routes/rooms";
import type { ApiProblem, PageResult } from "@/api/types";
import { mockDatabase } from "@/api/mocks/database";

const propertyDetailPattern = /\/api\/properties\/([^/]+)$/;
const roomDetailPattern = /\/api\/rooms\/([^/]+)$/;

function readBody<T>(config: AxiosRequestConfig): T {
  if (typeof config.data === "string") {
    return JSON.parse(config.data) as T;
  }

  return config.data as T;
}

function getPathId(config: AxiosRequestConfig, pattern: RegExp) {
  const match = config.url?.match(pattern);
  return match?.[1] ?? "";
}

function paginate<T>(items: T[], page: number, pageSize: number): PageResult<T> {
  const safePage = Math.max(page, 1);
  const safePageSize = Math.max(pageSize, 1);
  const start = (safePage - 1) * safePageSize;

  return {
    items: items.slice(start, start + safePageSize),
    page: safePage,
    pageSize: safePageSize,
    totalItems: items.length,
    totalPages: Math.ceil(items.length / safePageSize),
  };
}

function problem(status: number, code: string, message: string, errors?: ApiProblem["errors"]): ApiProblem {
  return {
    status,
    code,
    title: message,
    message,
    detail: message,
    traceId: `mock-${Date.now()}`,
    errors,
  };
}

function normalizeSearch(value: unknown) {
  return String(value ?? "").trim().toLocaleLowerCase("vi");
}

export function registerMockHandlers(mock: MockAdapter) {
  registerPropertyHandlers(mock);
  registerRoomHandlers(mock);

  // Unknown endpoints can still reach the real backend while mock mode is enabled.
  mock.onAny().passThrough();
}

function registerPropertyHandlers(mock: MockAdapter) {
  mock.onGet("/api/properties").reply((config) => {
    const params = (config.params ?? {}) as Partial<GetPropertiesRequest>;
    const search = normalizeSearch(params.search);
    const page = Number(params.page ?? 1);
    const pageSize = Number(params.pageSize ?? 20);

    const result = mockDatabase.properties.filter((property) => {
      const matchesSearch =
        !search ||
        [property.name, property.code, property.address, property.propertyTypeName]
          .filter(Boolean)
          .some((value) => normalizeSearch(value).includes(search));
      const matchesType = !params.propertyTypeId || property.propertyTypeId === params.propertyTypeId;
      const matchesActive = params.isActive == null || property.isActive === params.isActive;

      return matchesSearch && matchesType && matchesActive;
    });

    return [200, paginate(result, page, pageSize)];
  });

  mock.onGet(propertyDetailPattern).reply((config) => {
    const propertyId = getPathId(config, propertyDetailPattern);
    const property = mockDatabase.properties.find((item) => item.id === propertyId);

    return property
      ? [200, property]
      : [404, problem(404, "ERR-PROPERTY-404", "Không tìm thấy khu nhà.")];
  });

  mock.onPost("/api/properties").reply((config) => {
    const payload = readBody<CreatePropertyRequest>(config);
    const duplicated = mockDatabase.properties.some(
      (item) => item.code.toLocaleLowerCase() === payload.code.toLocaleLowerCase(),
    );

    if (duplicated) {
      return [
        400,
        problem(400, "ERR-PROPERTY-001", "Mã khu nhà đã tồn tại.", {
          Code: ["Mã khu nhà đã tồn tại."],
        }),
      ];
    }

    const now = new Date().toISOString();
    const property: Property = {
      id: crypto.randomUUID(),
      ...payload,
      propertyTypeName: getPropertyTypeName(payload.propertyTypeId),
      createdAt: now,
      updatedAt: now,
    };

    mockDatabase.properties.unshift(property);
    return [201, property];
  });

  mock.onPut(propertyDetailPattern).reply((config) => {
    const propertyId = getPathId(config, propertyDetailPattern);
    const index = mockDatabase.properties.findIndex((item) => item.id === propertyId);

    if (index < 0) {
      return [404, problem(404, "ERR-PROPERTY-404", "Không tìm thấy khu nhà.")];
    }

    const payload = readBody<UpdatePropertyRequest>(config);
    const duplicated = mockDatabase.properties.some(
      (item) => item.id !== propertyId && item.code.toLocaleLowerCase() === payload.code.toLocaleLowerCase(),
    );

    if (duplicated) {
      return [
        400,
        problem(400, "ERR-PROPERTY-001", "Mã khu nhà đã tồn tại.", {
          Code: ["Mã khu nhà đã tồn tại."],
        }),
      ];
    }

    const updated: Property = {
      ...mockDatabase.properties[index],
      ...payload,
      propertyTypeName: getPropertyTypeName(payload.propertyTypeId),
      updatedAt: new Date().toISOString(),
    };

    mockDatabase.properties[index] = updated;
    mockDatabase.rooms.forEach((room) => {
      if (room.propertyId === propertyId) room.propertyName = updated.name;
    });

    return [200, updated];
  });

  mock.onDelete(propertyDetailPattern).reply((config) => {
    const propertyId = getPathId(config, propertyDetailPattern);
    const index = mockDatabase.properties.findIndex((item) => item.id === propertyId);

    if (index < 0) {
      return [404, problem(404, "ERR-PROPERTY-404", "Không tìm thấy khu nhà.")];
    }

    if (mockDatabase.rooms.some((room) => room.propertyId === propertyId)) {
      return [
        409,
        problem(409, "ERR-PROPERTY-ROOMS", "Không thể xóa khu nhà đang có phòng."),
      ];
    }

    mockDatabase.properties.splice(index, 1);
    return [204];
  });
}

function registerRoomHandlers(mock: MockAdapter) {
  mock.onGet("/api/rooms").reply((config) => {
    const params = (config.params ?? {}) as Partial<GetRoomsRequest>;
    const search = normalizeSearch(params.search);
    const page = Number(params.page ?? 1);
    const pageSize = Number(params.pageSize ?? 20);

    const result = mockDatabase.rooms.filter((room) => {
      const matchesSearch =
        !search ||
        [room.code, room.propertyName, room.description]
          .filter(Boolean)
          .some((value) => normalizeSearch(value).includes(search));
      const matchesProperty = !params.propertyId || room.propertyId === params.propertyId;
      const matchesStatus = !params.status || room.status === params.status;

      return matchesSearch && matchesProperty && matchesStatus;
    });

    return [200, paginate(result, page, pageSize)];
  });

  mock.onGet(roomDetailPattern).reply((config) => {
    const roomId = getPathId(config, roomDetailPattern);
    const room = mockDatabase.rooms.find((item) => item.id === roomId);

    return room
      ? [200, room]
      : [404, problem(404, "ERR-ROOM-404", "Không tìm thấy phòng.")];
  });

  mock.onPost("/api/rooms").reply((config) => {
    const payload = readBody<CreateRoomRequest>(config);
    const property = mockDatabase.properties.find((item) => item.id === payload.propertyId);

    if (!property) {
      return [
        400,
        problem(400, "ERR-ROOM-PROPERTY", "Khu nhà không tồn tại.", {
          PropertyId: ["Khu nhà không tồn tại."],
        }),
      ];
    }

    const duplicated = mockDatabase.rooms.some(
      (item) => item.propertyId === payload.propertyId && item.code.toLocaleLowerCase() === payload.code.toLocaleLowerCase(),
    );

    if (duplicated) {
      return [
        400,
        problem(400, "ERR-ROOM-001", "Mã phòng đã tồn tại trong khu nhà.", {
          Code: ["Mã phòng đã tồn tại trong khu nhà."],
        }),
      ];
    }

    const now = new Date().toISOString();
    const room: Room = {
      id: crypto.randomUUID(),
      ...payload,
      propertyName: property.name,
      createdAt: now,
      updatedAt: now,
    };

    mockDatabase.rooms.unshift(room);
    return [201, room];
  });

  mock.onPut(roomDetailPattern).reply((config) => {
    const roomId = getPathId(config, roomDetailPattern);
    const index = mockDatabase.rooms.findIndex((item) => item.id === roomId);

    if (index < 0) {
      return [404, problem(404, "ERR-ROOM-404", "Không tìm thấy phòng.")];
    }

    const payload = readBody<UpdateRoomRequest>(config);
    const property = mockDatabase.properties.find((item) => item.id === payload.propertyId);

    if (!property) {
      return [
        400,
        problem(400, "ERR-ROOM-PROPERTY", "Khu nhà không tồn tại.", {
          PropertyId: ["Khu nhà không tồn tại."],
        }),
      ];
    }

    const duplicated = mockDatabase.rooms.some(
      (item) =>
        item.id !== roomId &&
        item.propertyId === payload.propertyId &&
        item.code.toLocaleLowerCase() === payload.code.toLocaleLowerCase(),
    );

    if (duplicated) {
      return [
        400,
        problem(400, "ERR-ROOM-001", "Mã phòng đã tồn tại trong khu nhà.", {
          Code: ["Mã phòng đã tồn tại trong khu nhà."],
        }),
      ];
    }

    const updated: Room = {
      ...mockDatabase.rooms[index],
      ...payload,
      propertyName: property.name,
      updatedAt: new Date().toISOString(),
    };

    mockDatabase.rooms[index] = updated;
    return [200, updated];
  });

  mock.onDelete(roomDetailPattern).reply((config) => {
    const roomId = getPathId(config, roomDetailPattern);
    const index = mockDatabase.rooms.findIndex((item) => item.id === roomId);

    if (index < 0) {
      return [404, problem(404, "ERR-ROOM-404", "Không tìm thấy phòng.")];
    }

    mockDatabase.rooms.splice(index, 1);
    return [204];
  });
}

function getPropertyTypeName(propertyTypeId: string) {
  const names: Record<string, string> = {
    "boarding-house": "Nhà trọ",
    apartment: "Căn hộ",
    "shared-house": "Nhà nguyên căn",
  };

  return names[propertyTypeId] ?? propertyTypeId;
}
