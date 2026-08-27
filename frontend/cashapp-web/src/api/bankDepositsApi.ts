import type {
  BankDepositDetail,
  BankDepositFilter,
  BankDepositListItem,
  CreateBankDepositPayload,
  PagedResponse
} from '@/types';
import { apiClient } from './client';

export const bankDepositsApi = {
  list: (filter: BankDepositFilter = {}) =>
    apiClient
      .get<PagedResponse<BankDepositListItem>>('/api/bank-deposits', { params: filter })
      .then((r) => r.data),
  get: (id: number) => apiClient.get<BankDepositDetail>(`/api/bank-deposits/${id}`).then((r) => r.data),
  create: (p: CreateBankDepositPayload) =>
    apiClient.post<BankDepositDetail>('/api/bank-deposits', p).then((r) => r.data),
  complete: (id: number) =>
    apiClient.post<BankDepositDetail>(`/api/bank-deposits/${id}/complete`).then((r) => r.data),
  cancel: (id: number, reason: string) =>
    apiClient
      .post<BankDepositDetail>(`/api/bank-deposits/${id}/cancel`, { reason })
      .then((r) => r.data)
};
