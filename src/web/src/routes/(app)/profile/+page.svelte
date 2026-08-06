<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { logout } from '$lib/api/client';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	let loggingOut = $state(false);

	async function handleLogout() {
		loggingOut = true;
		try {
			await logout();
		} finally {
			await goto(resolve('/login'));
		}
	}
</script>

<svelte:head>
	<title>Profile — Project Thor</title>
</svelte:head>

<h1 class="text-2xl font-semibold">Profile</h1>

<div class="mt-6 max-w-sm rounded-2xl border border-border bg-surface p-6">
	<p class="text-sm text-text-muted">Name</p>
	<p class="mt-1 text-base font-medium">{data.user.name}</p>

	<p class="mt-4 text-sm text-text-muted">Email</p>
	<p class="mt-1 text-base font-medium">{data.user.email}</p>

	<button
		type="button"
		onclick={handleLogout}
		disabled={loggingOut}
		class="mt-6 w-full rounded-lg border border-border px-4 py-2 text-sm font-medium text-text transition-colors hover:border-danger hover:text-danger disabled:opacity-50"
	>
		Log out
	</button>
</div>
