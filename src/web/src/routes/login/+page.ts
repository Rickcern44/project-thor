// Purely a client-driven form (no server action) — avoids a pre-hydration window
// where the submit button is painted but its handler isn't attached yet.
export const ssr = false;
