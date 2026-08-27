import type {
  AccountingAccountDetail,
  AccountingAccountFilter,
  AccountingAccountListItem,
  AccountingChecklist,
  AccountingDashboard,
  AccountingEntryDetail,
  AccountingEntryFilter,
  AccountingEntryListItem,
  AccountingExportDetail,
  AccountingExportLog,
  AccountingExportLogFilter,
  AccountingExportPreview,
  AccountingGenerationDetail,
  AccountingGenerationFilter,
  AccountingGenerationListItem,
  AccountingGenerationPreview,
  AccountingJournalDetail,
  AccountingJournalListItem,
  AccountingLedgerStats,
  AccountingPending,
  AccountingPendingFilter,
  AccountingPreviewResult,
  AccountingQueueFilter,
  AccountingQueueItem,
  AccountingSettings,
  CreateAccountingAccountPayload,
  CreateAccountingJournalPayload,
  EnqueueManualGenerationPayload,
  GenerateAccountingEntriesPayload,
  PagedResponse,
  UpdateAccountingAccountPayload,
  UpdateAccountingEntryPayload,
  UpdateAccountingJournalPayload,
  UpdateAccountingSettingsPayload,
  WizardCashRegister,
  WizardCategory
} from '@/types';
import { apiClient } from './client';

async function downloadBlob(response: { headers: Record<string, unknown>; data: unknown }, fallbackName: string) {
  const cd = response.headers['content-disposition'] as string | undefined;
  const match = cd?.match(/filename="?([^";]+)"?/i);
  const fileName = match?.[1] ?? fallbackName;
  const blob = response.data as Blob;
  const objectUrl = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = objectUrl;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(objectUrl);
}

