import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { reconciliationApi } from '@/api/reconciliationApi';
import type { CreateReconciliationBatchPayload, ReconcileItemsPayload } from '@/types';

export function useReconciliations(page = 1, pageSize = 50) {
  return useQuery({
    queryKey: ['reconciliations', page, pageSize],
    queryFn: () => reconciliationApi.list(page, pageSize)
  });
}

export function useReconciliation(id: number | undefined) {
  return useQuery({
    queryKey: ['reconciliation', id],
    queryFn: () => reconciliationApi.get(id!),
    enabled: !!id
  });
}

export function useCreateReconciliation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CreateReconciliationBatchPayload) => reconciliationApi.create(p),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['reconciliations'] })
  });
}

export function useMatchReconciliation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: ReconcileItemsPayload }) =>
      reconciliationApi.match(id, payload),
    onSuccess: (_, { id }) => {
      qc.invalidateQueries({ queryKey: ['reconciliations'] });
      qc.invalidateQueries({ queryKey: ['reconciliation', id] });
    }
  });
}
