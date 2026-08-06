import { redirect } from '@sveltejs/kit';
import { getMe } from '$lib/api/client';
import type { LayoutLoad } from './$types';

// Same CSR-only auth model as the (app) group, plus a role check — only Admins get past here.
export const ssr = false;

export const load: LayoutLoad = async () => {
	let user;
	try {
		user = await getMe();
	} catch {
		redirect(307, '/login');
	}

	if (user.role !== 'Admin') {
		redirect(307, '/');
	}

	return { user };
};
