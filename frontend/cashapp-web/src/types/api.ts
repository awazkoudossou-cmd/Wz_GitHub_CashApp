import type {
  AppMode,
  CashSessionStatus,
  OperationDirection,
  PaymentMethod,
  RoleCode
} from './enums';

// --- Generic envelopes ---
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// --- Auth ---
export interface CurrentUser {
  id: number;
  username: string;
  fullName: string;
  roleCode: RoleCode;
  isActive: boolean;
}

export interface CurrentCashRegister {
  id: number;
  code: string;
  name: string;
  currencyCode: string;
  isActive: boolean;
}

export interface FeatureDto {
  featureCode: string;
  featureName: string;
  isEnabled: boolean;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: CurrentUser;
  cashRegisters: CurrentCashRegister[];
  features: FeatureDto[];
  appMode: AppMode;
}

// --- Users ---
export interface UserListItem {
  id: number;
  username: string;
  fullName: string;
  roleCode: RoleCode;
  isActive: boolean;
  createdAt: string;
}

export interface UserDetail extends UserListItem {
  updatedAt?: string | null;
  cashRegisterIds: number[];
}

export interface CreateUserPayload {
  username: string;
  fullName: string;
  password: string;
  roleCode: RoleCode;
  cashRegisterIds?: number[];
}

export interface UpdateUserPayload {
  fullName: string;
  roleCode: RoleCode;
  cashRegisterIds?: number[];
}

// --- Cash registers ---
export interface CashRegisterListItem {
  id: number;
  code: string;
  name: string;
  currencyCode: string;
  isActive: boolean;
  createdAt: string;
  defaultDirection: OperationDirection;
  defaultPaymentMethod: PaymentMethod;
}

export interface CashRegisterDetail extends CashRegisterListItem {
  description?: string | null;
  updatedAt?: string | null;
  accountingAccountId?: number | null;
  accountingAccountNumber?: string | null;
  accountingJournalId?: number | null;
  accountingJournalCode?: string | null;
}

export interface CreateCashRegisterPayload {
  code: string;
  name: string;
  description?: string | null;
  currencyCode: string;
  defaultDirection: OperationDirection;
  defaultPaymentMethod: PaymentMethod;
}

export interface UpdateCashRegisterPayload {
  name: string;
  description?: string | null;
  currencyCode: string;
  defaultDirection: OperationDirection;
  defaultPaymentMethod: PaymentMethod;
}

// --- Categories ---
export interface CategoryListItem {
  id: number;
  code: string;
  label: string;
  direction: OperationDirection;
  isActive: boolean;
  createdAt: string;
  groupId?: number | null;
  groupName?: string | null;
}

export interface CategoryDetail extends CategoryListItem {
  updatedAt?: string | null;
}

export interface CreateCategoryPayload {
  code: string;
  label: string;
  direction: OperationDirection;
  groupName: string;
}

export interface UpdateCategoryPayload {
  label: string;
  direction: OperationDirection;
  groupName: string;
}

// --- Category groups ---
export interface CategoryGroup {
  id: number;
  name: string;
}

// --- Third parties ---
export interface ThirdParty {
  id: number;
  name: string;
}

// --- Cash sessions ---
export interface CashSessionSummary {
  operationCount: number;
  totalIn: number;
  totalOut: number;
  netMovement: number;
}

export interface CashSessionListItem {
  id: number;
  cashRegisterId: number;
  cashRegisterCode: string;
  cashRegisterName: string;
  openedBy: number;
  openedByName: string;
  openedAt: string;
  openingBalance: number;
  closedBy?: number | null;
  closedAt?: string | null;
  status: CashSessionStatus;
  theoreticalBalance?: number | null;
  physicalBalance?: number | null;
  varianceAmount?: number | null;
}

export interface CashSessionDetail extends CashSessionListItem {
  closedByName?: string | null;
  openComment?: string | null;
  closeComment?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  summary: CashSessionSummary;
}

export interface OpenCashSessionPayload {
  cashRegisterId: number;
  openingBalance: number;
  openComment?: string;
}

export interface CloseCashSessionPayload {
  physicalBalance: number;
  closeComment?: string;
  varianceJustification?: string;
  confirmDeletePending?: boolean;
}

export interface SessionPendingItems {
  pendingOperationCount: number;
  pendingTransferCount: number;
  pendingDepositCount: number;
  pendingOperationRefs: string[];
  pendingTransferRefs: string[];
  pendingDepositRefs: string[];
}

// --- Cash operations ---
export interface CashOperationListItem {
  id: number;
  operationRef: string;
  cashRegisterId: number;
  cashRegisterCode: string;
  cashSessionId: number;
  cashSessionStatus: CashSessionStatus;
  operationDate: string;
  direction: OperationDirection;
  categoryId: number;
  categoryLabel: string;
  amount: number;
  currencyCode: string;
  paymentMethod: PaymentMethod;
  label: string;
  thirdPartyName?: string | null;
  isDeleted: boolean;
  isLocked: boolean;
  isPendingApproval: boolean;
  isPendingCancellation: boolean;
  hasWorkflowHistory: boolean;
}

