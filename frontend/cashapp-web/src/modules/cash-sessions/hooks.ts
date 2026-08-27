import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { cashSessionsApi } from '@/api/cashSessionsApi';
import type { CloseCashSessionPayload, OpenCashSessionPayload } from '@/types';

export function useCashSessions(cashRegisterId?: number) {
  return useQuery({
    queryKey: ['cash-sessions', cashRegisterId],
    queryFn: () => cashSessionsApi.list(cashRegisterId)
  });
}

export function useCashSession(id: number | undefined) {
  return useQuery({
    queryKey: ['cash-session', id],
    queryFn: () => cashSessionsApi.get(id!),
    enabled: !!id
  });
}

export function useOpenCashSession() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: OpenCashSessionPayload) => cashSessionsApi.open(p),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cash-sessions'] });
      qc.invalidateQueries({ queryKey: ['dashboard'] });
    }
  });
}

export function useCloseCashSession() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: CloseCashSessionPayload }) =>
      cashSessionsApi.close(id, payload),
    onSuccess: (_, { id }) => {
      qc.invalidateQueries({ queryKey: ['cash-sessions'] });
      qc.invalidateQueries({ queryKey: ['cash-session', id] });
      qc.invalidateQueries({ queryKey: ['dashboard'] });
    }
  });
}
