<script lang="ts">
	import GameCard from '$lib/components/GameCard.svelte';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import {
		ApiError,
		cancelSignUp,
		getGameRoster,
		getLiveGame,
		signUpForGame,
		type GameResponse,
		type SignUpResponse
	} from '$lib/api/client';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	let game = $state<GameResponse | null>(null);
	let roster = $state<SignUpResponse[]>([]);
	let loading = $state(true);
	let actionPending = $state(false);
	let errorMessage = $state('');

	const mySignUp = $derived(roster.find((s) => s.playerUserId === data.user.id) ?? null);
	const rosterCount = $derived(roster.filter((s) => s.status === 'Rostered').length);

	async function load() {
		loading = true;
		const liveGame = await getLiveGame();
		game = liveGame ?? null;
		roster = game ? await getGameRoster(game.id) : [];
		loading = false;
	}

	load();

	async function withAction(action: () => Promise<unknown>) {
		actionPending = true;
		errorMessage = '';
		try {
			await action();
			await load();
		} catch (err) {
			errorMessage =
				err instanceof ApiError ? err.message : 'Something went wrong. Please try again.';
		} finally {
			actionPending = false;
		}
	}

	function handleSignUp() {
		if (game) withAction(() => signUpForGame(game!.id));
	}

	function handleCancel() {
		if (game) withAction(() => cancelSignUp(game!.id));
	}
</script>

<svelte:head>
	<title>Live Game — Project Thor</title>
</svelte:head>

<h1 class="text-2xl font-semibold">Live Game</h1>

<div class="mt-6">
	{#if loading}
		<p class="text-text-muted">Loading…</p>
	{:else if !game}
		<EmptyState
			title="No upcoming game scheduled"
			description="Check back soon — new games are announced here as they're scheduled."
		/>
	{:else}
		<GameCard
			{game}
			{rosterCount}
			{mySignUp}
			busy={actionPending}
			onSignUp={handleSignUp}
			onCancel={handleCancel}
		/>
		{#if errorMessage}
			<p class="mt-4 text-sm text-danger">{errorMessage}</p>
		{/if}
	{/if}
</div>
