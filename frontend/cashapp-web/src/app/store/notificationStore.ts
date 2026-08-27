import { create } from 'zustand';

export type NotificationSeverity = 'success' | 'info' | 'warning' | 'error';

export interface Notification {
  id: string;
  message: string;
  severity: NotificationSeverity;
  durationMs?: number;
}

interface NotificationState {
  notifications: Notification[];
  notify: (message: string, severity?: NotificationSeverity, durationMs?: number) => void;
  notifySuccess: (message: string, durationMs?: number) => void;
  notifyError: (message: string, durationMs?: number) => void;
  notifyInfo: (message: string, durationMs?: number) => void;
  notifyWarning: (message: string, durationMs?: number) => void;
  dismiss: (id: string) => void;
}

let counter = 0;
const nextId = () => `n-${Date.now()}-${counter++}`;

export const useNotificationStore = create<NotificationState>((set) => ({
  notifications: [],
  notify: (message, severity = 'info', durationMs = 4000) =>
    set((s) => ({ notifications: [...s.notifications, { id: nextId(), message, severity, durationMs }] })),
  notifySuccess: (message, durationMs) =>
    set((s) => ({ notifications: [...s.notifications, { id: nextId(), message, severity: 'success', durationMs: durationMs ?? 4000 }] })),
  notifyError: (message, durationMs) =>
    set((s) => ({ notifications: [...s.notifications, { id: nextId(), message, severity: 'error', durationMs: durationMs ?? 6000 }] })),
  notifyInfo: (message, durationMs) =>
    set((s) => ({ notifications: [...s.notifications, { id: nextId(), message, severity: 'info', durationMs: durationMs ?? 4000 }] })),
  notifyWarning: (message, durationMs) =>
    set((s) => ({ notifications: [...s.notifications, { id: nextId(), message, severity: 'warning', durationMs: durationMs ?? 5000 }] })),
  dismiss: (id) => set((s) => ({ notifications: s.notifications.filter((n) => n.id !== id) }))
}));
