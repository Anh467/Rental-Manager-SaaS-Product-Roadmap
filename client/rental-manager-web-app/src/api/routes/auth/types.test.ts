import { describe, expect, it } from "vitest";

import { isOrganizationSelectionRequired, type LoginResponse } from "./types";

describe("isOrganizationSelectionRequired", () => {
  it("detects organization selection payloads", () => {
    const response: LoginResponse = {
      status: "organizationSelectionRequired",
      organizations: [{ id: "org-1", name: "Org 1" }],
      selectionTicket: "ticket",
    };
    expect(isOrganizationSelectionRequired(response)).toBe(true);
  });

  it("rejects authenticated user payloads", () => {
    const response: LoginResponse = {
      id: "user-1",
      name: "User",
      email: "user@example.com",
      scope: "organization",
      organizationId: "org-1",
      permissions: [],
    };
    expect(isOrganizationSelectionRequired(response)).toBe(false);
  });
});
