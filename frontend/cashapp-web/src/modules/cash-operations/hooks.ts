import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { cashOperationsApi } from '@/api/cashOperationsApi';
import type {
  CashOperationFilter,
  CreateCashOperationPayload,
  UpdateCashOperationPayload
} from '@/types';

export function useCashOperations(filter: CashOperationFilter = {}) {
  return useQuery({
    queryKey: ['cash-operations', filter],
    queryFn: () => cashOperationsApi.list(filter)
  });
}

export function useCashOperation(id: number | undefined) {
  return useQuery({
    queryKey: ['cash-operation', id],
    queryFn: () => cashOperationsApi.get(id!),
    enabled: !!id
  });
}

export function useCreateCashOperation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CreateCashOperationPayload) => cashOperationsApi.create(p),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cash-operations'] });
      qc.invalidateQueries({ queryKey: ['cash-sessions'] });
      qc.invalidateQueries({ queryKey: ['dashboard'] });
    }
  });
}

export function useUpdateCashOperation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: UpdateCashOperationPayload }) =>
      cashOperationsApi.update(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cash-operations'] });
      qc.invalidateQueries({ queryKey: ['cash-sessions'] });
    }
  });
}

export function useCancelCashOperation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: number; reason: string }) =>
      cashOperationsApi.cancel(id, reason),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cash-operations'] });
      qc.invalidateQueries({ queryKey: ['cash-sessions'] });
    }
  });
}
