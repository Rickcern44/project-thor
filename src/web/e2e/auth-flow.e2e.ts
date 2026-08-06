import { expect, test } from '@playwright/test';
import {
	API_ORIGIN,
	jsonResponse,
	mockAuthenticated,
	mockNoLiveGame,
	mockNotifications,
	mockUser
} from './support';

test('requesting a login link shows the confirmation state', async ({ page }) => {
	await page.route(`${API_ORIGIN}/auth/login/request`, (route) => route.fulfill({ status: 200 }));

	await page.goto('/login');
	await page.getByLabel('Email').fill('jamie@example.com');
	await page.getByRole('button', { name: 'Send login link' }).click();

	await expect(page.getByText('Check your email')).toBeVisible();
});

test('consuming a valid link authenticates and lands on the live game screen', async ({ page }) => {
	await page.route(`${API_ORIGIN}/auth/consume`, (route) => route.fulfill(jsonResponse(mockUser)));
	await mockAuthenticated(page);
	await mockNotifications(page, []);
	await mockNoLiveGame(page);

	await page.goto('/auth/consume?token=valid-token');

	await expect(page).toHaveURL(/\/$/);
	await expect(page.getByRole('heading', { name: 'Live Game' })).toBeVisible();
});

test('consuming an invalid link shows an error with a way to request a new one', async ({
	page
}) => {
	await page.route(`${API_ORIGIN}/auth/consume`, (route) => route.fulfill({ status: 401 }));

	await page.goto('/auth/consume?token=expired-token');

	await expect(page.getByText('This link is invalid or has expired.')).toBeVisible();
	await expect(page.getByRole('link', { name: 'Request a new login link' })).toBeVisible();
});
