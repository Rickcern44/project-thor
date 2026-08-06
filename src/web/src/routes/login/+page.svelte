<script lang="ts">
	import { requestLoginLink } from '$lib/api/client';

	let email = $state('');
	let status = $state<'idle' | 'submitting' | 'sent' | 'error'>('idle');

	async function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		status = 'submitting';
		try {
			await requestLoginLink(email);
			status = 'sent';
		} catch {
			status = 'error';
		}
	}
</script>

<svelte:head>
	<title>Log in — Project Thor</title>
</svelte:head>

<div class="flex min-h-screen items-center justify-center bg-bg px-4 text-text">
	<div class="w-full max-w-sm rounded-2xl border border-border bg-surface p-6">
		<h1 class="text-xl font-semibold">Project Thor</h1>
		<p class="mt-1 text-sm text-text-muted">Sign in with your email to get a login link.</p>

		{#if status === 'sent'}
			<p class="mt-6 rounded-lg bg-success-soft px-4 py-3 text-sm font-medium text-success">
				Check your email — we sent a link to sign in.
			</p>
		{:else}
			<form class="mt-6 flex flex-col gap-3" onsubmit={handleSubmit}>
				<label class="flex flex-col gap-1.5">
					<span class="text-sm font-medium text-text-muted">Email</span>
					<input
						type="email"
						name="email"
						required
						autocomplete="email"
						bind:value={email}
						class="rounded-lg border border-border bg-bg px-3 py-2 text-sm text-text outline-none focus:border-accent"
					/>
				</label>

				{#if status === 'error'}
					<p class="text-sm text-danger">Something went wrong. Please try again.</p>
				{/if}

				<button
					type="submit"
					disabled={status === 'submitting'}
					class="mt-2 rounded-lg bg-accent px-4 py-2 text-sm font-medium text-accent-fg transition-colors hover:bg-accent-strong disabled:opacity-50"
				>
					{status === 'submitting' ? 'Sending…' : 'Send login link'}
				</button>
			</form>
		{/if}
	</div>
</div>
