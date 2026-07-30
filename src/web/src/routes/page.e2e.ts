import { expect, test } from '@playwright/test';

test('home page renders the app header', async ({ page }) => {
	await page.goto('/');

	await expect(page.getByRole('heading', { name: 'Project Thor' })).toBeVisible();
});
