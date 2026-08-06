import { expect, test } from '@playwright/test';
import {
	mockAuthenticated,
	mockNoLiveGame,
	mockNotifications,
	mockUnauthenticated
} from './support';

test('an authenticated non-admin visiting /admin/import is denied/redirected', async ({ page }) => {
	await mockAuthenticated(page);
	await mockNotifications(page, []);
	await mockNoLiveGame(page);

	await page.goto('/admin/import');

	await expect(page).toHaveURL(/\/$/);
	await expect(page.getByRole('heading', { name: 'Import Roster' })).toHaveCount(0);
});

test('an unauthenticated visitor hitting /admin/import is redirected to /login', async ({
	page
}) => {
	await mockUnauthenticated(page);

	await page.goto('/admin/import');

	await expect(page).toHaveURL(/\/login$/);
});
