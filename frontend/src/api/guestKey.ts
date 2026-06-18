const GUEST_KEY_STORAGE_KEY = 'aodai_guest_key';

export function getGuestKey(): string {
  try {
    const existing = localStorage.getItem(GUEST_KEY_STORAGE_KEY);
    if (existing) return existing;
    const next = crypto.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`;
    localStorage.setItem(GUEST_KEY_STORAGE_KEY, next);
    return next;
  } catch {
    return 'anonymous';
  }
}
