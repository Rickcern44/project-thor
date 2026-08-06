<script lang="ts">
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { Bell, Home, Shield, User, Wallet } from '@lucide/svelte';
	import ThemeToggle from './ThemeToggle.svelte';
	import { unreadCount } from '$lib/notifications.svelte';

	let { children, role }: { children: import('svelte').Snippet; role?: string } = $props();

	const playerNavItems = [
		{ href: '/', label: 'Live Game', icon: Home },
		{ href: '/balance', label: 'Balance', icon: Wallet },
		{ href: '/notifications', label: 'Notifications', icon: Bell },
		{ href: '/profile', label: 'Profile', icon: User }
	] as const;

	const adminNavItem = { href: '/admin/import', label: 'Admin', icon: Shield } as const;

	// D3 (add-admin-import-ui): one conditional entry, shown only for Admins, rather than a
	// parallel admin nav system.
	const navItems = $derived(role === 'Admin' ? [...playerNavItems, adminNavItem] : playerNavItems);

	function isActive(href: string) {
		return href === '/' ? page.url.pathname === '/' : page.url.pathname.startsWith(href);
	}
</script>

<!--
	A size container can't be queried by the element that establishes it, so `@container` lives on
	this outer wrapper while `@shell:*` variants apply to the inner flex layout below it — putting
	both on one element silently drops the query (caught visually: the shell never switched to a
	row, even though @shell:flex/@shell:hidden on the children worked fine).
-->
<div class="@container">
	<div class="flex h-screen flex-col bg-bg text-text @shell:flex-row">
		<aside
			class="hidden w-60 shrink-0 flex-col gap-6 border-r border-border bg-surface p-6 @shell:flex"
		>
			<span class="text-lg font-semibold tracking-tight">Project Thor</span>
			<nav class="flex flex-1 flex-col gap-1">
				{#each navItems as item (item.href)}
					<a
						href={resolve(item.href)}
						aria-current={isActive(item.href) ? 'page' : undefined}
						class="flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors {isActive(
							item.href
						)
							? 'bg-surface-raised text-accent'
							: 'text-text-muted hover:bg-surface-raised hover:text-text'}"
					>
						<item.icon class="size-5" aria-hidden="true" />
						<span>{item.label}</span>
						{#if item.href === '/notifications' && unreadCount() > 0}
							<span
								class="ml-auto rounded-full bg-accent px-2 py-0.5 text-xs font-medium text-accent-fg"
							>
								{unreadCount()}
							</span>
						{/if}
					</a>
				{/each}
			</nav>
			<ThemeToggle />
		</aside>

		<header
			class="flex items-center justify-between border-b border-border bg-surface px-4 py-3 @shell:hidden"
		>
			<span class="text-lg font-semibold tracking-tight">Project Thor</span>
			<ThemeToggle />
		</header>

		<div class="flex flex-1 flex-col overflow-hidden">
			<main class="flex-1 overflow-y-auto p-4 pb-24 @shell:p-8 @shell:pb-8">
				{@render children()}
			</main>
		</div>

		<nav
			aria-label="Primary"
			class="fixed inset-x-0 bottom-0 flex border-t border-border bg-surface @shell:hidden"
		>
			{#each navItems as item (item.href)}
				<a
					href={resolve(item.href)}
					aria-current={isActive(item.href) ? 'page' : undefined}
					class="relative flex flex-1 flex-col items-center gap-1 py-2 text-xs font-medium {isActive(
						item.href
					)
						? 'text-accent'
						: 'text-text-muted'}"
				>
					<item.icon class="size-5" aria-hidden="true" />
					<span>{item.label}</span>
					{#if item.href === '/notifications' && unreadCount() > 0}
						<span class="absolute top-1 right-5 size-2 rounded-full bg-accent" aria-hidden="true"
						></span>
					{/if}
				</a>
			{/each}
		</nav>
	</div>
</div>
