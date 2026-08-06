import { env } from '$env/dynamic/public';

// Dynamic (not static) public env: resolved at request/dev-server time rather than baked into
// the client bundle at build time, so the same built image can point at different API origins
// per environment. Falls back to the local API port so it works with no env var set at all
// (e.g. in CI, which has no `.env`).
const API_ORIGIN = env.PUBLIC_API_ORIGIN ?? 'http://localhost:5044';

export class ApiError extends Error {
	constructor(
		public status: number,
		message: string
	) {
		super(message);
	}
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
	// A FormData body (multipart upload) must not get a manual Content-Type — fetch sets its
	// own with the correct boundary, and overriding it here would corrupt the request.
	const isFormData = init?.body instanceof FormData;
	const response = await fetch(`${API_ORIGIN}${path}`, {
		...init,
		credentials: 'include',
		headers: {
			...(init?.body && !isFormData ? { 'Content-Type': 'application/json' } : {}),
			...init?.headers
		}
	});

	if (!response.ok) {
		const detail = await response.text().catch(() => '');
		throw new ApiError(response.status, detail || response.statusText);
	}

	const text = await response.text();
	return (text ? JSON.parse(text) : undefined) as T;
}

export interface MeResponse {
	id: string;
	name: string;
	email: string;
	role: string;
}

export function getMe() {
	return request<MeResponse>('/auth/me');
}

export function requestLoginLink(email: string) {
	return request<void>('/auth/login/request', {
		method: 'POST',
		body: JSON.stringify({ email })
	});
}

export function consumeMagicLink(token: string) {
	return request<MeResponse>('/auth/consume', {
		method: 'POST',
		body: JSON.stringify({ token })
	});
}

export function logout() {
	return request<void>('/auth/logout', { method: 'POST' });
}

export type GameState = 'Closed' | 'Open' | 'Past';

export interface GameResponse {
	id: string;
	startsAt: string;
	signupOpensAt: string;
	capacity: number;
	fee: number;
	isAdHoc: boolean;
	isCancelled: boolean;
	state: GameState;
}

/** Returns `undefined` when there is no live game (API responds 204 No Content). */
export function getLiveGame() {
	return request<GameResponse | undefined>('/games/live');
}

export type SignUpStatus = 'Rostered' | 'Waitlisted';

export interface SignUpResponse {
	id: string;
	gameId: string;
	playerUserId: string;
	status: SignUpStatus;
	waitlistPosition: number | null;
	signedUpAt: string;
}

export function getGameRoster(gameId: string) {
	return request<SignUpResponse[]>(`/games/${gameId}/roster`);
}

export function signUpForGame(gameId: string) {
	return request<SignUpResponse>(`/games/${gameId}/signup`, { method: 'POST' });
}

export function cancelSignUp(gameId: string) {
	return request<void>(`/games/${gameId}/cancel`, { method: 'POST' });
}

export interface BalanceResponse {
	playerUserId: string;
	balance: number;
}

export function getBalance(playerUserId: string) {
	return request<BalanceResponse>(`/players/${playerUserId}/balance`);
}

export interface NotificationResponse {
	id: string;
	type: string;
	message: string;
	createdAt: string;
	readAt: string | null;
}

export function getNotifications() {
	return request<NotificationResponse[]>('/notifications');
}

export function markNotificationRead(id: string) {
	return request<void>(`/notifications/${id}/read`, { method: 'POST' });
}

export interface ImportRosterResult {
	gamesCreated: number;
	rowsFlagged: number;
	rowsSkippedAsDuplicate: number;
}

export function importRoster(file: File, seasonYear: number) {
	const formData = new FormData();
	formData.append('file', file);
	formData.append('seasonYear', String(seasonYear));
	return request<ImportRosterResult>('/admin/import/roster', {
		method: 'POST',
		body: formData
	});
}
