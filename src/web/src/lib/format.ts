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

// A bare calendar date (e.g. from a .NET DateOnly) has no timezone — format in UTC so a
// "2026-01-08" value doesn't shift to the 7th for anyone west of UTC.
const dateOnlyFormatter = new Intl.DateTimeFormat(undefined, {
	year: 'numeric',
	month: 'short',
	day: 'numeric',
	timeZone: 'UTC'
});

export function formatGameDateTime(iso: string) {
	const date = new Date(iso);
	return `${dateFormatter.format(date)} · ${timeFormatter.format(date)}`;
}

export function formatCurrency(amount: number) {
	return currencyFormatter.format(amount);
}

export function formatDateOnly(isoDate: string) {
	return dateOnlyFormatter.format(new Date(isoDate));
}