export interface CashOperationDetail extends CashOperationListItem {
  cashRegisterName: string;
  categoryCode: string;
  description?: string | null;
  externalReference?: string | null;
  createdBy?: number | null;
  createdAt: string;
  updatedBy?: number | null;
  updatedAt?: string | null;
  deletedBy?: number | null;
  deletedAt?: string | null;
  deleteReason?: string | null;
  lockedByType?: string | null;       // "CashTransfer" | "BankDeposit"
  lockedByReference?: string | null;
  lockedById?: number | null;
}

export interface CreateCashOperationPayload {
  cashSessionId: number;
  operationDate: string;
  direction: OperationDirection;
  categoryId: number;
  amount: number;
  paymentMethod: PaymentMethod;
  label: string;
  description?: string;
  externalReference?: string;
  thirdPartyName?: string;
}

export interface UpdateCashOperationPayload {
  operationDate: string;
  categoryId: number;
  amount: number;
  paymentMethod: PaymentMethod;
  label: string;
  description?: string;
  externalReference?: string;
  thirdPartyName?: string;
}

export interface CashOperationFilter {
  cashRegisterId?: number;
  cashSessionId?: number;
  from?: string;
  to?: string;
  direction?: OperationDirection;
  categoryId?: number;
  page?: number;
  pageSize?: number;
  sortBy?: 'date' | 'ref';
  sortDir?: 'asc' | 'desc';
  includeDeleted?: boolean;
}

// --- Dashboard ---
export interface CashSessionWidget {
  id: number;
  cashRegisterId: number;
  cashRegisterName: string;
  openedAt: string;
  openingBalance: number;
  currentTheoreticalBalance: number;
  operationCount: number;
}

export interface OperationWidget {
  id: number;
  operationRef: string;
  operationDate: string;
  direction: string;
  categoryLabel: string;
  amount: number;
  label: string;
}

export interface DailyTrendPoint {
  date: string;
  totalIn: number;
  totalOut: number;
  net: number;
}

export interface CategoryBreakdown {
  categoryLabel: string;
  amount: number;
  operationCount: number;
}

export interface RegisterBreakdown {
  cashRegisterId: number;
  cashRegisterCode: string;
  cashRegisterName: string;
  net: number;
  operationCount: number;
}

export interface CashierDashboard {
  activeSession?: CashSessionWidget | null;
  todayTotalIn: number;
  todayTotalOut: number;
  todayOperationCount: number;
  recentOperations: OperationWidget[];
  trend7Days: DailyTrendPoint[];
  todayCategoryBreakdown: CategoryBreakdown[];
}

export interface SupervisorKpi {
  label: string;
  value: number;
  unit?: string | null;
}

export interface SupervisorDashboard {
  openSessionsCount: number;
  todayClosedSessionsCount: number;
  todayTotalIn: number;
  todayTotalOut: number;
  todayNetMovement: number;
  sessionsWithVarianceCount: number;
  openSessions: CashSessionWidget[];
  kpis: SupervisorKpi[];
  trend14Days: DailyTrendPoint[];
  topCategories30Days: CategoryBreakdown[];
  registerBreakdownToday: RegisterBreakdown[];
  pendingApprovalsCount: number;
  approvalsFeatureEnabled: boolean;
  openVarianceCasesCount: number;
  varianceFeatureEnabled: boolean;
  openAnomaliesCount: number;
  anomaliesFeatureEnabled: boolean;
}

// --- Settings / Features ---
export interface GeneralSettings {
  defaultCurrency: string;
  autoBackupEnabled: boolean;
  autoBackupTime: string;
  autoBackupOnSessionClose: boolean;
  allowOperationEditBeforeSessionClose: boolean;
  allowSupervisorCloseAnySession: boolean;
  operationRefPrefix: string;
  backupDirectory: string;
  openingBalanceDefaultMode: 'ZERO' | 'LAST_CLOSING_PHYSICAL';
  varianceJustificationThreshold: number;
  varianceForceJustificationBelowThreshold: boolean;
  varianceTrackAllNonZero: boolean;
  showDeletedOperationsInList: boolean;
  receiptCopiesCount: 1 | 2;
}

export interface CompanyInfo {
  name?: string | null;
  legalForm?: string | null;
  address?: string | null;
  city?: string | null;
  country?: string | null;
  phone?: string | null;
  email?: string | null;
  website?: string | null;
  registrationNumber?: string | null;
  taxId?: string | null;
  logoPath?: string | null;
}

export interface OpeningDefault {
  cashRegisterId: number;
  defaultOpeningBalance: number;
  source: 'ZERO' | 'LAST_CLOSING_PHYSICAL' | 'NO_PREVIOUS_SESSION';
}

export interface FeatureSetting {
  id: number;
  featureCode: string;
  featureName: string;
  isEnabled: boolean;
  updatedAt?: string | null;
}

export interface UpdateFeatureSettings {
  features: { featureCode: string; isEnabled: boolean }[];
}

// --- Backups ---
export interface BackupListItem {
  id: number;
  fileName: string;
  filePath: string;
  createdBy?: number | null;
  createdByName?: string | null;
  createdAt: string;
  sizeBytes?: number | null;
}

// --- Exports ---
export interface ExportOperationsRequest {
  from: string;
  to: string;
  cashRegisterId?: number;
  format: 'xlsx' | 'pdf';
  direction?: OperationDirection;
  includeDeleted?: boolean;
}

export interface ExportSessionsRequest {
  from: string;
  to: string;
  cashRegisterId?: number;
  format: 'xlsx' | 'pdf';
}

export interface ExportCashStateRequest {
  cashSessionId: number;
}
