import { expect, test } from '@playwright/test';
import { mockAuthenticated, mockNoLiveGame, mockNotifications } from './support';

test.beforeEach(async ({ page }) => {
	await mockAuthenticated(page);
	await mockNotifications(page, []);
	await mockNoLiveGame(page);
});

test('shows a bottom tab bar and no sidebar under the desktop breakpoint', async ({ page }) => {
	await page.setViewportSize({ width: 500, height: 800 });
	await page.goto('/');

	await expect(page.locator('nav[aria-label="Primary"]')).toBeVisible();
	await expect(page.locator('aside')).toBeHidden();
});

test('shows a sidebar and no bottom tab bar at/above the desktop breakpoint', async ({ page }) => {
	await page.setViewportSize({ width: 1200, height: 800 });
	await page.goto('/');

	await expect(page.locator('aside')).toBeVisible();
	await expect(page.locator('nav[aria-label="Primary"]')).toBeHidden();
});

test('theme toggle switches the interface and persists across a reload', async ({ page }) => {
	await page.goto('/');

	const html = page.locator('html');
	const initialTheme = await html.getAttribute('data-theme');

	await page.getByRole('button', { name: /switch to (light|dark) theme/i }).click();

	const toggledTheme = await html.getAttribute('data-theme');
	expect(toggledTheme).not.toBe(initialTheme);

	await page.reload();
	await expect(html).toHaveAttribute('data-theme', toggledTheme ?? '');
});
