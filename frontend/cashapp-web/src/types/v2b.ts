import type {
  AnomalySeverity,
  AnomalyStatus,
  ApprovalStatus,
  ApprovalTargetType,
  BankDepositStatus,
  CashTransferStatus,
  ImportBatchStatus,
  ImportBatchType,
  ImportLineStatus,
  ReconciliationBatchType,
  ReconciliationMatchStatus,
  ReconciliationStatus,
  VarianceStatus
} from './v2Enums';
import type { OperationDirection } from './enums';

// === Attachments ===

export interface AttachmentDto {
  id: number;
  fileName: string;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  entityType: string;
  entityId: number;
  uploadedBy: number;
  uploadedByName: string;
  uploadedAt: string;
  description?: string | null;
}

// === Imports ===

export interface ImportBatchListItem {
  id: number;
  batchRef: string;
  batchType: ImportBatchType;
  originalFileName: string;
  uploadedBy: number;
  uploadedByName: string;
  uploadedAt: string;
  status: ImportBatchStatus;
  totalLines: number;
  validLines: number;
  invalidLines: number;
  importedLines: number;
  cashRegisterId?: number | null;
}

export interface ImportPreviewLine {
  lineNumber: number;
  status: ImportLineStatus;
  errorMessage?: string | null;
  rawDataJson: string;
  parsedDataJson?: string | null;
  createdEntityId?: number | null;
}

export interface ImportBatchDetail extends ImportBatchListItem {
  cashRegisterCode?: string | null;
  errorSummaryJson?: string | null;
  lines: ImportPreviewLine[];
}

export interface ImportPreview {
  batchId: number;
  totalLines: number;
  validLines: number;
  invalidLines: number;
  lines: ImportPreviewLine[];
}

// === Reconciliation ===

export interface ReconciliationBatchListItem {
  id: number;
  reference: string;
  batchType: ReconciliationBatchType;
  cashRegisterId?: number | null;
  cashRegisterCode?: string | null;
  createdBy: number;
  createdByName: string;
  status: ReconciliationStatus;
  createdAt: string;
}

export interface ReconciliationItemDto {
  id: number;
  leftEntityType: string;
  leftEntityId: number;
  rightEntityType?: string | null;
  rightEntityId?: number | null;
  matchedAmount?: number | null;
  matchStatus: ReconciliationMatchStatus;
  notes?: string | null;
}

export interface ReconciliationBatchDetail extends ReconciliationBatchListItem {
  notes?: string | null;
  items: ReconciliationItemDto[];
}

export interface CreateReconciliationBatchPayload {
  batchType: ReconciliationBatchType;
  cashRegisterId?: number;
  notes?: string;
}

export interface ReconcilePairPayload {
  leftEntityType: string;
  leftEntityId: number;
  rightEntityType?: string;
  rightEntityId?: number;
  matchedAmount?: number;
  notes?: string;
}

export interface ReconcileItemsPayload {
  pairs: ReconcilePairPayload[];
  closeAfter: boolean;
}

// === Reports ===

export interface CashReportFilter {
  from: string;
  to: string;
  cashRegisterId?: number;
}

export interface CashReportRow {
  cashRegisterId: number;
  cashRegisterCode: string;
  totalIn: number;
  totalOut: number;
  netMovement: number;
  operationCount: number;
}

export interface CashReportSummary {
  totalIn: number;
  totalOut: number;
  net: number;
  operationCount: number;
}

export interface CashReportResult {
  summary: CashReportSummary;
  rows: CashReportRow[];
}

export interface CategoryReportFilter extends CashReportFilter {
  direction?: OperationDirection;
}

export interface CategoryReportRow {
  categoryId: number;
  categoryCode: string;
  categoryLabel: string;
  direction: OperationDirection;
  total: number;
  count: number;
}

export interface CategoryReportResult {
  rows: CategoryReportRow[];
}

export interface VarianceReportFilter extends CashReportFilter {
  status?: VarianceStatus;
}

export interface VarianceReportRow {
  varianceCaseId: number;
  cashSessionId: number;
  cashRegisterId: number;
  cashRegisterCode: string;
  varianceAmount: number;
  status: VarianceStatus;
  detectedAt: string;
}

export interface VarianceReportResult {
  rows: VarianceReportRow[];
}

export interface TransferReportFilter extends CashReportFilter {
  status?: CashTransferStatus;
}

export interface TransferReportRow {
  id: number;
  transferRef: string;
  sourceCode: string;
  destinationCode: string;
  amount: number;
  currencyCode: string;
  status: CashTransferStatus;
  transferDate: string;
}

export interface TransferReportResult { rows: TransferReportRow[]; }

export interface DepositReportFilter extends CashReportFilter {
  status?: BankDepositStatus;
}

export interface DepositReportRow {
  id: number;
  depositRef: string;
  cashRegisterCode: string;
  bankName: string;
  amount: number;
  currencyCode: string;
  status: BankDepositStatus;
  depositDate: string;
}

export interface DepositReportResult { rows: DepositReportRow[]; }

export interface AnomalyReportFilter extends CashReportFilter {
  status?: AnomalyStatus;
  severity?: AnomalySeverity;
}

export interface AnomalyReportRow {
  id: number;
  reference: string;
  severity: AnomalySeverity;
  status: AnomalyStatus;
  cashRegisterCode?: string | null;
  detectedAt: string;
}

export interface AnomalyReportResult { rows: AnomalyReportRow[]; }

export interface ApprovalReportFilter {
  from: string;
  to: string;
  status?: ApprovalStatus;
  targetType?: ApprovalTargetType;
}

export interface ApprovalReportRow {
  id: number;
  requestRef: string;
  targetType: ApprovalTargetType;
  targetEntityType: string;
  status: ApprovalStatus;
  amount?: number | null;
  requestedAt: string;
}

export interface ApprovalReportResult { rows: ApprovalReportRow[]; }
