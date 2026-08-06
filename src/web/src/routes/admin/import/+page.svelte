<script lang="ts">
	import DropZone from '$lib/components/DropZone.svelte';
	import { ApiError, importRoster, type ImportRosterResult } from '$lib/api/client';

	let file = $state<File | null>(null);
	let seasonYear = $state(new Date().getFullYear());
	let status = $state<'idle' | 'uploading' | 'error'>('idle');
	let errorMessage = $state('');
	let result = $state<ImportRosterResult | null>(null);

	function handleFileSelect(selected: File) {
		file = selected;
		result = null;
		status = 'idle';
	}

	async function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		if (!file) {
			return;
		}

		status = 'uploading';
		errorMessage = '';
		try {
			result = await importRoster(file, seasonYear);
			status = 'idle';
		} catch (err) {
			errorMessage =
				err instanceof ApiError ? err.message : 'Something went wrong. Please try again.';
			status = 'error';
		}
	}
</script>

<svelte:head>
	<title>Import Roster — Project Thor</title>
</svelte:head>

<h1 class="text-2xl font-semibold">Import Roster</h1>
<p class="mt-1 text-sm text-text-muted">Step 1 of 3 — import the season's roster spreadsheet.</p>

<form class="mt-6 flex max-w-lg flex-col gap-4" onsubmit={handleSubmit}>
	<DropZone onSelect={handleFileSelect} selectedFileName={file?.name} />

	<label class="flex flex-col gap-1.5">
		<span class="text-sm font-medium text-text-muted">Season year</span>
		<input
			type="number"
			bind:value={seasonYear}
			required
			class="w-32 rounded-lg border border-border bg-bg px-3 py-2 text-sm text-text outline-none focus:border-accent"
		/>
	</label>

	{#if errorMessage}
		<p class="text-sm text-danger">{errorMessage}</p>
	{/if}

	<button
		type="submit"
		disabled={!file || status === 'uploading'}
		class="self-start rounded-lg bg-accent px-4 py-2 text-sm font-medium text-accent-fg transition-colors hover:bg-accent-strong disabled:opacity-50"
	>
		{status === 'uploading' ? 'Importing…' : 'Import'}
	</button>
</form>

{#if result}
	<div class="mt-6 max-w-lg rounded-2xl border border-border bg-surface p-6">
		<p class="text-sm font-medium text-text">Import complete</p>
		<dl class="mt-4 grid grid-cols-3 gap-4 font-mono text-sm">
			<div>
				<dt class="text-text-muted">Games created</dt>
				<dd class="mt-1 text-xl text-text">{result.gamesCreated}</dd>
			</div>
			<div>
				<dt class="text-text-muted">Rows flagged</dt>
				<dd class="mt-1 text-xl text-text">{result.rowsFlagged}</dd>
			</div>
			<div>
				<dt class="text-text-muted">Duplicates skipped</dt>
				<dd class="mt-1 text-xl text-text">{result.rowsSkippedAsDuplicate}</dd>
			</div>
		</dl>
	</div>
{/if}
