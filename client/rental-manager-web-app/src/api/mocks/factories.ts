import { faker } from "@faker-js/faker";

import type { Property } from "@/api/routes/properties";
import type { Room, RoomStatus } from "@/api/routes/rooms";

const PROPERTY_TYPES = [
  { id: "boarding-house", name: "Nhà trọ" },
  { id: "apartment", name: "Căn hộ" },
  { id: "shared-house", name: "Nhà nguyên căn" },
] as const;

const PROPERTY_NAMES = [
  "Nhà trọ Minh Anh",
  "Khu trọ An Phú",
  "Căn hộ Hải Châu",
  "Nhà trọ Sinh Viên",
  "Khu phòng trọ Hòa Khánh",
  "Căn hộ Sơn Trà",
  "Nhà trọ Thanh Khê",
  "Khu trọ Bình An",
  "Nhà cho thuê Ngũ Hành Sơn",
  "Căn hộ Cẩm Lệ",
  "Nhà trọ Phước Mỹ",
  "Khu lưu trú Hòa Xuân",
] as const;

const ROOM_STATUSES: RoomStatus[] = [
  "vacant",
  "occupied",
  "occupied",
  "occupied",
  "maintenance",
  "inactive",
];

export function createMockProperties(count = PROPERTY_NAMES.length): Property[] {
  faker.seed(4672026);

  return Array.from({ length: count }, (_, index) => {
    const propertyType = PROPERTY_TYPES[index % PROPERTY_TYPES.length];
    const createdAt = faker.date.recent({ days: 365 }).toISOString();

    return {
      id: faker.string.uuid(),
      name: PROPERTY_NAMES[index % PROPERTY_NAMES.length],
      code: `PROP-${String(index + 1).padStart(3, "0")}`,
      propertyTypeId: propertyType.id,
      propertyTypeName: propertyType.name,
      address: `${faker.location.streetAddress()}, Đà Nẵng`,
      totalFloors: faker.number.int({ min: 2, max: 7 }),
      description: faker.lorem.sentence(),
      isActive: index % 10 !== 9,
      createdAt,
      updatedAt: createdAt,
    };
  });
}

export function createMockRooms(properties: Property[]): Room[] {
  faker.seed(4672027);

  return properties.flatMap((property, propertyIndex) => {
    const roomCount = faker.number.int({ min: 8, max: 18 });

    return Array.from({ length: roomCount }, (_, roomIndex) => {
      const floor = (roomIndex % property.totalFloors) + 1;
      const status = faker.helpers.arrayElement(ROOM_STATUSES);
      const createdAt = faker.date.recent({ days: 240 }).toISOString();

      return {
        id: faker.string.uuid(),
        propertyId: property.id,
        propertyName: property.name,
        code: `${property.code.replace("PROP", "P")}-${floor}${String(roomIndex + 1).padStart(2, "0")}`,
        floor,
        capacity: faker.number.int({ min: 1, max: 4 }),
        monthlyRent: faker.number.int({ min: 18, max: 65 }) * 100_000,
        status,
        description: faker.datatype.boolean() ? faker.lorem.sentence() : undefined,
        isActive: status !== "inactive",
        createdAt,
        updatedAt: createdAt,
      } satisfies Room;
    });
  });
}
