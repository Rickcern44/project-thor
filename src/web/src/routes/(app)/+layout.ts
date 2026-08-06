import { redirect } from '@sveltejs/kit';
import { getMe } from '$lib/api/client';
import type { LayoutLoad } from './$types';

// D7: authenticated routes are client-side-only — the SvelteKit server never forwards
// the browser's session cookie to the API, so there is nothing useful to render there.
export const ssr = false;

export const load: LayoutLoad = async () => {
	try {
		const user = await getMe();
		return { user };
	} catch {
		redirect(307, '/login');
	}
};
