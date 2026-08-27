import type {
  CashOperationDetail,
  CashOperationFilter,
  CashOperationListItem,
  CreateCashOperationPayload,
  PagedResponse,
  UpdateCashOperationPayload
} from '@/types';
import { apiClient } from './client';

export const cashOperationsApi = {
  list: (filter: CashOperationFilter = {}) =>
    apiClient
      .get<PagedResponse<CashOperationListItem>>('/api/cash-operations', { params: filter })
      .then((r) => r.data),
  get: (id: number) =>
    apiClient.get<CashOperationDetail>(`/api/cash-operations/${id}`).then((r) => r.data),
  create: (p: CreateCashOperationPayload) =>
    apiClient.post<CashOperationDetail>('/api/cash-operations', p).then((r) => r.data),
  update: (id: number, p: UpdateCashOperationPayload) =>
    apiClient.put<CashOperationDetail>(`/api/cash-operations/${id}`, p).then((r) => r.data),
  cancel: (id: number, reason: string) =>
    apiClient.patch(`/api/cash-operations/${id}/cancel`, { reason }).then((r) => r.data)
};
