// V2 enums alignés sur le backend (string literals).

export const ApprovalStatus = {
  PENDING: 'PENDING',
  APPROVED: 'APPROVED',
  REJECTED: 'REJECTED',
  CANCELLED: 'CANCELLED'
} as const;
export type ApprovalStatus = (typeof ApprovalStatus)[keyof typeof ApprovalStatus];

export const ApprovalTargetType = {
  CASH_OPERATION: 'CASH_OPERATION',
  CASH_OPERATION_CANCEL: 'CASH_OPERATION_CANCEL',
  CASH_TRANSFER: 'CASH_TRANSFER',
  BANK_DEPOSIT: 'BANK_DEPOSIT',
  CASH_SESSION_CLOSE: 'CASH_SESSION_CLOSE',
  CASH_SESSION_REOPEN: 'CASH_SESSION_REOPEN',
  VARIANCE_CASE: 'VARIANCE_CASE'
} as const;
export type ApprovalTargetType = (typeof ApprovalTargetType)[keyof typeof ApprovalTargetType];

export const CashTransferStatus = {
  DRAFT: 'DRAFT',
  PENDING_APPROVAL: 'PENDING_APPROVAL',
  APPROVED: 'APPROVED',
  REJECTED: 'REJECTED',
  COMPLETED: 'COMPLETED',
  CANCELLED: 'CANCELLED'
} as const;
export type CashTransferStatus = (typeof CashTransferStatus)[keyof typeof CashTransferStatus];

export const BankDepositStatus = CashTransferStatus;
export type BankDepositStatus = CashTransferStatus;

export const AnomalySeverity = {
  LOW: 'LOW',
  MEDIUM: 'MEDIUM',
  HIGH: 'HIGH',
  CRITICAL: 'CRITICAL'
} as const;
export type AnomalySeverity = (typeof AnomalySeverity)[keyof typeof AnomalySeverity];

export const AnomalyStatus = {
  OPEN: 'OPEN',
  IN_REVIEW: 'IN_REVIEW',
  RESOLVED: 'RESOLVED',
  CLOSED: 'CLOSED'
} as const;
export type AnomalyStatus = (typeof AnomalyStatus)[keyof typeof AnomalyStatus];

export const VarianceStatus = {
  OPEN: 'OPEN',
  JUSTIFIED: 'JUSTIFIED',
  PENDING_APPROVAL: 'PENDING_APPROVAL',
  APPROVED: 'APPROVED',
  REJECTED: 'REJECTED',
  CLOSED: 'CLOSED'
} as const;
export type VarianceStatus = (typeof VarianceStatus)[keyof typeof VarianceStatus];

export const AuditAction = {
  LOGIN: 'LOGIN',
  LOGOUT: 'LOGOUT',
  CREATE: 'CREATE',
  UPDATE: 'UPDATE',
  DELETE: 'DELETE',
  OPEN: 'OPEN',
  CLOSE: 'CLOSE',
  CANCEL: 'CANCEL',
  APPROVE: 'APPROVE',
  REJECT: 'REJECT',
  ASSIGN: 'ASSIGN',
  RESOLVE: 'RESOLVE',
  COMPLETE: 'COMPLETE',
  CONFIG_CHANGE: 'CONFIG_CHANGE',
  FEATURE_TOGGLE: 'FEATURE_TOGGLE'
} as const;
export type AuditAction = (typeof AuditAction)[keyof typeof AuditAction];

export const ImportBatchType = {
  CASH_OPERATIONS: 'CASH_OPERATIONS',
  CATEGORIES: 'CATEGORIES',
  ACCOUNTING_ACCOUNTS: 'ACCOUNTING_ACCOUNTS'
} as const;
export type ImportBatchType = (typeof ImportBatchType)[keyof typeof ImportBatchType];

export const ImportBatchStatus = {
  UPLOADED: 'UPLOADED',
  PREVIEWED: 'PREVIEWED',
  CONFIRMED: 'CONFIRMED',
  PARTIAL: 'PARTIAL',
  FAILED: 'FAILED',
  CANCELLED: 'CANCELLED'
} as const;
export type ImportBatchStatus = (typeof ImportBatchStatus)[keyof typeof ImportBatchStatus];

export const ImportLineStatus = {
  PENDING: 'PENDING',
  VALID: 'VALID',
  INVALID: 'INVALID',
  IMPORTED: 'IMPORTED',
  SKIPPED: 'SKIPPED'
} as const;
export type ImportLineStatus = (typeof ImportLineStatus)[keyof typeof ImportLineStatus];

export const ReconciliationBatchType = {
  BANK_DEPOSIT_VS_OPERATIONS: 'BANK_DEPOSIT_VS_OPERATIONS',
  IMPORTED_VS_RECORDED: 'IMPORTED_VS_RECORDED',
  GENERIC: 'GENERIC'
} as const;
export type ReconciliationBatchType = (typeof ReconciliationBatchType)[keyof typeof ReconciliationBatchType];

export const ReconciliationStatus = {
  OPEN: 'OPEN',
  IN_PROGRESS: 'IN_PROGRESS',
  CLOSED: 'CLOSED'
} as const;
export type ReconciliationStatus = (typeof ReconciliationStatus)[keyof typeof ReconciliationStatus];

export const ReconciliationMatchStatus = {
  UNMATCHED: 'UNMATCHED',
  MATCHED: 'MATCHED',
  DISPUTED: 'DISPUTED'
} as const;
export type ReconciliationMatchStatus = (typeof ReconciliationMatchStatus)[keyof typeof ReconciliationMatchStatus];

// V3 — Comptabilité
export const AccountingAccountNature = {
  ASSET: 'ASSET',
  LIABILITY: 'LIABILITY',
  EXPENSE: 'EXPENSE',
  REVENUE: 'REVENUE',
  BANK: 'BANK',
  CASH: 'CASH',
  EQUITY: 'EQUITY',
  OTHER: 'OTHER'
} as const;
export type AccountingAccountNature = (typeof AccountingAccountNature)[keyof typeof AccountingAccountNature];

export const AccountingGenerationType = {
  CENTRALIZED: 'CENTRALIZED',
  DETAILED: 'DETAILED'
} as const;
export type AccountingGenerationType = (typeof AccountingGenerationType)[keyof typeof AccountingGenerationType];

export const AccountingGenerationMode = {
  MANUAL: 'MANUAL',
  ON_CASH_CLOSING: 'ON_CASH_CLOSING'
} as const;
export type AccountingGenerationMode = (typeof AccountingGenerationMode)[keyof typeof AccountingGenerationMode];

export const AccountingGenerationStatus = {
  DRAFT: 'DRAFT',
  GENERATED: 'GENERATED',
  EXPORTED: 'EXPORTED',
  CANCELLED: 'CANCELLED'
} as const;
export type AccountingGenerationStatus = (typeof AccountingGenerationStatus)[keyof typeof AccountingGenerationStatus];

export const AccountingExportStatus = {
  GENERATED: 'GENERATED',
  DOWNLOADED: 'DOWNLOADED',
  DELETED: 'DELETED'
} as const;
export type AccountingExportStatus = (typeof AccountingExportStatus)[keyof typeof AccountingExportStatus];
