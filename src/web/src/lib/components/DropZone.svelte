<script lang="ts">
	import { FileSpreadsheet, Upload } from '@lucide/svelte';

	let {
		onSelect,
		selectedFileName = null
	}: {
		onSelect: (file: File) => void;
		selectedFileName?: string | null;
	} = $props();

	let error = $state('');
	let isDragging = $state(false);

	function handleFiles(files: FileList | null) {
		const file = files?.[0];
		if (!file) {
			return;
		}

		if (!file.name.toLowerCase().endsWith('.csv')) {
			error = `"${file.name}" is not a CSV file. Please select a .csv file.`;
			return;
		}

		error = '';
		onSelect(file);
	}

	function handleDrop(event: DragEvent) {
		event.preventDefault();
		isDragging = false;
		handleFiles(event.dataTransfer?.files ?? null);
	}

	function handleInputChange(event: Event) {
		handleFiles((event.target as HTMLInputElement).files);
	}
</script>

<div>
	<label
		for="csv-file-input"
		ondragover={(event) => {
			event.preventDefault();
			isDragging = true;
		}}
		ondragleave={() => (isDragging = false)}
		ondrop={handleDrop}
		class="flex cursor-pointer flex-col items-center gap-2 rounded-2xl border-2 border-dashed px-6 py-12 text-center transition-colors {isDragging
			? 'border-accent bg-accent/10'
			: 'border-border bg-surface hover:border-accent'}"
	>
		{#if selectedFileName}
			<FileSpreadsheet class="size-8 text-accent" aria-hidden="true" />
			<p class="text-sm font-medium text-text">{selectedFileName}</p>
			<p class="text-xs text-text-muted">Drop or choose a different file to replace it</p>
		{:else}
			<Upload class="size-8 text-text-muted" aria-hidden="true" />
			<p class="text-sm font-medium text-text">Drop the roster CSV here</p>
			<p class="text-xs text-text-muted">or click to choose a file</p>
		{/if}
	</label>
	<input
		id="csv-file-input"
		type="file"
		accept=".csv"
		class="sr-only"
		onchange={handleInputChange}
	/>

	{#if error}
		<p class="mt-2 text-sm text-danger">{error}</p>
	{/if}
</div>
