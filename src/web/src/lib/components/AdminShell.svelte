<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { logout } from '$lib/api/client';

	let { children } = $props();

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

<div class="flex min-h-screen flex-col bg-bg text-text">
	<header class="flex items-center justify-between border-b border-border bg-surface px-4 py-3">
		<span class="text-lg font-semibold tracking-tight">Admin</span>
		<button
			type="button"
			onclick={handleLogout}
			disabled={loggingOut}
			class="rounded-lg border border-border px-3 py-1.5 text-sm font-medium text-text-muted transition-colors hover:border-danger hover:text-danger disabled:opacity-50"
		>
			Log out
		</button>
	</header>

	<main class="flex-1 overflow-y-auto p-4 md:p-8">
		{@render children()}
	</main>
</div>