export const accountingApi = {
  // Journaux
  listJournals: () => apiClient.get<AccountingJournalListItem[]>('/api/accounting/journals').then((r) => r.data),
  createJournal: (p: CreateAccountingJournalPayload) =>
    apiClient.post<AccountingJournalDetail>('/api/accounting/journals', p).then((r) => r.data),
  updateJournal: (id: number, p: UpdateAccountingJournalPayload) =>
    apiClient.put<AccountingJournalDetail>(`/api/accounting/journals/${id}`, p).then((r) => r.data),
  setJournalStatus: (id: number, isActive: boolean) =>
    apiClient.patch<AccountingJournalDetail>(`/api/accounting/journals/${id}/status`, { isActive }).then((r) => r.data),
  deleteJournal: (id: number) => apiClient.delete(`/api/accounting/journals/${id}`).then((r) => r.data),

  // Comptes
  listAccounts: (filter: AccountingAccountFilter = {}) =>
    apiClient.get<PagedResponse<AccountingAccountListItem>>('/api/accounting/accounts', { params: filter }).then((r) => r.data),
  createAccount: (p: CreateAccountingAccountPayload) =>
    apiClient.post<AccountingAccountDetail>('/api/accounting/accounts', p).then((r) => r.data),
  updateAccount: (id: number, p: UpdateAccountingAccountPayload) =>
    apiClient.put<AccountingAccountDetail>(`/api/accounting/accounts/${id}`, p).then((r) => r.data),
  setAccountStatus: (id: number, isActive: boolean) =>
    apiClient.patch<AccountingAccountDetail>(`/api/accounting/accounts/${id}/status`, { isActive }).then((r) => r.data),
  deleteAccount: (id: number) => apiClient.delete(`/api/accounting/accounts/${id}`).then((r) => r.data),

  // Paramètres
  getSettings: () => apiClient.get<AccountingSettings>('/api/accounting/settings').then((r) => r.data),
  updateSettings: (p: UpdateAccountingSettingsPayload) =>
    apiClient.put<AccountingSettings>('/api/accounting/settings', p).then((r) => r.data),

  // Assistant de configuration
  listWizardCashRegisters: () =>
    apiClient.get<WizardCashRegister[]>('/api/accounting/wizard/cash-registers').then((r) => r.data),
  assignCashRegisterJournal: (cashRegisterId: number, accountingJournalId: number) =>
    apiClient.put<WizardCashRegister>(`/api/accounting/wizard/cash-registers/${cashRegisterId}/journal`, { accountingJournalId }).then((r) => r.data),
  assignCashRegisterAccount: (cashRegisterId: number, accountingAccountId: number) =>
    apiClient.put<WizardCashRegister>(`/api/accounting/wizard/cash-registers/${cashRegisterId}/account`, { accountingAccountId }).then((r) => r.data),
  listWizardCategories: () =>
    apiClient.get<WizardCategory[]>('/api/accounting/wizard/categories').then((r) => r.data),
  assignCategoryAccount: (categoryId: number, accountingAccountId: number) =>
    apiClient.put<WizardCategory>(`/api/accounting/wizard/categories/${categoryId}/account`, { accountingAccountId }).then((r) => r.data),
  getChecklist: () => apiClient.get<AccountingChecklist>('/api/accounting/wizard/checklist').then((r) => r.data),
  preview: (template: string) =>
    apiClient.post<AccountingPreviewResult>('/api/accounting/wizard/preview', { template }).then((r) => r.data),

  // Moteur de génération
  generate: (p: GenerateAccountingEntriesPayload) =>
    apiClient.post<AccountingGenerationDetail>('/api/accounting/generations/generate', p).then((r) => r.data),
  generatePending: () =>
    apiClient.post<AccountingGenerationDetail>('/api/accounting/generations/generate-pending').then((r) => r.data),
  previewGeneration: (params: GenerateAccountingEntriesPayload) => {
    // Construit la query string à la main : ASP.NET Core lie un int[] via des clés répétées
    // (cashRegisterIds=1&cashRegisterIds=2), pas via la notation "cashRegisterIds[]=1" qu'axios
    // produit par défaut pour un tableau passé dans `params` — sinon le filtre est silencieusement ignoré.
    const search = new URLSearchParams({ startDate: params.startDate, endDate: params.endDate });
    for (const id of params.cashRegisterIds ?? []) search.append('cashRegisterIds', String(id));
    return apiClient
      .get<AccountingGenerationPreview>(`/api/accounting/generations/preview?${search.toString()}`)
      .then((r) => r.data);
  },

  // Historique / batchs
  listGenerations: (filter: AccountingGenerationFilter = {}) =>
    apiClient.get<PagedResponse<AccountingGenerationListItem>>('/api/accounting/generations', { params: filter }).then((r) => r.data),
  getGeneration: (id: number) =>
    apiClient.get<AccountingGenerationDetail>(`/api/accounting/generations/${id}`).then((r) => r.data),
  cancelGeneration: (id: number) =>
    apiClient.post<AccountingGenerationDetail>(`/api/accounting/generations/${id}/cancel`).then((r) => r.data),
  deleteGeneration: (id: number) => apiClient.delete(`/api/accounting/generations/${id}`).then((r) => r.data),
  regenerateGeneration: (id: number) =>
    apiClient.post<AccountingGenerationDetail>(`/api/accounting/generations/${id}/regenerate`).then((r) => r.data),

  // Brouillard comptable (écritures)
  listEntries: (filter: AccountingEntryFilter = {}) =>
    apiClient.get<PagedResponse<AccountingEntryListItem>>('/api/accounting/entries', { params: filter }).then((r) => r.data),
  getEntry: (id: number) =>
    apiClient.get<AccountingEntryDetail>(`/api/accounting/entries/${id}`).then((r) => r.data),
  updateEntry: (id: number, p: UpdateAccountingEntryPayload) =>
    apiClient.patch<AccountingEntryDetail>(`/api/accounting/entries/${id}`, p).then((r) => r.data),
  getLedgerStats: () => apiClient.get<AccountingLedgerStats>('/api/accounting/entries/stats').then((r) => r.data),

  listPending: (filter: AccountingPendingFilter = {}) =>
    apiClient.get<PagedResponse<AccountingPending>>('/api/accounting/pending', { params: filter }).then((r) => r.data),

  // File d'attente (Queue)
  enqueueManual: (p: EnqueueManualGenerationPayload) =>
    apiClient.post<AccountingQueueItem>('/api/accounting/queue/manual', p).then((r) => r.data),
  retryQueue: (id: number) =>
    apiClient.post<AccountingQueueItem>(`/api/accounting/queue/${id}/retry`).then((r) => r.data),
  cancelQueue: (id: number) =>
    apiClient.post<AccountingQueueItem>(`/api/accounting/queue/${id}/cancel`).then((r) => r.data),
  listQueue: (filter: AccountingQueueFilter = {}) =>
    apiClient.get<PagedResponse<AccountingQueueItem>>('/api/accounting/queue', { params: filter }).then((r) => r.data),
  getQueueItem: (id: number) =>
    apiClient.get<AccountingQueueItem>(`/api/accounting/queue/${id}`).then((r) => r.data),

  // Centre d'Exports
  previewExport: (filter: AccountingEntryFilter = {}) =>
    apiClient.post<AccountingExportPreview>('/api/accounting/exports/preview', filter).then((r) => r.data),
  exportGeneration: async (generationId: number) => {
    const r = await apiClient.post(`/api/accounting/exports/generations/${generationId}`, null, { responseType: 'blob' });
    await downloadBlob(r, `ACCOUNTING_BATCH_${generationId}.xlsx`);
  },
  exportEntries: async (filter: AccountingEntryFilter = {}) => {
    const r = await apiClient.post('/api/accounting/exports/entries', filter, { responseType: 'blob' });
    await downloadBlob(r, 'brouillard_comptable.xlsx');
  },
  listExportLogs: (filter: AccountingExportLogFilter = {}) =>
    apiClient.get<PagedResponse<AccountingExportLog>>('/api/accounting/exports', { params: filter }).then((r) => r.data),
  getExportDetail: (logId: number) =>
    apiClient.get<AccountingExportDetail>(`/api/accounting/exports/${logId}`).then((r) => r.data),
  downloadExportLog: async (logId: number, fallbackName: string) => {
    const r = await apiClient.get(`/api/accounting/exports/${logId}/download`, { responseType: 'blob' });
    await downloadBlob(r, fallbackName);
  },
  reexportLog: async (logId: number, fallbackName: string) => {
    const r = await apiClient.post(`/api/accounting/exports/${logId}/reexport`, null, { responseType: 'blob' });
    await downloadBlob(r, fallbackName);
  },
  deleteExportLog: (logId: number) => apiClient.delete(`/api/accounting/exports/${logId}`).then((r) => r.data),

  // Tableau de bord
  getDashboard: () => apiClient.get<AccountingDashboard>('/api/accounting/dashboard').then((r) => r.data)
};
