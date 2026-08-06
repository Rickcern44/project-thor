import type { Page, Route } from '@playwright/test';

/** Matches the default `PUBLIC_API_ORIGIN` in `.env` / `.env.example` for local dev and CI. */
export const API_ORIGIN = 'http://localhost:5044';

export const mockUser = {
	id: '11111111-1111-1111-1111-111111111111',
	name: 'Jamie Rivera',
	email: 'jamie@example.com',
	role: 'Player'
};

export function jsonResponse(body: unknown, status = 200) {
	return { status, contentType: 'application/json', body: JSON.stringify(body) };
}

export async function mockAuthenticated(page: Page) {
	await page.route(`${API_ORIGIN}/auth/me`, (route: Route) =>
		route.fulfill(jsonResponse(mockUser))
	);
}

export async function mockUnauthenticated(page: Page) {
	await page.route(`${API_ORIGIN}/auth/me`, (route: Route) => route.fulfill({ status: 401 }));
}

export async function mockNotifications(page: Page, notifications: unknown[] = []) {
	await page.route(`${API_ORIGIN}/notifications`, (route: Route) => {
		if (route.request().method() === 'GET') {
			return route.fulfill(jsonResponse(notifications));
		}
		return route.continue();
	});
}

export async function mockNoLiveGame(page: Page) {
	await page.route(`${API_ORIGIN}/games/live`, (route: Route) => route.fulfill({ status: 204 }));
}
