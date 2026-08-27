import type { BackupListItem } from '@/types';
import { apiClient } from './client';

export const backupsApi = {
  list: () => apiClient.get<BackupListItem[]>('/api/backups').then((r) => r.data),
  create: () => apiClient.post('/api/backups/create').then((r) => r.data),
  restore: (fileName: string) =>
    apiClient.post('/api/backups/restore', { fileName }).then((r) => r.data)
};
