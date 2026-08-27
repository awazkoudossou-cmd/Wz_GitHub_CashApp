import type {
  ImportBatchDetail,
  ImportBatchListItem,
  ImportBatchType,
  ImportPreview,
  PagedResponse
} from '@/types';
import { apiClient } from './client';

export const importsApi = {
  list: (page = 1, pageSize = 50) =>
    apiClient
      .get<PagedResponse<ImportBatchListItem>>('/api/import-batches', { params: { page, pageSize } })
      .then((r) => r.data),
  get: (id: number) =>
    apiClient.get<ImportBatchDetail>(`/api/import-batches/${id}`).then((r) => r.data),
  upload: async (batchType: ImportBatchType, file: File, cashRegisterId?: number) => {
    const form = new FormData();
    form.append('batchType', batchType);
    form.append('file', file);
    if (cashRegisterId) form.append('cashRegisterId', String(cashRegisterId));
    const r = await apiClient.post<ImportBatchDetail>('/api/import-batches/upload', form, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    return r.data;
  },
  preview: (id: number) =>
    apiClient.post<ImportPreview>(`/api/import-batches/${id}/preview`).then((r) => r.data),
  confirm: (id: number, allowPartialSuccess: boolean) =>
    apiClient
      .post<ImportBatchDetail>(`/api/import-batches/${id}/confirm`, { allowPartialSuccess })
      .then((r) => r.data)
};
