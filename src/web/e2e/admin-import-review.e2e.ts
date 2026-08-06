import { expect, test } from '@playwright/test';
import { API_ORIGIN, jsonResponse, mockAdminUser, mockAuthenticated } from './support';

function flaggedRow(
	id: string,
	pending: { Name: string; AttendedDates: string[]; TotalDue: number; AmountPaid: number }
) {
	return {
		id,
		rawData: JSON.stringify(pending),
		reason: 'Missing email and phone (not present in source spreadsheet).',
		createdAt: new Date().toISOString()
	};
}

test.beforeEach(async ({ page }) => {
	await mockAuthenticated(page, mockAdminUser);
});

test("the review list shows each pending row's parsed name, attended dates, total due, and amount paid", async ({
	page
}) => {
	await page.route(`${API_ORIGIN}/admin/import/flagged-rows`, (route) =>
		route.fulfill(
			jsonResponse([
				flaggedRow('row-1', {
					Name: 'Jane Doe',
					AttendedDates: ['2026-01-08', '2026-01-15'],
					TotalDue: 100,
					AmountPaid: 50
				})
			])
		)
	);

	await page.goto('/admin/import');
	await page.getByRole('button', { name: /2\. Review/ }).click();

	await expect(page.getByRole('textbox').first()).toHaveValue('Jane Doe');
	await expect(page.getByText('Jan 8, 2026, Jan 15, 2026')).toBeVisible();
	await expect(page.getByText('$100.00')).toBeVisible();
	await expect(page.getByText('$50.00')).toBeVisible();
});

test('a row missing email or phone cannot be submitted', async ({ page }) => {
	await page.route(`${API_ORIGIN}/admin/import/flagged-rows`, (route) =>
		route.fulfill(
			jsonResponse([
				flaggedRow('row-1', {
					Name: 'Jane Doe',
					AttendedDates: ['2026-01-08'],
					TotalDue: 50,
					AmountPaid: 0
				})
			])
		)
	);
	let resolveCalled = false;
	await page.route(`${API_ORIGIN}/admin/import/flagged-rows/row-1/resolve`, (route) => {
		resolveCalled = true;
		return route.fulfill(jsonResponse({ rosterRecordId: 'rr-1', userId: 'u-1' }));
	});

	await page.goto('/admin/import');
	await page.getByRole('button', { name: /2\. Review/ }).click();
	await page.getByRole('checkbox', { name: 'Select Jane Doe' }).check();
	await page.getByRole('button', { name: 'Submit' }).click();

	await expect(page.getByText('Name, email, and phone are required.')).toBeVisible();
	expect(resolveCalled).toBe(false);
});
