import { test as base, expect, type Page } from "@playwright/test";

/** Keep in sync with src/api/mocks/auth-constants.ts */
export const MOCK_ACCESS_TOKEN = "mock-access-token";
export const MOCK_ORGANIZATION_ID = "org-mock-1";

/** Seed a deterministic mock session before any app script reads localStorage. */
export async function seedMockAuth(page: Page) {
  await page.addInitScript(
    ({ token, organizationId }) => {
      window.localStorage.setItem("access_token", token);
      window.localStorage.setItem("organization_id", organizationId);
    },
    { token: MOCK_ACCESS_TOKEN, organizationId: MOCK_ORGANIZATION_ID },
  );
}

export async function expectAuthenticatedUrl(page: Page) {
  await expect(page).not.toHaveURL(/\/login(?:\?|$)/);
}

export const test = base.extend({
  page: async ({ page }, use) => {
    await seedMockAuth(page);
    await use(page);
  },
});

export { expect };
