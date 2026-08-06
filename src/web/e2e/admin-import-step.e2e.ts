import { expect, test } from '@playwright/test';
import { API_ORIGIN, jsonResponse, mockAdminUser, mockAuthenticated } from './support';

test.beforeEach(async ({ page }) => {
	await mockAuthenticated(page, mockAdminUser);
});

test('a successful import shows the summary counts', async ({ page }) => {
	await page.route(`${API_ORIGIN}/admin/import/roster`, (route) =>
		route.fulfill(jsonResponse({ gamesCreated: 3, rowsFlagged: 2, rowsSkippedAsDuplicate: 1 }))
	);

	await page.goto('/admin/import');
	await page.locator('#csv-file-input').setInputFiles({
		name: 'roster.csv',
		mimeType: 'text/csv',
		buffer: Buffer.from('Name,8-Jan,Total Due,Amount Paid\nJane Doe,x,50,50\n')
	});
	await page.getByRole('button', { name: 'Import' }).click();

	await expect(page.getByText('Import complete')).toBeVisible();
	await expect(page.getByText('3', { exact: true })).toBeVisible();
	await expect(page.getByText('2', { exact: true })).toBeVisible();
	await expect(page.getByText('1', { exact: true })).toBeVisible();
});

test('selecting a non-CSV file is rejected before any upload call', async ({ page }) => {
	let uploadCalled = false;
	await page.route(`${API_ORIGIN}/admin/import/roster`, (route) => {
		uploadCalled = true;
		return route.fulfill(
			jsonResponse({ gamesCreated: 0, rowsFlagged: 0, rowsSkippedAsDuplicate: 0 })
		);
	});

	await page.goto('/admin/import');
	await page.locator('#csv-file-input').setInputFiles({
		name: 'roster.xlsx',
		mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
		buffer: Buffer.from('not a csv')
	});

	await expect(page.getByText('is not a CSV file')).toBeVisible();
	await expect(page.getByRole('button', { name: 'Import' })).toBeDisabled();
	expect(uploadCalled).toBe(false);
});
