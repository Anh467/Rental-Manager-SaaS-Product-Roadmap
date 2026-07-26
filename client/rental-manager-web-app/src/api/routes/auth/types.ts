export type AuthScope = "global" | "organization";

export type AuthUser = {
  id: string;
  name: string;
  email: string;
  scope: AuthScope;
  organizationId?: string | null;
  role?: { key: string; name: string } | null;
  permissions: string[];
};

export type LoginRequest = {
  email: string;
  password: string;
  organizationId?: string;
};

export type LoginResponse = {
  accessToken?: string;
  access_token?: string;
  token?: string;
};
