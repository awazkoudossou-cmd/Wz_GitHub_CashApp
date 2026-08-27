import type {
  CashTransferDetail,
  CashTransferFilter,
  CashTransferListItem,
  CreateCashTransferPayload,
  PagedResponse
} from '@/types';
import { apiClient } from './client';

export const cashTransfersApi = {
  list: (filter: CashTransferFilter = {}) =>
    apiClient
      .get<PagedResponse<CashTransferListItem>>('/api/cash-transfers', { params: filter })
      .then((r) => r.data),
  get: (id: number) => apiClient.get<CashTransferDetail>(`/api/cash-transfers/${id}`).then((r) => r.data),
  create: (p: CreateCashTransferPayload) =>
    apiClient.post<CashTransferDetail>('/api/cash-transfers', p).then((r) => r.data),
  complete: (id: number) =>
    apiClient.post<CashTransferDetail>(`/api/cash-transfers/${id}/complete`).then((r) => r.data),
  cancel: (id: number, reason: string) =>
    apiClient
      .post<CashTransferDetail>(`/api/cash-transfers/${id}/cancel`, { reason })
      .then((r) => r.data)
};
