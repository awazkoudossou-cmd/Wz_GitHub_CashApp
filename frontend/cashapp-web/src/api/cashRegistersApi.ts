import type {
  CashRegisterDetail,
  CashRegisterListItem,
  CreateCashRegisterPayload,
  UpdateCashRegisterPayload
} from '@/types';
import { apiClient } from './client';

export const cashRegistersApi = {
  list: () => apiClient.get<CashRegisterListItem[]>('/api/cash-registers').then((r) => r.data),
  get: (id: number) => apiClient.get<CashRegisterDetail>(`/api/cash-registers/${id}`).then((r) => r.data),
  create: (p: CreateCashRegisterPayload) =>
    apiClient.post<CashRegisterDetail>('/api/cash-registers', p).then((r) => r.data),
  update: (id: number, p: UpdateCashRegisterPayload) =>
    apiClient.put<CashRegisterDetail>(`/api/cash-registers/${id}`, p).then((r) => r.data),
  updateStatus: (id: number, isActive: boolean) =>
    apiClient.patch(`/api/cash-registers/${id}/status`, { isActive }).then((r) => r.data)
};
