import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { accountingApi } from '@/api/accountingApi';
import { useNotificationStore } from '@/app/store/notificationStore';
import type {
  AccountingAccountFilter,
  AccountingEntryFilter,
  AccountingExportLogFilter,
  AccountingGenerationFilter,
  AccountingPendingFilter,
  AccountingQueueFilter,
  CreateAccountingAccountPayload,
  CreateAccountingJournalPayload,
  EnqueueManualGenerationPayload,
  GenerateAccountingEntriesPayload,
  UpdateAccountingAccountPayload,
  UpdateAccountingEntryPayload,
  UpdateAccountingJournalPayload,
  UpdateAccountingSettingsPayload
} from '@/types';

function notify(message: string, severity: 'success' | 'info' | 'error' = 'success') {
  const store = useNotificationStore.getState();
  (severity === 'success' ? store.notifySuccess : severity === 'error' ? store.notifyError : store.notifyInfo)(message);
}

// --- Journaux ---

export function useAccountingJournals() {
  return useQuery({ queryKey: ['accounting', 'journals'], queryFn: accountingApi.listJournals });
}

export function useCreateAccountingJournal() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CreateAccountingJournalPayload) => accountingApi.createJournal(p),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: ['accounting', 'journals'] });
      notify(`Journal ${data.code} créé.`);
    }
  });
}

export function useUpdateAccountingJournal() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, p }: { id: number; p: UpdateAccountingJournalPayload }) => accountingApi.updateJournal(id, p),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'journals'] });
      notify('Journal mis à jour.');
    }
  });
}

export function useSetAccountingJournalStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) => accountingApi.setJournalStatus(id, isActive),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['accounting', 'journals'] })
  });
}

export function useDeleteAccountingJournal() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => accountingApi.deleteJournal(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'journals'] });
      notify('Journal supprimé.', 'info');
    }
  });
}

// --- Comptes ---

export function useAccountingAccounts(filter: AccountingAccountFilter = {}) {
  return useQuery({ queryKey: ['accounting', 'accounts', filter], queryFn: () => accountingApi.listAccounts(filter) });
}

export function useCreateAccountingAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CreateAccountingAccountPayload) => accountingApi.createAccount(p),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: ['accounting', 'accounts'] });
      notify(`Compte ${data.accountNumber} créé.`);
    }
  });
}

export function useUpdateAccountingAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, p }: { id: number; p: UpdateAccountingAccountPayload }) => accountingApi.updateAccount(id, p),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'accounts'] });
      notify('Compte mis à jour.');
    }
  });
}

export function useSetAccountingAccountStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) => accountingApi.setAccountStatus(id, isActive),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['accounting', 'accounts'] })
  });
}

export function useDeleteAccountingAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => accountingApi.deleteAccount(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'accounts'] });
      notify('Compte supprimé.', 'info');
    }
  });
}

// --- Paramètres ---

export function useAccountingSettings() {
  return useQuery({ queryKey: ['accounting', 'settings'], queryFn: accountingApi.getSettings });
}

export function useUpdateAccountingSettings() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: UpdateAccountingSettingsPayload) => accountingApi.updateSettings(p),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'settings'] });
      qc.invalidateQueries({ queryKey: ['accounting', 'checklist'] });
    }
  });
}

// --- Assistant de configuration ---

export function useWizardCashRegisters() {
  return useQuery({ queryKey: ['accounting', 'wizard', 'cash-registers'], queryFn: accountingApi.listWizardCashRegisters });
}

export function useAssignCashRegisterJournal() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ cashRegisterId, accountingJournalId }: { cashRegisterId: number; accountingJournalId: number }) =>
      accountingApi.assignCashRegisterJournal(cashRegisterId, accountingJournalId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'wizard', 'cash-registers'] });
      qc.invalidateQueries({ queryKey: ['accounting', 'checklist'] });
    }
  });
}

export function useAssignCashRegisterAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ cashRegisterId, accountingAccountId }: { cashRegisterId: number; accountingAccountId: number }) =>
      accountingApi.assignCashRegisterAccount(cashRegisterId, accountingAccountId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'wizard', 'cash-registers'] });
      qc.invalidateQueries({ queryKey: ['accounting', 'checklist'] });
    }
  });
}

export function useWizardCategories() {
  return useQuery({ queryKey: ['accounting', 'wizard', 'categories'], queryFn: accountingApi.listWizardCategories });
}

export function useAssignCategoryAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ categoryId, accountingAccountId }: { categoryId: number; accountingAccountId: number }) =>
      accountingApi.assignCategoryAccount(categoryId, accountingAccountId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'wizard', 'categories'] });
      qc.invalidateQueries({ queryKey: ['accounting', 'checklist'] });
    }
  });
}

export function useAccountingChecklist(enabled = true) {
  return useQuery({ queryKey: ['accounting', 'checklist'], queryFn: accountingApi.getChecklist, enabled });
}

export function useAccountingPreview() {
  return useMutation({ mutationFn: (template: string) => accountingApi.preview(template) });
}

// --- Moteur de génération ---

export function useGenerateAccountingEntries() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: GenerateAccountingEntriesPayload) => accountingApi.generate(p),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: ['accounting', 'generations'] });
      qc.invalidateQueries({ queryKey: ['accounting', 'entries'] });
      qc.invalidateQueries({ queryKey: ['accounting', 'pending-ops'] });
      notify(`Génération ${data.reference} : ${data.entries.length} écriture(s).`);
    }
  });
}

