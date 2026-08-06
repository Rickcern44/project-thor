<script lang="ts">
	import { CheckCircle2, Clock } from '@lucide/svelte';
	import type { GameResponse, SignUpResponse } from '$lib/api/client';
	import { formatCurrency, formatGameDateTime } from '$lib/format';

	let {
		game,
		rosterCount,
		mySignUp,
		busy = false,
		onSignUp,
		onCancel
	}: {
		game: GameResponse;
		rosterCount: number;
		mySignUp: SignUpResponse | null;
		busy?: boolean;
		onSignUp: () => void;
		onCancel: () => void;
	} = $props();

	const statusLabel = $derived(
		{ Open: 'Open for sign-up', Closed: 'Not open yet', Past: 'Completed' }[game.state]
	);
	const isFull = $derived(rosterCount >= game.capacity);
</script>

<div class="rounded-2xl border border-border bg-surface p-6">
	<div class="flex items-center justify-between">
		<span
			class="inline-flex items-center gap-2 rounded-full px-3 py-1 text-xs font-medium {game.state ===
			'Open'
				? 'bg-accent/15 text-accent'
				: 'bg-surface-raised text-text-muted'}"
		>
			{#if game.state === 'Open'}
				<span class="size-1.5 animate-pulse rounded-full bg-accent" aria-hidden="true"></span>
			{/if}
			{statusLabel}
		</span>
		<span class="font-mono text-sm text-text-muted">{formatCurrency(game.fee)}</span>
	</div>

	<p class="mt-4 text-xl font-semibold">{formatGameDateTime(game.startsAt)}</p>

	<p class="mt-1 font-mono text-sm text-text-muted">
		{rosterCount} / {game.capacity} rostered
	</p>

	{#if mySignUp?.status === 'Rostered'}
		<p
			class="mt-4 inline-flex items-center gap-2 rounded-lg bg-success-soft px-3 py-2 text-sm font-medium text-success"
		>
			<CheckCircle2 class="size-4" aria-hidden="true" />
			You're rostered for this game
		</p>
	{:else if mySignUp?.status === 'Waitlisted'}
		<p
			class="mt-4 inline-flex items-center gap-2 rounded-lg bg-warning-soft px-3 py-2 text-sm font-medium text-warning"
		>
			<Clock class="size-4" aria-hidden="true" />
			Waitlisted — position #{mySignUp.waitlistPosition}
		</p>
	{/if}

	<div class="mt-6 flex gap-3">
		{#if mySignUp}
			<button
				type="button"
				onclick={onCancel}
				disabled={busy}
				class="rounded-lg border border-border px-4 py-2 text-sm font-medium text-text transition-colors hover:border-danger hover:text-danger disabled:opacity-50"
			>
				Cancel sign-up
			</button>
		{:else}
			<button
				type="button"
				onclick={onSignUp}
				disabled={busy || game.state !== 'Open'}
				class="rounded-lg bg-accent px-4 py-2 text-sm font-medium text-accent-fg transition-colors hover:bg-accent-strong disabled:opacity-50"
			>
				{isFull ? 'Join waitlist' : 'Sign up'}
			</button>
		{/if}
	</div>
</div>
