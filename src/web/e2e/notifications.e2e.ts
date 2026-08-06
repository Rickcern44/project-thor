import { expect, test, type Route } from '@playwright/test';
import { API_ORIGIN, jsonResponse, mockAuthenticated, mockNoLiveGame } from './support';

interface Notification {
	id: string;
	type: string;
	message: string;
	createdAt: string;
	readAt: string | null;
}

test('unread indicator shows with unread notifications; opening one marks it read and the indicator updates', async ({
	page
}) => {
	let notifications: Notification[] = [
		{
			id: 'n1',
			type: 'NewGameOpen',
			message: 'A new game is open for sign-up',
			createdAt: new Date().toISOString(),
			readAt: null
		}
	];

	await mockAuthenticated(page);
	await mockNoLiveGame(page);
	await page.route(`${API_ORIGIN}/notifications`, (route: Route) => {
		if (route.request().method() === 'GET') {
			return route.fulfill(jsonResponse(notifications));
		}
		return route.continue();
	});
	await page.route(`${API_ORIGIN}/notifications/n1/read`, (route: Route) => {
		notifications = notifications.map((notification) =>
			notification.id === 'n1'
				? { ...notification, readAt: new Date().toISOString() }
				: notification
		);
		return route.fulfill({ status: 204 });
	});

	await page.setViewportSize({ width: 1200, height: 800 });
	await page.goto('/');

	await expect(page.locator('aside').getByText('1', { exact: true })).toBeVisible();

	await page.locator('aside').getByRole('link', { name: 'Notifications' }).click();
	await expect(page).toHaveURL(/\/notifications$/);

	await expect(page.getByLabel('Unread')).toHaveCount(1);
	await page.getByText('A new game is open for sign-up').click();

	await expect(page.getByLabel('Unread')).toHaveCount(0);
	await expect(page.locator('aside').getByText('1', { exact: true })).toHaveCount(0);
});
