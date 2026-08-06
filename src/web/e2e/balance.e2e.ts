import { expect, test } from '@playwright/test';
import {
	API_ORIGIN,
	jsonResponse,
	mockAuthenticated,
	mockNotifications,
	mockUser
} from './support';

test('balance screen shows the current balance', async ({ page }) => {
	await mockAuthenticated(page);
	await mockNotifications(page, []);
	await page.route(`${API_ORIGIN}/players/${mockUser.id}/balance`, (route) =>
		route.fulfill(jsonResponse({ playerUserId: mockUser.id, balance: 25 }))
	);

	await page.goto('/balance');

	await expect(page.getByText('Outstanding balance')).toBeVisible();
	await expect(page.getByText('$25.00')).toBeVisible();
});

test('balance screen shows a credit balance', async ({ page }) => {
	await mockAuthenticated(page);
	await mockNotifications(page, []);
	await page.route(`${API_ORIGIN}/players/${mockUser.id}/balance`, (route) =>
		route.fulfill(jsonResponse({ playerUserId: mockUser.id, balance: -10 }))
	);

	await page.goto('/balance');

	await expect(page.getByText('Credit', { exact: true })).toBeVisible();
	await expect(page.getByText('$10.00')).toBeVisible();
});
