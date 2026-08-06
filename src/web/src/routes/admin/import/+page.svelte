<script lang="ts">
	import DropZone from '$lib/components/DropZone.svelte';
	import {
		ApiError,
		getFlaggedRows,
		importRoster,
		issueInvite,
		parseFlaggedRowData,
		resolveFlaggedRow,
		type ImportRosterResult
	} from '$lib/api/client';
	import { formatCurrency, formatDateOnly } from '$lib/format';

	type Step = 'import' | 'review';

	interface ReviewRow {
		id: string;
		selected: boolean;
		name: string;
		email: string;
		phone: string;
		attendedDates: string[];
		totalDue: number;
		amountPaid: number;
		status: 'idle' | 'submitting' | 'success' | 'error';
		errorMessage: string;
	}

	let step = $state<Step>('import');

	// Import step
	let file = $state<File | null>(null);
	let seasonYear = $state(new Date().getFullYear());
	let importStatus = $state<'idle' | 'uploading' | 'error'>('idle');
	let importErrorMessage = $state('');
	let importResult = $state<ImportRosterResult | null>(null);

	function handleFileSelect(selected: File) {
		file = selected;
		importResult = null;
		importStatus = 'idle';
	}

	async function handleImportSubmit(event: SubmitEvent) {
		event.preventDefault();
		if (!file) {
			return;
		}

		importStatus = 'uploading';
		importErrorMessage = '';
		try {
			importResult = await importRoster(file, seasonYear);
			importStatus = 'idle';
		} catch (err) {
			importErrorMessage =
				err instanceof ApiError ? err.message : 'Something went wrong. Please try again.';
			importStatus = 'error';
		}
	}

	// Review step — a standing queue of every pending flagged row, not scoped to this session's
	// import, so it's reachable directly too (D4).
	let reviewRows = $state<ReviewRow[]>([]);
	let reviewLoading = $state(false);
	let submitting = $state(false);

	async function goToReview() {
		step = 'review';
		reviewLoading = true;
		const rows = await getFlaggedRows();
		reviewRows = rows.map((row) => {
			const parsed = parseFlaggedRowData(row.rawData);
			return {
				id: row.id,
				// D9: unchecked by default — an Admin opts in to who gets processed this session;
				// unselected rows just stay in this same pending queue for next time.
				selected: false,
				name: parsed.Name,
				email: '',
				phone: '',
				attendedDates: parsed.AttendedDates,
				totalDue: parsed.TotalDue,
				amountPaid: parsed.AmountPaid,
				status: 'idle' as const,
				errorMessage: ''
			};
		});
		reviewLoading = false;
	}

	const allSelected = $derived(reviewRows.length > 0 && reviewRows.every((row) => row.selected));
	const hasSubmittableRows = $derived(
		reviewRows.some((row) => row.selected && row.status !== 'success')
	);

	function toggleSelectAll(checked: boolean) {
		for (const row of reviewRows) {
			row.selected = checked;
		}
	}

	function isRowValid(row: ReviewRow) {
		return row.name.trim().length > 0 && row.email.trim().length > 0 && row.phone.trim().length > 0;
	}

	// D7/D8: sequential, one row at a time — each row's outcome is independent, so one failure
	// (or one row missing email/phone) never blocks the rest.
	async function handleSubmitAll() {
		submitting = true;
		for (const row of reviewRows) {
			// D9: only selected rows are submitted — everything else is left untouched.
			if (!row.selected || row.status === 'success') {
				continue;
			}

			if (!isRowValid(row)) {
				row.status = 'error';
				row.errorMessage = 'Name, email, and phone are required.';
				continue;
			}

			row.status = 'submitting';
			row.errorMessage = '';
			try {
				const resolved = await resolveFlaggedRow(row.id, row.name, row.email, row.phone);
				await issueInvite(resolved.rosterRecordId);
				row.status = 'success';
			} catch (err) {
				row.status = 'error';
				row.errorMessage = err instanceof ApiError ? err.message : 'Something went wrong.';
			}
		}
		submitting = false;
	}
</script>

<svelte:head>
	<title>Import Roster — Project Thor</title>
</svelte:head>

<h1 class="text-2xl font-semibold">Import Roster</h1>

