import type { AuditLogDetail, AuditLogFilter, AuditLogListItem, PagedResponse } from '@/types';
import { apiClient } from './client';

function downloadBlob(response: { headers: Record<string, unknown>; data: unknown }, fallbackName: string) {
  const cd = response.headers['content-disposition'] as string | undefined;
  const match = cd?.match(/filename="?([^";]+)"?/i);
  const fileName = match?.[1] ?? fallbackName;
  const blob = response.data as Blob;
  const objectUrl = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = objectUrl;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(objectUrl);
}

export const auditLogsApi = {
  list: (filter: AuditLogFilter = {}) =>
    apiClient
      .get<PagedResponse<AuditLogListItem>>('/api/audit-logs', { params: filter })
      .then((r) => r.data),
  get: (id: number) => apiClient.get<AuditLogDetail>(`/api/audit-logs/${id}`).then((r) => r.data),
  export: async (filter: AuditLogFilter = {}) => {
    const r = await apiClient.get('/api/audit-logs/export', { params: filter, responseType: 'blob' });
    downloadBlob(r, 'audit_log.xlsx');
  }
};
