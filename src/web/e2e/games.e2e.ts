import { expect, test, type Route } from '@playwright/test';
import {
	API_ORIGIN,
	jsonResponse,
	mockAuthenticated,
	mockNotifications,
	mockUser
} from './support';

const game = {
	id: 'game-1',
	startsAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
	signupOpensAt: new Date(Date.now() - 60 * 60 * 1000).toISOString(),
	capacity: 1,
	fee: 10,
	isAdHoc: false,
	isCancelled: false,
	state: 'Open'
};

interface SignUp {
	id: string;
	gameId: string;
	playerUserId: string;
	status: 'Rostered' | 'Waitlisted';
	waitlistPosition: number | null;
	signedUpAt: string;
}

test.beforeEach(async ({ page }) => {
	await mockAuthenticated(page);
	await mockNotifications(page, []);
	await page.route(`${API_ORIGIN}/games/live`, (route: Route) => route.fulfill(jsonResponse(game)));
});

test('signing up while open and under capacity shows rostered state', async ({ page }) => {
	const openGame = { ...game, capacity: 2 };
	let roster: SignUp[] = [];

	await page.unroute(`${API_ORIGIN}/games/live`);
	await page.route(`${API_ORIGIN}/games/live`, (route: Route) =>
		route.fulfill(jsonResponse(openGame))
	);
	await page.route(`${API_ORIGIN}/games/${openGame.id}/roster`, (route: Route) =>
		route.fulfill(jsonResponse(roster))
	);
	await page.route(`${API_ORIGIN}/games/${openGame.id}/signup`, (route: Route) => {
		const signUp: SignUp = {
			id: 'signup-1',
			gameId: openGame.id,
			playerUserId: mockUser.id,
			status: 'Rostered',
			waitlistPosition: null,
			signedUpAt: new Date().toISOString()
		};
		roster = [signUp];
		return route.fulfill(jsonResponse(signUp, 201));
	});

	await page.goto('/');
	await page.getByRole('button', { name: 'Sign up' }).click();

	await expect(page.getByText("You're rostered for this game")).toBeVisible();
	await expect(page.getByText('1 / 2 rostered')).toBeVisible();
});

test('signing up at capacity shows waitlisted state with position', async ({ page }) => {
	let roster: SignUp[] = [
		{
			id: 'existing',
			gameId: game.id,
			playerUserId: 'other-player',
			status: 'Rostered',
			waitlistPosition: null,
			signedUpAt: new Date().toISOString()
		}
	];

	await page.route(`${API_ORIGIN}/games/${game.id}/roster`, (route: Route) =>
		route.fulfill(jsonResponse(roster))
	);
	await page.route(`${API_ORIGIN}/games/${game.id}/signup`, (route: Route) => {
		const signUp: SignUp = {
			id: 'signup-2',
			gameId: game.id,
			playerUserId: mockUser.id,
			status: 'Waitlisted',
			waitlistPosition: 1,
			signedUpAt: new Date().toISOString()
		};
		roster = [...roster, signUp];
		return route.fulfill(jsonResponse(signUp, 201));
	});

	await page.goto('/');
	await expect(page.getByRole('button', { name: 'Join waitlist' })).toBeVisible();
	await page.getByRole('button', { name: 'Join waitlist' }).click();

	await expect(page.getByText('Waitlisted — position #1')).toBeVisible();
});

test('cancel removes the sign-up and updates the card', async ({ page }) => {
	let roster: SignUp[] = [
		{
			id: 'signup-1',
			gameId: game.id,
			playerUserId: mockUser.id,
			status: 'Rostered',
			waitlistPosition: null,
			signedUpAt: new Date().toISOString()
		}
	];

	await page.route(`${API_ORIGIN}/games/${game.id}/roster`, (route: Route) =>
		route.fulfill(jsonResponse(roster))
	);
	await page.route(`${API_ORIGIN}/games/${game.id}/cancel`, (route: Route) => {
		roster = [];
		return route.fulfill({ status: 204 });
	});

	await page.goto('/');
	await expect(page.getByText("You're rostered for this game")).toBeVisible();

	await page.getByRole('button', { name: 'Cancel sign-up' }).click();

	await expect(page.getByText("You're rostered for this game")).toBeHidden();
	await expect(page.getByRole('button', { name: 'Sign up' })).toBeVisible();
	await expect(page.getByText('0 / 1 rostered')).toBeVisible();
});

test('no live game renders the empty state, not a broken or blank card', async ({ page }) => {
	await page.unroute(`${API_ORIGIN}/games/live`);
	await page.route(`${API_ORIGIN}/games/live`, (route: Route) => route.fulfill({ status: 204 }));

	await page.goto('/');

	await expect(page.getByText('No upcoming game scheduled')).toBeVisible();
	await expect(page.getByRole('button', { name: /sign up|join waitlist/i })).toHaveCount(0);
});
