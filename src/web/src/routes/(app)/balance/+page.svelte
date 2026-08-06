<script lang="ts">
	import { getBalance, type BalanceResponse } from '$lib/api/client';
	import { formatCurrency } from '$lib/format';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	let balance = $state<BalanceResponse | null>(null);
	let loading = $state(true);

	async function load() {
		loading = true;
		balance = await getBalance(data.user.id);
		loading = false;
	}

	load();

	const isCredit = $derived((balance?.balance ?? 0) < 0);
</script>

<svelte:head>
	<title>Balance — Project Thor</title>
</svelte:head>

<h1 class="text-2xl font-semibold">Balance</h1>

<div class="mt-6">
	{#if loading || !balance}
		<p class="text-text-muted">Loading…</p>
	{:else}
		<div class="rounded-2xl border border-border bg-surface p-6">
			<p class="text-sm text-text-muted">{isCredit ? 'Credit' : 'Outstanding balance'}</p>
			<p class="mt-2 font-mono text-3xl font-semibold {isCredit ? 'text-success' : 'text-text'}">
				{formatCurrency(Math.abs(balance.balance))}
			</p>
		</div>
	{/if}
</div>
