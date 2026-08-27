import { useQuery } from '@tanstack/react-query';
import { dashboardApi } from '@/api/dashboardApi';

export function useCashierDashboard(cashRegisterId: number | null) {
  return useQuery({
    queryKey: ['dashboard', 'cashier', cashRegisterId],
    queryFn: () => dashboardApi.cashier(cashRegisterId!),
    enabled: !!cashRegisterId
  });
}

export function useSupervisorDashboard() {
  return useQuery({
    queryKey: ['dashboard', 'supervisor'],
    queryFn: dashboardApi.supervisor
  });
}
