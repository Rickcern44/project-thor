export type Theme = 'light' | 'dark';

function readInitialTheme(): Theme {
	if (typeof document === 'undefined') return 'dark';
	return document.documentElement.getAttribute('data-theme') === 'light' ? 'light' : 'dark';
}

export const themeState = $state<{ value: Theme }>({ value: readInitialTheme() });

export function setTheme(next: Theme) {
	themeState.value = next;
	document.documentElement.setAttribute('data-theme', next);
	localStorage.setItem('theme', next);
}

export function toggleTheme() {
	setTheme(themeState.value === 'dark' ? 'light' : 'dark');
}
