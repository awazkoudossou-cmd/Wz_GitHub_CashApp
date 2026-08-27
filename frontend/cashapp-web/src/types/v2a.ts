import type {
  AnomalySeverity,
  AnomalyStatus,
  ApprovalStatus,
  ApprovalTargetType,
  AuditAction,
  BankDepositStatus,
  CashTransferStatus,
  VarianceStatus
} from './v2Enums';

// === Approval Rules ===

export interface ApprovalRule {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  targetType: ApprovalTargetType;
  amountThreshold?: number | null;
  currencyCode?: string | null;
  requiredApproverRole: string;
  isBlocking: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateApprovalRulePayload {
  code: string;
  name: string;
  description?: string;
  targetType: ApprovalTargetType;
  amountThreshold?: number | null;
  currencyCode?: string | null;
  requiredApproverRole: string;
  isBlocking: boolean;
}

export interface UpdateApprovalRulePayload {
  name: string;
  description?: string;
  amountThreshold?: number | null;
  currencyCode?: string | null;
  requiredApproverRole: string;
  isBlocking: boolean;
}

// === Approval Requests ===

export interface ApprovalRequestListItem {
  id: number;
  requestRef: string;
  targetType: ApprovalTargetType;
  targetEntityType: string;
  targetEntityId: number;
  cashRegisterId?: number | null;
  cashRegisterCode?: string | null;
  amount?: number | null;
  currencyCode?: string | null;
  status: ApprovalStatus;
  requestedBy: number;
  requestedByName: string;
  requestedAt: string;
  decidedBy?: number | null;
  decidedByName?: string | null;
  decidedAt?: string | null;
  reason: string;
  cashSessionId?: number | null;
}

export interface ApprovalAction {
  id: number;
  actionType: AuditAction;
  performedBy: number;
  performedByName: string;
  performedAt: string;
  comment?: string | null;
}

export interface ApprovalRequestDetail extends ApprovalRequestListItem {
  approvalRuleId: number;
  approvalRuleCode: string;
  decisionComment?: string | null;
  createdAt: string;
  actions: ApprovalAction[];
}

export interface ApprovalRequestFilter {
  status?: ApprovalStatus;
  targetType?: ApprovalTargetType;
  cashRegisterId?: number;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
  sortBy?: 'date' | 'amount';
  sortDir?: 'asc' | 'desc';
}

// === Cash Transfers ===

export interface CashTransferListItem {
  id: number;
  transferRef: string;
  sourceCashRegisterId: number;
  sourceCashRegisterCode: string;
  destinationCashRegisterId: number;
  destinationCashRegisterCode: string;
  amount: number;
  currencyCode: string;
  transferDate: string;
  status: CashTransferStatus;
  requestedBy: number;
  requestedByName: string;
  createdAt: string;
}

export interface CashTransferDetail extends CashTransferListItem {
  sourceCashRegisterName: string;
  sourceSessionId?: number | null;
  destinationCashRegisterName: string;
  destinationSessionId?: number | null;
  reason: string;
  approvedBy?: number | null;
  approvedByName?: string | null;
  approvedAt?: string | null;
  completedAt?: string | null;
  cancelledAt?: string | null;
  sourceOperationId?: number | null;
  sourceOperationRef?: string | null;
  destinationOperationId?: number | null;
  destinationOperationRef?: string | null;
  approvalRequestId?: number | null;
  updatedAt?: string | null;
}

export interface CreateCashTransferPayload {
  sourceCashRegisterId: number;
  destinationCashRegisterId: number;
  amount: number;
  currencyCode: string;
  transferDate: string;
  reason: string;
}

export interface CashTransferFilter {
  status?: CashTransferStatus;
  sourceCashRegisterId?: number;
  destinationCashRegisterId?: number;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

// === Bank Deposits ===

export interface BankDepositListItem {
  id: number;
  depositRef: string;
  cashRegisterId: number;
  cashRegisterCode: string;
  depositDate: string;
  amount: number;
  currencyCode: string;
  bankName: string;
  status: BankDepositStatus;
  requestedBy: number;
  requestedByName: string;
  createdAt: string;
}

export interface BankDepositDetail extends BankDepositListItem {
  cashRegisterName: string;
  cashSessionId?: number | null;
  accountReference?: string | null;
  depositSlipReference?: string | null;
  description?: string | null;
  approvedBy?: number | null;
  approvedByName?: string | null;
  approvedAt?: string | null;
  completedAt?: string | null;
  cancelledAt?: string | null;
  linkedOperationId?: number | null;
  linkedOperationRef?: string | null;
  approvalRequestId?: number | null;
  updatedAt?: string | null;
}

export interface CreateBankDepositPayload {
  cashRegisterId: number;
  depositDate: string;
  amount: number;
  currencyCode: string;
  bankName: string;
  accountReference?: string;
  depositSlipReference?: string;
  description?: string;
}

export interface BankDepositFilter {
  status?: BankDepositStatus;
  cashRegisterId?: number;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

// === Anomalies ===

export interface AnomalyListItem {
  id: number;
  reference: string;
  severity: AnomalySeverity;
  status: AnomalyStatus;
  title: string;
  cashRegisterId?: number | null;
  cashRegisterCode?: string | null;
  detectedAt: string;
  assignedTo?: number | null;
  assignedToName?: string | null;
}

export interface AnomalyComment {
  id: number;
  authorId: number;
  authorName: string;
  body: string;
  createdAt: string;
}

export interface AnomalyDetail extends AnomalyListItem {
  description?: string | null;
  relatedEntityType?: string | null;
  relatedEntityId?: number | null;
  cashSessionId?: number | null;
  detectedBy?: number | null;
  detectedByName?: string | null;
  assignedAt?: string | null;
  resolvedAt?: string | null;
  resolvedBy?: number | null;
  resolvedByName?: string | null;
  resolutionComment?: string | null;
  comments: AnomalyComment[];
}

export interface CreateAnomalyPayload {
  severity: AnomalySeverity;
  title: string;
  description?: string;
  relatedEntityType?: string;
  relatedEntityId?: number;
  cashRegisterId?: number;
  cashSessionId?: number;
}

export interface AnomalyFilter {
  status?: AnomalyStatus;
  severity?: AnomalySeverity;
  cashRegisterId?: number;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

// === Variances ===

export interface VarianceJustification {
  id: number;
  providedBy: number;
  providedByName: string;
  providedAt: string;
  comment: string;
}

export interface VarianceAction {
  id: number;
  actionType: string;
  performedBy: number;
  performedByName: string;
  performedAt: string;
  comment?: string | null;
}

export interface VarianceListItem {
  id: number;
  cashSessionId: number;
  cashRegisterId: number;
  cashRegisterCode: string;
  cashRegisterName: string;
  varianceAmount: number;
  currencyCode: string;
  status: VarianceStatus;
  detectedAt: string;
  approvalRequestId?: number | null;
  anomalyCaseId?: number | null;
  sessionOpenedAt: string;
  sessionClosedAt?: string | null;
  sessionOpenedByName: string;
  sessionClosedByName?: string | null;
  sessionOpeningBalance: number;
  sessionTheoreticalBalance?: number | null;
  sessionPhysicalBalance?: number | null;
}

export interface VarianceDetail extends VarianceListItem {
  closedAt?: string | null;
  sessionCloseComment?: string | null;
  justifications: VarianceJustification[];
  actions: VarianceAction[];
}

export interface VarianceFilter {
  status?: VarianceStatus;
  cashRegisterId?: number;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

// === Audit Logs ===

export interface AuditLogListItem {
  id: number;
  actionType: AuditAction;
  entityType: string;
  entityId?: number | null;
  performedBy?: number | null;
  performedByName?: string | null;
  performedAt: string;
  description?: string | null;
  amount?: number | null;
  currencyCode?: string | null;
}

export interface AuditLogDetail extends AuditLogListItem {
  oldValuesJson?: string | null;
  newValuesJson?: string | null;
  metadataJson?: string | null;
  ipAddress?: string | null;
}

export interface AuditLogFilter {
  actionType?: AuditAction;
  entityType?: string;
  entityId?: number;
  performedBy?: number;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}
