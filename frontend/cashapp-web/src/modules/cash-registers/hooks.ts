import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { cashRegistersApi } from '@/api/cashRegistersApi';
import type { CreateCashRegisterPayload, UpdateCashRegisterPayload } from '@/types';

const KEY = ['cash-registers'] as const;

export function useCashRegisters() {
  return useQuery({ queryKey: KEY, queryFn: cashRegistersApi.list });
}

export function useCashRegister(id: number | undefined) {
  return useQuery({
    queryKey: ['cash-register', id],
    queryFn: () => cashRegistersApi.get(id!),
    enabled: !!id
  });
}

export function useCreateCashRegister() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CreateCashRegisterPayload) => cashRegistersApi.create(p),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY })
  });
}

export function useUpdateCashRegister() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: UpdateCashRegisterPayload }) =>
      cashRegistersApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY })
  });
}

export function useUpdateCashRegisterStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) =>
      cashRegistersApi.updateStatus(id, isActive),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY })
  });
}
