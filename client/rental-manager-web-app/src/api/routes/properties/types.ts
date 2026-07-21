import type { PageRequest } from "@/api/types";

export type Property = {
  id: string;
  name: string;
  code: string;
  propertyTypeId: string;
  propertyTypeName?: string;
  address: string;
  totalFloors: number;
  description?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
};

export type GetPropertiesRequest = PageRequest & {
  propertyTypeId?: string;
  isActive?: boolean;
};

export type CreatePropertyRequest = Pick<
  Property,
  "name" | "code" | "propertyTypeId" | "address" | "totalFloors" | "description" | "isActive"
>;

export type UpdatePropertyRequest = CreatePropertyRequest;
