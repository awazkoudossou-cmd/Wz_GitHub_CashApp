import type {
  CreateReconciliationBatchPayload,
  PagedResponse,
  ReconcileItemsPayload,
  ReconciliationBatchDetail,
  ReconciliationBatchListItem
} from '@/types';
import { apiClient } from './client';

export const reconciliationApi = {
  list: (page = 1, pageSize = 50) =>
    apiClient
      .get<PagedResponse<ReconciliationBatchListItem>>('/api/reconciliation-batches', {
        params: { page, pageSize }
      })
      .then((r) => r.data),
  get: (id: number) =>
    apiClient
      .get<ReconciliationBatchDetail>(`/api/reconciliation-batches/${id}`)
      .then((r) => r.data),
  create: (p: CreateReconciliationBatchPayload) =>
    apiClient
      .post<ReconciliationBatchDetail>('/api/reconciliation-batches', p)
      .then((r) => r.data),
  match: (id: number, p: ReconcileItemsPayload) =>
    apiClient
      .post<ReconciliationBatchDetail>(`/api/reconciliation-batches/${id}/match`, p)
      .then((r) => r.data)
};
