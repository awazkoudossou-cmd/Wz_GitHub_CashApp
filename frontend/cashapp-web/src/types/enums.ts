// Aligned with backend enums — stored as string codes everywhere.

export const OperationDirection = {
  IN: 'IN',
  OUT: 'OUT'
} as const;
export type OperationDirection = (typeof OperationDirection)[keyof typeof OperationDirection];

export const CashSessionStatus = {
  OPEN: 'OPEN',
  CLOSED: 'CLOSED'
} as const;
export type CashSessionStatus = (typeof CashSessionStatus)[keyof typeof CashSessionStatus];

export const PaymentMethod = {
  CASH: 'CASH',
  MOBILE_MONEY: 'MOBILE_MONEY',
  BANK_TRANSFER: 'BANK_TRANSFER',
  CHECK: 'CHECK'
} as const;
export type PaymentMethod = (typeof PaymentMethod)[keyof typeof PaymentMethod];

export const AppMode = {
  ESSENTIAL: 'ESSENTIAL',
  INTERMEDIATE: 'INTERMEDIATE',
  ADVANCED: 'ADVANCED'
} as const;
export type AppMode = (typeof AppMode)[keyof typeof AppMode];

export const RoleCodes = {
  ADMIN: 'ADMIN',
  SUPERVISOR: 'SUPERVISOR',
  CASHIER: 'CASHIER'
} as const;
export type RoleCode = (typeof RoleCodes)[keyof typeof RoleCodes];

export const FeatureCodes = {
  CORE_AUTH: 'CORE_AUTH',
  CORE_USERS: 'CORE_USERS',
  CORE_CASH_REGISTERS: 'CORE_CASH_REGISTERS',
  CORE_CASH_SESSIONS: 'CORE_CASH_SESSIONS',
  CORE_OPERATIONS: 'CORE_OPERATIONS',
  CORE_CATEGORIES: 'CORE_CATEGORIES',
  CORE_DASHBOARD: 'CORE_DASHBOARD',
  CORE_EXPORTS: 'CORE_EXPORTS',
  CORE_BACKUP: 'CORE_BACKUP',
  CORE_SETTINGS: 'CORE_SETTINGS',
  ADV_VALIDATION: 'ADV_VALIDATION',
  ADV_TRANSFERS: 'ADV_TRANSFERS',
  ADV_BANK_DEPOSITS: 'ADV_BANK_DEPOSITS',
  ADV_ATTACHMENTS: 'ADV_ATTACHMENTS',
  ADV_ADVANCED_REPORTS: 'ADV_ADVANCED_REPORTS',
  ADV_IMPORTS: 'ADV_IMPORTS',
  ADV_RECONCILIATION: 'ADV_RECONCILIATION',
  ADV_VARIANCE_MANAGEMENT: 'ADV_VARIANCE_MANAGEMENT',
  ADV_ANOMALIES: 'ADV_ANOMALIES',
  ADV_AUDIT_LOG: 'ADV_AUDIT_LOG',
  ADV_ACCOUNTING: 'ADV_ACCOUNTING'
} as const;
export type FeatureCode = (typeof FeatureCodes)[keyof typeof FeatureCodes];
