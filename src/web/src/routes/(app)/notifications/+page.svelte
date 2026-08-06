<script lang="ts">
	import EmptyState from '$lib/components/EmptyState.svelte';
	import { markRead, notificationsState, refreshNotifications } from '$lib/notifications.svelte';

	let loading = $state(true);

	async function load() {
		loading = true;
		await refreshNotifications();
		loading = false;
	}

	load();

	const dateTimeFormatter = new Intl.DateTimeFormat(undefined, {
		month: 'short',
		day: 'numeric',
		hour: 'numeric',
		minute: '2-digit'
	});
</script>

<svelte:head>
	<title>Notifications — Project Thor</title>
</svelte:head>

<h1 class="text-2xl font-semibold">Notifications</h1>

<div class="mt-6">
	{#if loading}
		<p class="text-text-muted">Loading…</p>
	{:else if notificationsState.items.length === 0}
		<EmptyState
			title="No notifications yet"
			description="Waitlist promotions and new game openings will show up here."
		/>
	{:else}
		<ul class="flex flex-col gap-2">
			{#each notificationsState.items as notification (notification.id)}
				<li>
					<button
						type="button"
						onclick={() => !notification.readAt && markRead(notification.id)}
						class="flex w-full flex-col items-start gap-1 rounded-xl border border-border p-4 text-left transition-colors {notification.readAt
							? 'bg-surface text-text-muted'
							: 'bg-surface-raised text-text'}"
					>
						<span class="flex w-full items-center justify-between gap-3">
							<span class="text-sm font-medium">{notification.message}</span>
							{#if !notification.readAt}
								<span class="size-2 shrink-0 rounded-full bg-accent" aria-label="Unread"></span>
							{/if}
						</span>
						<span class="font-mono text-xs text-text-subtle">
							{dateTimeFormatter.format(new Date(notification.createdAt))}
						</span>
					</button>
				</li>
			{/each}
		</ul>
	{/if}
</div>
