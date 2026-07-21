import type { AxiosRequestConfig } from "axios";
import type MockAdapter from "axios-mock-adapter";

import type {
  ApiErrorResponse,
  ApiFieldError,
  ApiMessageParameters,
  ApiSuccessResponse,
  ErrorMessageKey,
  PageResult,
  SuccessMessageKey,
} from "@/api/client";
import { mockDatabase } from "@/api/mocks/database";
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

const propertyDetailPattern = /\/api\/properties\/([^/]+)$/;
const roomDetailPattern = /\/api\/rooms\/([^/]+)$/;

function readBody<T>(config: AxiosRequestConfig): T {
  return (typeof config.data === "string" ? JSON.parse(config.data) : config.data) as T;
}

function getPathId(config: AxiosRequestConfig, pattern: RegExp) {
  return config.url?.match(pattern)?.[1] ?? "";
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

function success<T>(
  messageKey: SuccessMessageKey,
  data: T,
  parameters: ApiMessageParameters,
): ApiSuccessResponse<T> {
  return {
    success: true,
    messageKey,
    data,
    parameters,
    correlationId: `mock-${crypto.randomUUID()}`,
  };
}

function problem(
  messageKey: ErrorMessageKey,
  parameters: ApiMessageParameters = {},
  fieldErrors: ApiFieldError[] = [],
): ApiErrorResponse {
  return {
    success: false,
    messageKey,
    parameters,
    fieldErrors,
    correlationId: `mock-${crypto.randomUUID()}`,
  };
}

function normalizeSearch(value: unknown) {
  return String(value ?? "").trim().toLocaleLowerCase("vi");
}

export function registerMockHandlers(mock: MockAdapter) {
  registerPropertyHandlers(mock);
  registerRoomHandlers(mock);
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

    return [
      200,
      success("SCS-005", paginate(result, page, pageSize), { object: "property" }),
    ];
  });

  mock.onGet(propertyDetailPattern).reply((config) => {
    const propertyId = getPathId(config, propertyDetailPattern);
    const property = mockDatabase.properties.find((item) => item.id === propertyId);
    return property
      ? [200, success("SCS-005", property, { object: "property" })]
      : [404, problem("ERR-002", { object: "property" })];
  });

  mock.onPost("/api/properties").reply((config) => {
    const payload = readBody<CreatePropertyRequest>(config);
    const duplicated = mockDatabase.properties.some(
      (item) => item.code.toLocaleLowerCase() === payload.code.toLocaleLowerCase(),
    );

    if (duplicated) {
      const parameters = { object: "property", key: payload.code };
      return [
        409,
        problem("ERR-007", parameters, [
          { fieldKey: "code", messageKey: "ERR-007", parameters },
        ]),
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
    return [201, success("SCS-001", property, { object: "property" })];
  });

  mock.onPut(propertyDetailPattern).reply((config) => {
    const propertyId = getPathId(config, propertyDetailPattern);
    const index = mockDatabase.properties.findIndex((item) => item.id === propertyId);
    if (index < 0) return [404, problem("ERR-002", { object: "property" })];

    const payload = readBody<UpdatePropertyRequest>(config);
    const duplicated = mockDatabase.properties.some(
      (item) => item.id !== propertyId && item.code.toLocaleLowerCase() === payload.code.toLocaleLowerCase(),
    );
    if (duplicated) {
      const parameters = { object: "property", key: payload.code };
      return [409, problem("ERR-007", parameters, [
        { fieldKey: "code", messageKey: "ERR-007", parameters },
      ])];
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
    return [200, success("SCS-002", updated, { object: "property" })];
  });

  mock.onDelete(propertyDetailPattern).reply((config) => {
    const propertyId = getPathId(config, propertyDetailPattern);
    const index = mockDatabase.properties.findIndex((item) => item.id === propertyId);
    if (index < 0) return [404, problem("ERR-002", { object: "property" })];
    if (mockDatabase.rooms.some((room) => room.propertyId === propertyId)) {
      return [409, problem("ERR-017", { object: "property", dependency: "room" })];
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
    return [200, success("SCS-005", paginate(result, page, pageSize), { object: "room" })];
  });

  mock.onGet(roomDetailPattern).reply((config) => {
    const roomId = getPathId(config, roomDetailPattern);
    const room = mockDatabase.rooms.find((item) => item.id === roomId);
    return room
      ? [200, success("SCS-005", room, { object: "room" })]
      : [404, problem("ERR-002", { object: "room" })];
  });

  mock.onPost("/api/rooms").reply((config) => {
    const payload = readBody<CreateRoomRequest>(config);
    const property = mockDatabase.properties.find((item) => item.id === payload.propertyId);
    if (!property) {
      return [422, problem("ERR-002", { object: "property" }, [
        { fieldKey: "propertyId", messageKey: "ERR-002", parameters: { object: "property" } },
      ])];
    }
    const duplicated = mockDatabase.rooms.some(
      (item) => item.propertyId === payload.propertyId && item.code.toLocaleLowerCase() === payload.code.toLocaleLowerCase(),
    );
    if (duplicated) {
      const parameters = { object: "room", key: payload.code };
      return [409, problem("ERR-007", parameters, [
        { fieldKey: "code", messageKey: "ERR-007", parameters },
      ])];
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
    return [201, success("SCS-001", room, { object: "room" })];
  });

  mock.onPut(roomDetailPattern).reply((config) => {
    const roomId = getPathId(config, roomDetailPattern);
    const index = mockDatabase.rooms.findIndex((item) => item.id === roomId);
    if (index < 0) return [404, problem("ERR-002", { object: "room" })];

    const payload = readBody<UpdateRoomRequest>(config);
    const property = mockDatabase.properties.find((item) => item.id === payload.propertyId);
    if (!property) {
      return [422, problem("ERR-002", { object: "property" }, [
        { fieldKey: "propertyId", messageKey: "ERR-002", parameters: { object: "property" } },
      ])];
    }
    const duplicated = mockDatabase.rooms.some(
      (item) =>
        item.id !== roomId &&
        item.propertyId === payload.propertyId &&
        item.code.toLocaleLowerCase() === payload.code.toLocaleLowerCase(),
    );
    if (duplicated) {
      const parameters = { object: "room", key: payload.code };
      return [409, problem("ERR-007", parameters, [
        { fieldKey: "code", messageKey: "ERR-007", parameters },
      ])];
    }

    const updated: Room = {
      ...mockDatabase.rooms[index],
      ...payload,
      propertyName: property.name,
      updatedAt: new Date().toISOString(),
    };
    mockDatabase.rooms[index] = updated;
    return [200, success("SCS-002", updated, { object: "room" })];
  });

  mock.onDelete(roomDetailPattern).reply((config) => {
    const roomId = getPathId(config, roomDetailPattern);
    const index = mockDatabase.rooms.findIndex((item) => item.id === roomId);
    if (index < 0) return [404, problem("ERR-002", { object: "room" })];
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
