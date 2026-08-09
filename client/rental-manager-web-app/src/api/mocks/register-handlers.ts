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
import type { LoginRequest, SelectOrganizationRequest } from "@/api/routes/auth";
import {
  MOCK_LOGIN_PROVIDER,
  MOCK_LOGIN_SUBJECT,
  MOCK_MULTI_ORG_EMAIL,
  MOCK_MULTI_ORG_PROVIDER,
  MOCK_MULTI_ORG_SUBJECT,
  MOCK_ORGANIZATION_ID,
  MOCK_SESSION_COOKIE,
  buildMockAuthUser,
  mockOrganizations,
} from "@/api/mocks/auth-constants";

export {
  MOCK_LOGIN_EMAIL,
  MOCK_LOGIN_PROVIDER,
  MOCK_LOGIN_SUBJECT,
  MOCK_MULTI_ORG_EMAIL,
  MOCK_MULTI_ORG_PROVIDER,
  MOCK_MULTI_ORG_SUBJECT,
  MOCK_ORGANIZATION_ID,
  MOCK_SESSION_COOKIE,
  mockAuthUser,
  mockOrganizations,
} from "@/api/mocks/auth-constants";

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

function success<T>(messageKey: SuccessMessageKey, data: T, parameters: ApiMessageParameters): ApiSuccessResponse<T> {
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
  registerAuthHandlers(mock);
  registerPropertyHandlers(mock);
  registerRoomHandlers(mock);
  mock.onAny().passThrough();
}

/** Cookie-like mock session that survives full page reloads in the browser. */
let mockCsrfToken = `mock-csrf-${crypto.randomUUID()}`;
const mockSelectionTickets = new Map<string, { email: string; organizationIds: string[] }>();

function readCookie(name: string) {
  if (typeof document === "undefined") return null;
  const match = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}

function writeSessionCookie(organizationId: string | null) {
  if (typeof document === "undefined") return;
  if (!organizationId) {
    document.cookie = `${MOCK_SESSION_COOKIE}=; Path=/; Max-Age=0; SameSite=Lax`;
    return;
  }
  document.cookie = `${MOCK_SESSION_COOKIE}=${encodeURIComponent(organizationId)}; Path=/; SameSite=Lax`;
}

function getHeaderValue(config: AxiosRequestConfig, name: string) {
  const headers = config.headers ?? {};
  const direct = (headers as Record<string, unknown>)[name]
    ?? (headers as Record<string, unknown>)[name.toLowerCase()];
  if (typeof direct === "string") return direct;
  if (typeof headers.get === "function") {
    const value = headers.get(name);
    return typeof value === "string" ? value : "";
  }
  return "";
}

function requireCsrf(config: AxiosRequestConfig): [number, ApiErrorResponse] | null {
  const token = getHeaderValue(config, "X-CSRF-TOKEN");
  if (!token || token !== mockCsrfToken) {
    return [400, problem("ERR-001")];
  }
  return null;
}

function getSessionUser() {
  const organizationId = readCookie(MOCK_SESSION_COOKIE);
  if (!organizationId) return null;
  return buildMockAuthUser(organizationId);
}

function registerAuthHandlers(mock: MockAdapter) {
  mock.onGet("/api/v1/auth/csrf").reply(() => {
    mockCsrfToken = `mock-csrf-${crypto.randomUUID()}`;
    return [200, success("SCS-005", { requestToken: mockCsrfToken }, { object: "csrf" })];
  });

  mock.onPost("/api/v1/auth/login").reply((config) => {
    const csrfFailure = requireCsrf(config);
    if (csrfFailure) return csrfFailure;

    const payload = readBody<LoginRequest>(config) ?? {};
    const provider = payload.provider?.trim() || MOCK_LOGIN_PROVIDER;
    const subject = payload.subject?.trim();

    if (!subject) {
      return [400, problem("ERR-001", {}, [
        { fieldKey: "subject", messageKey: "ERR-001", parameters: { field: "subject" } },
      ])];
    }

    if (provider === MOCK_MULTI_ORG_PROVIDER && subject === MOCK_MULTI_ORG_SUBJECT) {
      const selectionTicket = `ticket-${crypto.randomUUID()}`;
      mockSelectionTickets.set(selectionTicket, {
        email: MOCK_MULTI_ORG_EMAIL,
        organizationIds: mockOrganizations.map((item) => item.id),
      });
      return [200, success("SCS-017", {
        status: "organizationSelectionRequired",
        organizations: mockOrganizations,
        selectionTicket,
      }, { object: "session" })];
    }

    if (provider === MOCK_LOGIN_PROVIDER && subject === MOCK_LOGIN_SUBJECT) {
      writeSessionCookie(MOCK_ORGANIZATION_ID);
      return [200, success("SCS-017", buildMockAuthUser(MOCK_ORGANIZATION_ID), { object: "session" })];
    }

    return [401, problem("ERR-003")];
  });

  mock.onPost("/api/v1/auth/select-organization").reply((config) => {
    const csrfFailure = requireCsrf(config);
    if (csrfFailure) return csrfFailure;

    const payload = readBody<SelectOrganizationRequest>(config);
    const ticket = mockSelectionTickets.get(payload?.selectionTicket ?? "");
    if (!ticket || !payload?.organizationId || !ticket.organizationIds.includes(payload.organizationId)) {
      return [401, problem("ERR-003")];
    }

    mockSelectionTickets.delete(payload.selectionTicket);
    writeSessionCookie(payload.organizationId);
    return [200, success(
      "SCS-017",
      buildMockAuthUser(payload.organizationId, ticket.email),
      { object: "session" },
    )];
  });

  mock.onPost("/api/v1/auth/logout").reply((config) => {
    const csrfFailure = requireCsrf(config);
    if (csrfFailure) return csrfFailure;

    if (!readCookie(MOCK_SESSION_COOKIE)) {
      return [401, problem("ERR-003")];
    }

    writeSessionCookie(null);
    return [200, success("SCS-018", null, { object: "session" })];
  });

  mock.onGet("/api/v1/auth/me").reply(() => {
    const user = getSessionUser();
    if (!user) return [401, problem("ERR-003")];
    return [200, success("SCS-005", user, { object: "user" })];
  });
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
    return [200, success("SCS-005", paginate(result, page, pageSize), { object: "property" })];
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
      return [409, problem("ERR-007", parameters, [
        { fieldKey: "code", messageKey: "ERR-007", parameters },
      ])];
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
    const property = mockDatabase.properties.find((item) => item.id === propertyId);
    if (!property) return [404, problem("ERR-002", { object: "property" })];
    if (mockDatabase.rooms.some((room) => room.propertyId === propertyId && room.isActive)) {
      return [409, problem("ERR-017", { object: "property", dependency: "room" })];
    }

    property.isActive = false;
    property.updatedAt = new Date().toISOString();
    return [200, success("SCS-003", null, { object: "property" })];
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
    const room = mockDatabase.rooms.find((item) => item.id === roomId);
    if (!room) return [404, problem("ERR-002", { object: "room" })];

    room.isActive = false;
    room.status = "inactive";
    room.updatedAt = new Date().toISOString();
    return [200, success("SCS-003", null, { object: "room" })];
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
