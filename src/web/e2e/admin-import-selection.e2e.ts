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

test('rows are unselected by default and submitting with nothing selected resolves/invites nothing', async ({
	page
}) => {
	await page.route(`${API_ORIGIN}/admin/import/flagged-rows`, (route) =>
		route.fulfill(jsonResponse([flaggedRow('row-1', 'Jane Doe')]))
	);
	let resolveCalled = false;
	await page.route(`${API_ORIGIN}/admin/import/flagged-rows/row-1/resolve`, (route) => {
		resolveCalled = true;
		return route.fulfill(jsonResponse({ rosterRecordId: 'rr-1', userId: 'u-1' }));
	});

	await page.goto('/admin/import');
	await page.getByRole('button', { name: /2\. Review/ }).click();

	await expect(page.getByRole('checkbox', { name: 'Select Jane Doe' })).not.toBeChecked();
	await expect(page.getByRole('button', { name: 'Submit' })).toBeDisabled();
	expect(resolveCalled).toBe(false);
});

test('selecting a subset and submitting only resolves/invites the selected rows, leaving the rest pending', async ({
	page
}) => {
	await page.route(`${API_ORIGIN}/admin/import/flagged-rows`, (route) =>
		route.fulfill(jsonResponse([flaggedRow('row-1', 'Jane Doe'), flaggedRow('row-2', 'Sam Lee')]))
	);
	let row2ResolveCalled = false;
	await page.route(`${API_ORIGIN}/admin/import/flagged-rows/row-1/resolve`, (route) =>
		route.fulfill(jsonResponse({ rosterRecordId: 'rr-1', userId: 'u-1' }))
	);
	await page.route(`${API_ORIGIN}/admin/import/flagged-rows/row-2/resolve`, (route) => {
		row2ResolveCalled = true;
		return route.fulfill(jsonResponse({ rosterRecordId: 'rr-2', userId: 'u-2' }));
	});
	await page.route(`${API_ORIGIN}/admin/invites`, (route) =>
		route.fulfill(jsonResponse({ userId: 'u-1', email: 'jane@example.com', status: 'Pending' }))
	);

	await page.goto('/admin/import');
	await page.getByRole('button', { name: /2\. Review/ }).click();

	const rows = page.locator('tbody tr');
	await rows.nth(0).getByPlaceholder('email@example.com').fill('jane@example.com');
	await rows.nth(0).getByPlaceholder('555-0100').fill('555-0100');
	await rows.nth(0).getByRole('checkbox').check();
	// row-2 is left filled in but unselected — it should stay untouched.
	await rows.nth(1).getByPlaceholder('email@example.com').fill('sam@example.com');
	await rows.nth(1).getByPlaceholder('555-0100').fill('555-0101');

	await page.getByRole('button', { name: 'Submit' }).click();

	await expect(rows.nth(0).getByText('Invited')).toBeVisible();
	expect(row2ResolveCalled).toBe(false);
});
