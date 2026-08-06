<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { consumeMagicLink } from '$lib/api/client';

	let status = $state<'pending' | 'error'>('pending');

	onMount(async () => {
		const token = page.url.searchParams.get('token');
		if (!token) {
			status = 'error';
			return;
		}
		try {
			await consumeMagicLink(token);
			await goto(resolve('/'));
		} catch {
			status = 'error';
		}
	});
</script>

<svelte:head>
	<title>Signing in — Project Thor</title>
</svelte:head>

<div class="flex min-h-screen items-center justify-center bg-bg px-4 text-center text-text">
	{#if status === 'pending'}
		<p class="text-text-muted">Signing you in…</p>
	{:else}
		<div class="w-full max-w-sm rounded-2xl border border-border bg-surface p-6">
			<p class="text-sm font-medium text-danger">This link is invalid or has expired.</p>
			<a
				href={resolve('/login')}
				class="mt-4 inline-block text-sm font-medium text-accent hover:underline"
			>
				Request a new login link
			</a>
		</div>
	{/if}
</div>
