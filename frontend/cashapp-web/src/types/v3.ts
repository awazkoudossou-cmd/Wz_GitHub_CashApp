import type { AccountingAccountNature, AccountingExportStatus, AccountingGenerationMode, AccountingGenerationType } from './v2Enums';

// --- Comptabilité (V3) ---

export interface AccountingJournalListItem {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface AccountingJournalDetail extends AccountingJournalListItem {
  updatedAt?: string | null;
}

export interface CreateAccountingJournalPayload {
  code: string;
  name: string;
  description?: string | null;
}

export interface UpdateAccountingJournalPayload {
  name: string;
  description?: string | null;
}

export interface AccountingAccountListItem {
  id: number;
  accountNumber: string;
  name: string;
  nature: AccountingAccountNature;
  isActive: boolean;
  createdAt: string;
}

export interface AccountingAccountDetail extends AccountingAccountListItem {
  updatedAt?: string | null;
}

export interface CreateAccountingAccountPayload {
  accountNumber: string;
  name: string;
  nature: AccountingAccountNature;
}

export interface UpdateAccountingAccountPayload {
  name: string;
  nature: AccountingAccountNature;
}

export interface AccountingAccountFilter {
  search?: string;
  nature?: AccountingAccountNature;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}

export interface AccountingSettings {
  id: number;
  generationType: AccountingGenerationType;
  generationMode: AccountingGenerationMode;
  narrationTemplate?: string | null;
  isConfigured: boolean;
  cashAccountRootNumber?: string | null;
  cashAccountNumberLength?: number | null;
  cashJournalRootCode?: string | null;
  updatedAt?: string | null;
}

export interface UpdateAccountingSettingsPayload {
  generationType: AccountingGenerationType;
  generationMode: AccountingGenerationMode;
  narrationTemplate?: string | null;
  isConfigured: boolean;
  cashAccountRootNumber?: string | null;
  cashAccountNumberLength?: number | null;
  cashJournalRootCode?: string | null;
}

export interface WizardCashRegister {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
  accountingJournalId?: number | null;
  journalCode?: string | null;
  journalName?: string | null;
  accountingAccountId?: number | null;
  accountNumber?: string | null;
  accountName?: string | null;
}

export interface WizardCategory {
  id: number;
  code: string;
  label: string;
  isActive: boolean;
  isUsed: boolean;
  accountingAccountId?: number | null;
  accountNumber?: string | null;
  accountName?: string | null;
}

export interface AccountingChecklistItem {
  code: string;
  label: string;
  ok: boolean;
  detail?: string | null;
}

export interface AccountingChecklist {
  items: AccountingChecklistItem[];
  allOk: boolean;
}

export interface AccountingPreviewResult {
  rendered: string;
}

export interface AccountingGenerationListItem {
  id: number;
  reference: string;
  generationType: AccountingGenerationType;
  generationMode: AccountingGenerationMode;
  startDate: string;
  endDate: string;
  status: string;
  generatedBy: number;
  generatedByName: string;
  generatedAt: string;
  totalOperations: number;
  entryCount: number;
  exported: boolean;
}

export interface AccountingEntry {
  id: number;
  generationId: number;
  cashOperationId?: number | null;
  cashOperationRef?: string | null;
  journalId: number;
  journalCode: string;
  accountId: number;
  accountNumber: string;
  accountName: string;
  entryDate: string;
  operationDate: string;
  reference: string;
  pieceNumber?: string | null;
  description: string;
  comment?: string | null;
  debit: number;
  credit: number;
}

export interface AccountingGenerationDetail {
  id: number;
  reference: string;
  generationType: AccountingGenerationType;
  generationMode: AccountingGenerationMode;
  startDate: string;
  endDate: string;
  status: string;
  generatedBy: number;
  generatedByName: string;
  generatedAt: string;
  exported: boolean;
  exportedAt?: string | null;
  totalOperations: number;
  totalEntries: number;
  totalDebit: number;
  totalCredit: number;
  processingTimeMs?: number | null;
  remarks?: string | null;
  entries: AccountingEntry[];
}

export interface AccountingGenerationFilter {
  status?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

// --- Brouillard comptable (Ledger) ---

export interface AccountingEntryListItem {
  id: number;
  generationId: number;
  batchReference: string;
  generationDate: string;
  batchStatus: string;
  cashOperationId?: number | null;
  cashOperationRef?: string | null;
  operationDate: string;
  entryDate: string;
  journalId: number;
  journalCode: string;
  reference: string;
  pieceNumber?: string | null;
  accountId: number;
  accountNumber: string;
  accountName: string;
  description: string;
  comment?: string | null;
  debit: number;
  credit: number;
  userId?: number | null;
  userName: string;
  isLocked: boolean;
}

export interface AccountingEntryDetail {
  id: number;
  generationId: number;
  batchReference: string;
  generationDate: string;
  batchStatus: string;
  cashOperationId?: number | null;
  cashOperationRef?: string | null;
  cashSessionId?: number | null;
  cashRegisterId?: number | null;
  cashRegisterCode?: string | null;
  categoryId?: number | null;
  categoryCode?: string | null;
  categoryLabel?: string | null;
  userId?: number | null;
  userName: string;
  journalId: number;
  journalCode: string;
  journalName: string;
  accountId: number;
  accountNumber: string;
  accountName: string;
  operationDate: string;
  entryDate: string;
  reference: string;
  pieceNumber?: string | null;
  description: string;
  comment?: string | null;
  debit: number;
  credit: number;
  isLocked: boolean;
}

export interface UpdateAccountingEntryPayload {
  description?: string | null;
  comment?: string | null;
}

export interface AccountingEntryFilter {
  from?: string;
  to?: string;
  journalId?: number;
  accountId?: number;
  cashRegisterId?: number;
  categoryId?: number;
  userId?: number;
  generationId?: number;
  locked?: boolean;
  reference?: string;
  pieceNumber?: string;
  search?: string;
  sortBy?: string;
  sortDescending?: boolean;
  page?: number;
  pageSize?: number;
  generationType?: AccountingGenerationType;
  generationMode?: AccountingGenerationMode;
}

export interface AccountingLedgerStats {
  entryCount: number;
  batchCount: number;
  exportedBatchCount: number;
  pendingCount: number;
}

export interface AccountingPending {
  id: number;
  cashOperationId: number;
  cashOperationRef: string;
  operationDate: string;
  reason: string;
  createdDate: string;
  resolved: boolean;
  resolvedDate?: string | null;
}

export interface AccountingPendingFilter {
  resolved?: boolean;
  page?: number;
  pageSize?: number;
}

export interface GenerateAccountingEntriesPayload {
  startDate: string;
  endDate: string;
  cashRegisterIds?: number[] | null;
}

export interface AccountingGenerationPreview {
  operationCount: number;
  estimatedEntryCount: number;
  pendingCount: number;
  estimatedProcessingMs: number;
  alreadyAccountedCount: number;
  ignoredCount: number;
}

// --- File d'attente de génération (Queue) ---

export type QueueStatus = 'PENDING' | 'PROCESSING' | 'COMPLETED' | 'FAILED' | 'CANCELLED';

export interface EnqueueManualGenerationPayload {
  startDate: string;
  endDate: string;
  cashRegisterIds?: number[] | null;
  priority?: number;
}

export interface AccountingQueueItem {
  id: number;
  createdDate: string;
  requestedBy: number;
  requestedByName: string;
  generationMode: AccountingGenerationMode;
  startDate: string;
  endDate: string;
  status: QueueStatus;
  priority: number;
  cashRegisterIds?: number[] | null;
  remarks?: string | null;
  retryCount: number;
  startedDate?: string | null;
  completedDate?: string | null;
  resultGenerationId?: number | null;
  resultGenerationReference?: string | null;
}

export interface AccountingQueueFilter {
  status?: QueueStatus;
  page?: number;
  pageSize?: number;
}

// --- Tableau de bord ---

export interface AccountingDailyCount {
  date: string;
  count: number;
}

export interface AccountingNamedCount {
  name: string;
  count: number;
}

export interface AccountingDashboard {
  accountCount: number;
  journalCount: number;
  configuredCategoryCount: number;
  configuredCashRegisterCount: number;
  batchCount: number;
  entryCount: number;
  pendingCount: number;
  lastGenerationReference?: string | null;
  lastGenerationAt?: string | null;
  lastExportFileName?: string | null;
  lastExportAt?: string | null;
  batchesToday: number;
  entriesToday: number;
  exportsToday: number;
  errorsCount: number;
  entriesByDay: AccountingDailyCount[];
  generationsByDay: AccountingDailyCount[];
  journalDistribution: AccountingNamedCount[];
  accountDistribution: AccountingNamedCount[];
}

// --- Historique des exports ---

export interface AccountingExportLog {
  id: number;
  exportNumber: string;
  exportType: 'BATCH' | 'LEDGER';
  generationId?: number | null;
  generationReference?: string | null;
  generationType?: AccountingGenerationType | null;
  generationMode?: AccountingGenerationMode | null;
  fileName: string;
  exportedBy: number;
  exportedByName: string;
  exportedAt: string;
  lineCount: number;
  status: AccountingExportStatus;
}

export interface AccountingExportLogFilter {
  page?: number;
  pageSize?: number;
  status?: AccountingExportStatus;
  exportType?: 'BATCH' | 'LEDGER';
}

export interface AccountingExportPreview {
  entryCount: number;
  batchCount: number;
  accountCount: number;
  journalCount: number;
  periodStart?: string | null;
  periodEnd?: string | null;
  totalDebit: number;
  totalCredit: number;
  isBalanced: boolean;
  estimatedSizeBytes: number;
}

export interface AccountingExportDetail {
  id: number;
  exportNumber: string;
  exportType: 'BATCH' | 'LEDGER';
  generationId?: number | null;
  generationReference?: string | null;
  filterDescription?: string | null;
  exportedBy: number;
  exportedByName: string;
  exportedAt: string;
  lineCount: number;
  processingTimeMs: number;
  status: AccountingExportStatus;
  downloadedAt?: string | null;
  remarks?: string | null;
}
