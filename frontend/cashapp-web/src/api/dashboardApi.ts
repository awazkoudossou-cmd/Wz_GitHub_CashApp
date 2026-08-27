import type { CashierDashboard, SupervisorDashboard } from '@/types';
import { apiClient } from './client';

export const dashboardApi = {
  cashier: (cashRegisterId: number) =>
    apiClient
      .get<CashierDashboard>('/api/dashboard/cashier', { params: { cashRegisterId } })
      .then((r) => r.data),
  supervisor: () =>
    apiClient.get<SupervisorDashboard>('/api/dashboard/supervisor').then((r) => r.data)
};
