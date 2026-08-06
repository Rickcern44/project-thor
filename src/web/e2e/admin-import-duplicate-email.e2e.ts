import { expect, test } from '@playwright/test';
import { API_ORIGIN, jsonResponse, mockAdminUser, mockAuthenticated } from './support';

function flaggedRow(id: string, name: string) {
	return {
		id,
		rawData: JSON.stringify({
			Name: name,
			AttendedDates: ['2026-01-08'],
			TotalDue: 50,
			AmountPaid: 0
		}),
		reason: 'Missing email and phone (not present in source spreadsheet).',
		createdAt: new Date().toISOString()
	};
}

test.beforeEach(async ({ page }) => {
	await mockAuthenticated(page, mockAdminUser);
});

test('a row whose email collides with an existing player is reported as failed without blocking the other selected rows', async ({
	page
}) => {
	await page.route(`${API_ORIGIN}/admin/import/flagged-rows`, (route) =>
		route.fulfill(jsonResponse([flaggedRow('row-1', 'Jane Doe'), flaggedRow('row-2', 'Sam Lee')]))
	);
	await page.route(`${API_ORIGIN}/admin/import/flagged-rows/row-1/resolve`, (route) =>
		route.fulfill({ status: 409, body: 'A player with this email already exists.' })
	);
	await page.route(`${API_ORIGIN}/admin/import/flagged-rows/row-2/resolve`, (route) =>
		route.fulfill(jsonResponse({ rosterRecordId: 'rr-2', userId: 'u-2' }))
	);
	await page.route(`${API_ORIGIN}/admin/invites`, (route) =>
		route.fulfill(jsonResponse({ userId: 'u-2', email: 'sam@example.com', status: 'Pending' }))
	);

	await page.goto('/admin/import');
	await page.getByRole('button', { name: /2\. Review/ }).click();

	const rows = page.locator('tbody tr');
	await rows.nth(0).getByPlaceholder('email@example.com').fill('existing@example.com');
	await rows.nth(0).getByPlaceholder('555-0100').fill('555-0100');
	await rows.nth(0).getByRole('checkbox').check();
	await rows.nth(1).getByPlaceholder('email@example.com').fill('sam@example.com');
	await rows.nth(1).getByPlaceholder('555-0100').fill('555-0101');
	await rows.nth(1).getByRole('checkbox').check();

	await page.getByRole('button', { name: 'Submit' }).click();

	await expect(rows.nth(0).getByText('A player with this email already exists.')).toBeVisible();
	await expect(rows.nth(1).getByText('Invited')).toBeVisible();
});
