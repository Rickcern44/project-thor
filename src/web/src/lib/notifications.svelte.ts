import { getNotifications, markNotificationRead, type NotificationResponse } from './api/client';

export const notificationsState = $state<{ items: NotificationResponse[]; loaded: boolean }>({
	items: [],
	loaded: false
});

export function unreadCount() {
	return notificationsState.items.filter((notification) => !notification.readAt).length;
}

export async function refreshNotifications() {
	notificationsState.items = await getNotifications();
	notificationsState.loaded = true;
}

export async function markRead(id: string) {
	await markNotificationRead(id);
	const notification = notificationsState.items.find((item) => item.id === id);
	if (notification) {
		// eslint-disable-next-line svelte/prefer-svelte-reactivity -- converted to a plain string immediately, never held as a Date
		notification.readAt = new Date().toISOString();
	}
}
