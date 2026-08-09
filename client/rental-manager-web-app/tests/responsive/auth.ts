import { test as base, expect, type Page } from "@playwright/test";

/** Keep in sync with src/api/mocks/auth-constants.ts */
export const MOCK_LOGIN_PROVIDER = "oidc";
export const MOCK_LOGIN_SUBJECT = "mock-admin-subject";
export const MOCK_ORGANIZATION_ID = "org-mock-1";

export async function loginAsMockUser(page: Page) {
  await page.context().clearCookies();
  await page.goto("/login");
  await page.getByRole("button", { name: /sign in|đăng nhập/i }).click();
  await expect(page).not.toHaveURL(/\/login(?:\?|$)/);

  const accessToken = await page.evaluate(() => window.localStorage.getItem("access_token"));
  expect(accessToken).toBeNull();
}

export async function expectAuthenticatedUrl(page: Page) {
  await expect(page).not.toHaveURL(/\/login(?:\?|$)/);
}

export const test = base.extend({
  page: async ({ page }, use) => {
    await loginAsMockUser(page);
    await use(page);
  },
});

export { expect };
