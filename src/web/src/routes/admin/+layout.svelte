<script lang="ts">
	import AdminShell from '$lib/components/AdminShell.svelte';
	import type { LayoutProps } from './$types';

	let { children }: LayoutProps = $props();
</script>

<AdminShell>
	{@render children()}
</AdminShell>