export function useGeneratePendingAccountingEntries() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => accountingApi.generatePending(),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: ['accounting', 'generations'] });
      qc.invalidateQueries({ queryKey: ['accounting', 'entries'] });
      qc.invalidateQueries({ queryKey: ['accounting', 'pending-ops'] });
      notify(`Relance ${data.reference} : ${data.entries.length} écriture(s).`);
    }
  });
}

export function usePreviewAccountingGeneration() {
  return useMutation({ mutationFn: (p: GenerateAccountingEntriesPayload) => accountingApi.previewGeneration(p) });
}

// --- Historique / brouillard / en attente ---

export function useAccountingGenerations(filter: AccountingGenerationFilter = {}) {
  return useQuery({ queryKey: ['accounting', 'generations', filter], queryFn: () => accountingApi.listGenerations(filter) });
}

export function useAccountingGeneration(id: number | undefined) {
  return useQuery({
    queryKey: ['accounting', 'generations', id],
    queryFn: () => accountingApi.getGeneration(id!),
    enabled: !!id
  });
}

export function useCancelAccountingGeneration() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => accountingApi.cancelGeneration(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'generations'] });
      notify('Génération annulée.', 'info');
    }
  });
}

export function useDeleteAccountingGeneration() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => accountingApi.deleteGeneration(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'generations'] });
      qc.invalidateQueries({ queryKey: ['accounting', 'entries'] });
      qc.invalidateQueries({ queryKey: ['accounting', 'ledger-stats'] });
      notify('Batch supprimé.', 'info');
    }
  });
}

export function useRegenerateAccountingGeneration() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => accountingApi.regenerateGeneration(id),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: ['accounting', 'generations'] });
      qc.invalidateQueries({ queryKey: ['accounting', 'entries'] });
      notify(`Batch ${data.reference} régénéré : ${data.entries.length} écriture(s).`);
    }
  });
}

export function useAccountingEntries(filter: AccountingEntryFilter = {}) {
  return useQuery({ queryKey: ['accounting', 'entries', filter], queryFn: () => accountingApi.listEntries(filter) });
}

export function useAccountingEntry(id: number | undefined) {
  return useQuery({
    queryKey: ['accounting', 'entries', 'detail', id],
    queryFn: () => accountingApi.getEntry(id!),
    enabled: !!id
  });
}

export function useUpdateAccountingEntry() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, p }: { id: number; p: UpdateAccountingEntryPayload }) => accountingApi.updateEntry(id, p),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'entries'] });
      notify('Écriture mise à jour.');
    }
  });
}

export function useAccountingLedgerStats() {
  return useQuery({ queryKey: ['accounting', 'ledger-stats'], queryFn: accountingApi.getLedgerStats });
}

export function useAccountingPendingOps(filter: AccountingPendingFilter = {}) {
  return useQuery({ queryKey: ['accounting', 'pending-ops', filter], queryFn: () => accountingApi.listPending(filter) });
}

export function useExportAccountingGeneration() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => accountingApi.exportGeneration(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'export-logs'] });
      qc.invalidateQueries({ queryKey: ['accounting', 'generations'] });
    }
  });
}

export function useExportAccountingEntries() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (filter: AccountingEntryFilter) => accountingApi.exportEntries(filter),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['accounting', 'export-logs'] })
  });
}

// --- File d'attente (Queue) ---

export function useEnqueueManualGeneration() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: EnqueueManualGenerationPayload) => accountingApi.enqueueManual(p),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'queue'] });
      notify('Demande de génération mise en file d’attente.');
    }
  });
}

export function useRetryQueueItem() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => accountingApi.retryQueue(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'queue'] });
      notify('Relance demandée.', 'info');
    }
  });
}

export function useCancelQueueItem() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => accountingApi.cancelQueue(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'queue'] });
      notify('Demande annulée.', 'info');
    }
  });
}

// Polling automatique tant que des éléments PENDING/PROCESSING existent, pour un Queue Monitor réactif.
export function useAccountingQueue(filter: AccountingQueueFilter = {}) {
  return useQuery({
    queryKey: ['accounting', 'queue', filter],
    queryFn: () => accountingApi.listQueue(filter),
    refetchInterval: (query) => {
      const items = query.state.data?.items ?? [];
      const hasActive = items.some((i) => i.status === 'PENDING' || i.status === 'PROCESSING');
      return hasActive ? 5000 : false;
    }
  });
}

// --- Tableau de bord ---

export function useAccountingDashboard() {
  return useQuery({ queryKey: ['accounting', 'dashboard'], queryFn: accountingApi.getDashboard });
}

// --- Historique des exports ---

export function useAccountingExportLogs(filter: AccountingExportLogFilter = {}) {
  return useQuery({ queryKey: ['accounting', 'export-logs', filter], queryFn: () => accountingApi.listExportLogs(filter) });
}

export function useAccountingExportDetail(id: number | null) {
  return useQuery({
    queryKey: ['accounting', 'export-logs', 'detail', id],
    queryFn: () => accountingApi.getExportDetail(id as number),
    enabled: id !== null
  });
}

export function usePreviewAccountingExport() {
  return useMutation({ mutationFn: (filter: AccountingEntryFilter) => accountingApi.previewExport(filter) });
}

export function useDownloadExportLog() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, fileName }: { id: number; fileName: string }) => accountingApi.downloadExportLog(id, fileName),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['accounting', 'export-logs'] })
  });
}

export function useReexportLog() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, fileName }: { id: number; fileName: string }) => accountingApi.reexportLog(id, fileName),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['accounting', 'export-logs'] })
  });
}

export function useDeleteExportLog() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => accountingApi.deleteExportLog(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounting', 'export-logs'] });
      notify('Export supprimé.', 'info');
    }
  });
}
