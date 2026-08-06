<script lang="ts">
	import AppShell from '$lib/components/AppShell.svelte';
	import { refreshNotifications } from '$lib/notifications.svelte';
	import type { LayoutProps } from './$types';

	let { children }: LayoutProps = $props();

	refreshNotifications();
</script>

<AppShell>
	{@render children()}
</AppShell>
