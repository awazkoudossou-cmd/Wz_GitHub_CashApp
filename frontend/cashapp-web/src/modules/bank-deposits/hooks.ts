import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { bankDepositsApi } from '@/api/bankDepositsApi';
import type { BankDepositFilter, CreateBankDepositPayload } from '@/types';

export function useBankDeposits(filter: BankDepositFilter = {}) {
  return useQuery({ queryKey: ['bank-deposits', filter], queryFn: () => bankDepositsApi.list(filter) });
}

export function useBankDeposit(id: number | undefined) {
  return useQuery({
    queryKey: ['bank-deposit', id],
    queryFn: () => bankDepositsApi.get(id!),
    enabled: !!id
  });
}

function notify(message: string, severity: 'success' | 'info' | 'error' = 'success') {
  import('@/app/store/notificationStore').then(({ useNotificationStore }) => {
    const fn = severity === 'success' ? useNotificationStore.getState().notifySuccess :
               severity === 'error' ? useNotificationStore.getState().notifyError :
               useNotificationStore.getState().notifyInfo;
    fn(message);
  });
}

export function useCreateBankDeposit() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CreateBankDepositPayload) => bankDepositsApi.create(p),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: ['bank-deposits'] });
      notify(`Dépôt banque ${data.depositRef} créé.`);
    }
  });
}

export function useCompleteBankDeposit() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => bankDepositsApi.complete(id),
    onSuccess: async (data, id) => {
      await Promise.all([
        qc.refetchQueries({ queryKey: ['bank-deposits'] }),
        qc.refetchQueries({ queryKey: ['bank-deposit', id] }),
        qc.refetchQueries({ queryKey: ['cash-operations'] }),
        qc.refetchQueries({ queryKey: ['cash-sessions'] }),
        qc.refetchQueries({ queryKey: ['dashboard'] })
      ]);
      notify(`Dépôt banque ${data.depositRef} finalisé.`);
    }
  });
}

export function useCancelBankDeposit() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: number; reason: string }) => bankDepositsApi.cancel(id, reason),
    onSuccess: (_, { id }) => {
      qc.invalidateQueries({ queryKey: ['bank-deposits'] });
      qc.invalidateQueries({ queryKey: ['bank-deposit', id] });
      notify('Dépôt banque annulé.', 'info');
    }
  });
}
