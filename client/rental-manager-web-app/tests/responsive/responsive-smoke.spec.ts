import { expect, test, type Page } from "@playwright/test";

const viewports = [
  { name: "small-phone", width: 320, height: 640 },
  { name: "phone", width: 390, height: 844 },
  { name: "tablet-portrait", width: 768, height: 1024 },
  { name: "tablet-landscape", width: 1024, height: 768 },
  { name: "desktop", width: 1440, height: 900 },
  { name: "wide-desktop", width: 1920, height: 1080 },
] as const;

async function expectNoDocumentOverflow(page: Page) {
  await expect.poll(async () => page.evaluate(() => {
    const documentWidth = Math.max(
      document.documentElement.scrollWidth,
      document.body.scrollWidth,
    );
    return documentWidth - window.innerWidth;
  })).toBeLessThanOrEqual(1);
}

for (const viewport of viewports) {
  test.describe(viewport.name, () => {
    test.use({ viewport: { width: viewport.width, height: viewport.height } });

    test("Property and Room baselines do not overflow and switch layouts correctly", async ({ page }) => {
      for (const path of ["/properties", "/rooms"] as const) {
        await page.goto(path);
        await expect(page.locator("h1")).toBeVisible();
        await expectNoDocumentOverflow(page);

        if (viewport.width < 768) {
          await expect(page.getByTestId("mobile-data-list")).toBeVisible();
          await expect(page.getByTestId("desktop-data-table")).toBeHidden();
        } else {
          await expect(page.getByTestId("desktop-data-table")).toBeVisible();
          await expect(page.getByTestId("mobile-data-list")).toBeHidden();
        }

        if (viewport.width < 1024) {
          await expect(page.getByTestId("mobile-menu-trigger")).toBeVisible();
          await expect(page.getByTestId("desktop-sidebar")).toBeHidden();
        } else {
          await expect(page.getByTestId("mobile-menu-trigger")).toBeHidden();
          await expect(page.getByTestId("desktop-sidebar")).toBeVisible();
        }
      }
    });
  });
}

test.describe("mobile navigation drawer", () => {
  test.use({ viewport: { width: 390, height: 844 } });

  test("opens, navigates and closes without page overflow", async ({ page }) => {
    await page.goto("/properties");
    await page.getByTestId("mobile-menu-trigger").click();
    await expect(page.getByTestId("mobile-navigation-drawer")).toBeVisible();

    await page.getByRole("link", { name: /phòng|rooms/i }).click();
    await expect(page).toHaveURL(/\/rooms/);
    await expect(page.getByTestId("mobile-navigation-drawer")).toBeHidden();
    await expectNoDocumentOverflow(page);
  });
});
