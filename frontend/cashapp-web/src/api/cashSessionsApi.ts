import type {
  CashSessionDetail,
  CashSessionListItem,
  CloseCashSessionPayload,
  OpenCashSessionPayload
} from '@/types';
import { apiClient } from './client';

export const cashSessionsApi = {
  list: (cashRegisterId?: number) =>
    apiClient
      .get<CashSessionListItem[]>('/api/cash-sessions', { params: { cashRegisterId } })
      .then((r) => r.data),
  get: (id: number) => apiClient.get<CashSessionDetail>(`/api/cash-sessions/${id}`).then((r) => r.data),
  open: (p: OpenCashSessionPayload) =>
    apiClient.post<CashSessionDetail>('/api/cash-sessions/open', p).then((r) => r.data),
  close: (id: number, p: CloseCashSessionPayload) =>
    apiClient.post<CashSessionDetail>(`/api/cash-sessions/${id}/close`, p).then((r) => r.data),
  openingDefault: (cashRegisterId: number) =>
    apiClient
      .get<import('@/types').OpeningDefault>('/api/cash-sessions/opening-default', { params: { cashRegisterId } })
      .then((r) => r.data),
  pendingItems: (id: number) =>
    apiClient
      .get<import('@/types').SessionPendingItems>(`/api/cash-sessions/${id}/pending-items`)
      .then((r) => r.data)
};
