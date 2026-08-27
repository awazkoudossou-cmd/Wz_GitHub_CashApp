import type { AttachmentDto } from '@/types';
import { apiClient } from './client';

export const attachmentsApi = {
  list: (entityType: string, entityId: number) =>
    apiClient
      .get<AttachmentDto[]>(`/api/attachments/${entityType}/${entityId}`)
      .then((r) => r.data),

  upload: async (entityType: string, entityId: number, file: File, description?: string) => {
    const form = new FormData();
    form.append('entityType', entityType);
    form.append('entityId', String(entityId));
    form.append('file', file);
    if (description) form.append('description', description);
    const r = await apiClient.post<{ attachment: AttachmentDto }>(
      '/api/attachments/upload',
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    );
    return r.data.attachment;
  },

  downloadUrl: (id: number) => `/api/attachments/${id}/download`,

  download: async (id: number, fileName: string) => {
    const r = await apiClient.get(`/api/attachments/${id}/download`, { responseType: 'blob' });
    const blob = r.data as Blob;
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  },

  delete: (id: number) => apiClient.delete(`/api/attachments/${id}`).then((r) => r.data)
};
