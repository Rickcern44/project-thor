<script lang="ts">
	import { Moon, Sun } from '@lucide/svelte';
	import { themeState, toggleTheme } from '$lib/theme.svelte';
</script>

<button
	type="button"
	onclick={toggleTheme}
	class="flex items-center justify-center rounded-full border border-border p-2 text-text-muted transition-colors hover:border-accent hover:text-accent"
	aria-label={themeState.value === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
>
	{#if themeState.value === 'dark'}
		<Sun class="size-5" aria-hidden="true" />
	{:else}
		<Moon class="size-5" aria-hidden="true" />
	{/if}
</button>
