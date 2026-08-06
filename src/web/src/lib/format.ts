const dateFormatter = new Intl.DateTimeFormat(undefined, {
	weekday: 'short',
	month: 'short',
	day: 'numeric'
});

const timeFormatter = new Intl.DateTimeFormat(undefined, {
	hour: 'numeric',
	minute: '2-digit'
});

const currencyFormatter = new Intl.NumberFormat(undefined, {
	style: 'currency',
	currency: 'USD'
});

export function formatGameDateTime(iso: string) {
	const date = new Date(iso);
	return `${dateFormatter.format(date)} · ${timeFormatter.format(date)}`;
}

export function formatCurrency(amount: number) {
	return currencyFormatter.format(amount);
}
