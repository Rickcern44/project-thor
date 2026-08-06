import { expect, test } from '@playwright/test';
import { mockUnauthenticated } from './support';

test('unauthenticated visitor hitting a protected route is redirected to /login', async ({
	page
}) => {
	await mockUnauthenticated(page);

	await page.goto('/');

	await expect(page).toHaveURL(/\/login$/);
});

test('unauthenticated visitor hitting a nested protected route is redirected to /login', async ({
	page
}) => {
	await mockUnauthenticated(page);

	await page.goto('/balance');

	await expect(page).toHaveURL(/\/login$/);
});