<div class="mt-2 flex items-center gap-2 text-sm font-medium">
	<button
		type="button"
		onclick={() => (step = 'import')}
		class="rounded-full px-3 py-1 {step === 'import'
			? 'bg-accent text-accent-fg'
			: 'text-text-muted'}"
	>
		1. Import
	</button>
	<span class="text-text-subtle">→</span>
	<button
		type="button"
		onclick={goToReview}
		class="rounded-full px-3 py-1 {step === 'review'
			? 'bg-accent text-accent-fg'
			: 'text-text-muted'}"
	>
		2. Review{#if reviewRows.length > 0}&nbsp;({reviewRows.length}){/if}
	</button>
</div>

{#if step === 'import'}
	<form class="mt-6 flex max-w-lg flex-col gap-4" onsubmit={handleImportSubmit}>
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

		{#if importErrorMessage}
			<p class="text-sm text-danger">{importErrorMessage}</p>
		{/if}

		<button
			type="submit"
			disabled={!file || importStatus === 'uploading'}
			class="self-start rounded-lg bg-accent px-4 py-2 text-sm font-medium text-accent-fg transition-colors hover:bg-accent-strong disabled:opacity-50"
		>
			{importStatus === 'uploading' ? 'Importing…' : 'Import'}
		</button>
	</form>

	{#if importResult}
		<div class="mt-6 max-w-lg rounded-2xl border border-border bg-surface p-6">
			<p class="text-sm font-medium text-text">Import complete</p>
			<dl class="mt-4 grid grid-cols-3 gap-4 font-mono text-sm">
				<div>
					<dt class="text-text-muted">Games created</dt>
					<dd class="mt-1 text-xl text-text">{importResult.gamesCreated}</dd>
				</div>
				<div>
					<dt class="text-text-muted">Rows flagged</dt>
					<dd class="mt-1 text-xl text-text">{importResult.rowsFlagged}</dd>
				</div>
				<div>
					<dt class="text-text-muted">Duplicates skipped</dt>
					<dd class="mt-1 text-xl text-text">{importResult.rowsSkippedAsDuplicate}</dd>
				</div>
			</dl>
			<button
				type="button"
				onclick={goToReview}
				class="mt-4 text-sm font-medium text-accent hover:underline"
			>
				Continue to review →
			</button>
		</div>
	{/if}
{:else if reviewLoading}
	<p class="mt-6 text-text-muted">Loading…</p>
{:else if reviewRows.length === 0}
	<p class="mt-6 text-text-muted">No rows are pending review.</p>
{:else}
	<div class="mt-6 overflow-x-auto rounded-2xl border border-border">
		<table class="w-full text-sm">
			<thead class="bg-surface-raised text-left text-text-muted">
				<tr>
					<th class="p-3">
						<input
							type="checkbox"
							checked={allSelected}
							aria-label="Select all"
							onchange={(event) => toggleSelectAll(event.currentTarget.checked)}
						/>
					</th>
					<th class="p-3">Name</th>
					<th class="p-3">Email</th>
					<th class="p-3">Phone</th>
					<th class="p-3">Attended dates</th>
					<th class="p-3">Total due</th>
					<th class="p-3">Amount paid</th>
					<th class="p-3">Status</th>
				</tr>
			</thead>
			<tbody>
				{#each reviewRows as row (row.id)}
					<tr class="border-t border-border bg-surface align-top">
						<td class="p-3">
							<input
								type="checkbox"
								bind:checked={row.selected}
								disabled={row.status === 'success'}
								aria-label="Select {row.name}"
							/>
						</td>
						<td class="p-3">
							<input
								type="text"
								bind:value={row.name}
								class="w-full rounded-lg border border-border bg-bg px-2 py-1 text-sm text-text outline-none focus:border-accent"
							/>
						</td>
						<td class="p-3">
							<input
								type="email"
								bind:value={row.email}
								placeholder="email@example.com"
								class="w-full rounded-lg border border-border bg-bg px-2 py-1 text-sm text-text outline-none focus:border-accent"
							/>
						</td>
						<td class="p-3">
							<input
								type="tel"
								bind:value={row.phone}
								placeholder="555-0100"
								class="w-full rounded-lg border border-border bg-bg px-2 py-1 text-sm text-text outline-none focus:border-accent"
							/>
						</td>
						<td class="p-3 font-mono text-xs text-text-muted">
							{row.attendedDates.map(formatDateOnly).join(', ')}
						</td>
						<td class="p-3 font-mono">{formatCurrency(row.totalDue)}</td>
						<td class="p-3 font-mono">{formatCurrency(row.amountPaid)}</td>
						<td class="p-3">
							{#if row.status === 'success'}
								<span class="text-success">Invited</span>
							{:else if row.status === 'submitting'}
								<span class="text-text-muted">Submitting…</span>
							{:else if row.status === 'error'}
								<span class="text-danger">{row.errorMessage}</span>
							{/if}
						</td>
					</tr>
				{/each}
			</tbody>
		</table>
	</div>

	<button
		type="button"
		onclick={handleSubmitAll}
		disabled={submitting || !hasSubmittableRows}
		class="mt-4 rounded-lg bg-accent px-4 py-2 text-sm font-medium text-accent-fg transition-colors hover:bg-accent-strong disabled:opacity-50"
	>
		{submitting ? 'Submitting…' : 'Submit'}
	</button>
{/if}
