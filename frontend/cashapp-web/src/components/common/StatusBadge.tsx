import { Chip } from '@mui/material';

type ChipColor = 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning';

// Mapping centralisé statut/enum -> couleur MUI.
const MAP: Record<string, ChipColor> = {
  // ApprovalStatus / generic statuses
  PENDING: 'warning',
  PENDING_APPROVAL: 'warning',
  APPROVED: 'success',
  REJECTED: 'error',
  CANCELLED: 'default',
  COMPLETED: 'success',
  DRAFT: 'default',
  // AnomalySeverity
  LOW: 'info',
  MEDIUM: 'warning',
  HIGH: 'error',
  CRITICAL: 'error',
  // AnomalyStatus
  OPEN: 'warning',
  IN_REVIEW: 'info',
  RESOLVED: 'success',
  CLOSED: 'default',
  // VarianceStatus
  JUSTIFIED: 'info',
  // ImportBatchStatus
  UPLOADED: 'info',
  PREVIEWED: 'warning',
  CONFIRMED: 'success',
  PARTIAL: 'warning',
  FAILED: 'error',
  // ReconciliationMatchStatus
  MATCHED: 'success',
  UNMATCHED: 'warning',
  DISPUTED: 'error',
  // CashSessionStatus + others
  IN: 'success',
  OUT: 'warning',
  // AccountingGenerationStatus
  GENERATED: 'info',
  EXPORTED: 'success',
  // AccountingExportStatus
  DOWNLOADED: 'success',
  DELETED: 'default',
  // AuditAction (OPEN est déjà mappé ci-dessus, réutilisé tel quel)
  LOGIN: 'success',
  LOGOUT: 'default',
  CREATE: 'success',
  UPDATE: 'info',
  DELETE: 'error',
  CLOSE: 'default',
  CANCEL: 'warning',
  APPROVE: 'success',
  REJECT: 'error',
  ASSIGN: 'info',
  RESOLVE: 'success',
  COMPLETE: 'success',
  CONFIG_CHANGE: 'warning',
  FEATURE_TOGGLE: 'secondary'
};

interface Props {
  value: string;
  label?: string;
  size?: 'small' | 'medium';
}

export function StatusBadge({ value, label, size = 'small' }: Props) {
  const color = MAP[value] ?? 'default';
  return <Chip label={label ?? value} color={color} size={size} variant="filled" />;
}
