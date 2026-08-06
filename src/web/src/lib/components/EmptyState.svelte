<script lang="ts">
	let { title, description }: { title: string; description?: string } = $props();
</script>

<div
	class="flex flex-col items-center justify-center rounded-2xl border border-dashed border-border px-6 py-16 text-center"
>
	<p class="text-base font-semibold text-text">{title}</p>
	{#if description}
		<p class="mt-2 max-w-sm text-sm text-text-muted">{description}</p>
	{/if}
</div>
