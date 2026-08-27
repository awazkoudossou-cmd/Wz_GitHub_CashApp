import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { cashTransfersApi } from '@/api/cashTransfersApi';
import type { CashTransferFilter, CreateCashTransferPayload } from '@/types';

export function useCashTransfers(filter: CashTransferFilter = {}) {
  return useQuery({ queryKey: ['cash-transfers', filter], queryFn: () => cashTransfersApi.list(filter) });
}

export function useCashTransfer(id: number | undefined) {
  return useQuery({
    queryKey: ['cash-transfer', id],
    queryFn: () => cashTransfersApi.get(id!),
    enabled: !!id
  });
}

export function useCreateCashTransfer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CreateCashTransferPayload) => cashTransfersApi.create(p),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: ['cash-transfers'] });
      import('@/app/store/notificationStore').then(({ useNotificationStore }) => {
        useNotificationStore.getState().notifySuccess(`Transfert ${data.transferRef} créé.`);
      });
    }
  });
}

export function useCompleteCashTransfer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => cashTransfersApi.complete(id),
    onSuccess: async (data, id) => {
      // refetchQueries (au lieu d'invalidate) force le rechargement immédiat des listes,
      // même quand l'utilisateur n'est pas actuellement sur la page concernée.
      await Promise.all([
        qc.refetchQueries({ queryKey: ['cash-transfers'] }),
        qc.refetchQueries({ queryKey: ['cash-transfer', id] }),
        qc.refetchQueries({ queryKey: ['cash-operations'] }),
        qc.refetchQueries({ queryKey: ['cash-sessions'] }),
        qc.refetchQueries({ queryKey: ['dashboard'] })
      ]);
      import('@/app/store/notificationStore').then(({ useNotificationStore }) => {
        useNotificationStore.getState().notifySuccess(`Transfert ${data.transferRef} finalisé.`);
      });
    }
  });
}

export function useCancelCashTransfer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: number; reason: string }) => cashTransfersApi.cancel(id, reason),
    onSuccess: (_, { id }) => {
      qc.invalidateQueries({ queryKey: ['cash-transfers'] });
      qc.invalidateQueries({ queryKey: ['cash-transfer', id] });
      import('@/app/store/notificationStore').then(({ useNotificationStore }) => {
        useNotificationStore.getState().notifyInfo('Transfert annulé.');
      });
    }
  });
}
