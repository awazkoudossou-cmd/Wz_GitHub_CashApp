import { Alert, Button, Card, CardContent, Grid, Stack, Typography } from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import CancelIcon from '@mui/icons-material/Cancel';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong';
import { downloadOperationReceipt } from '@/api/exportsApi';
import { useNotificationStore } from '@/app/store/notificationStore';
import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatusChip } from '@/components/common/StatusChip';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { ConfirmDialog } from '@/components/dialogs/ConfirmDialog';
import { useCancelCashOperation, useCashOperation } from '@/modules/cash-operations/hooks';
import { formatDate } from '@/utils/format';
import { CashSessionStatus } from '@/types/enums';
import { extractErrorMessage } from '@/api/client';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Grid item xs={12} sm={6} md={4}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography>{value ?? '—'}</Typography>
    </Grid>
  );
}

export function CashOperationDetailPage() {
  const navigate = useNavigate();
  const { id } = useParams();
  const opId = Number(id);
  const { data, isLoading } = useCashOperation(opId);
  const cancel = useCancelCashOperation();
  const [confirm, setConfirm] = useState(false);
  const [reason, setReason] = useState('');

  if (isLoading) return <LoadingScreen />;
  if (!data) return <PageContainer><Alert severity="error">Opération introuvable.</Alert></PageContainer>;

  const sessionOpen = data.cashSessionStatus === CashSessionStatus.OPEN;
  // Une op passée par le workflow d'approbation (PENDING/APPROVED/REJECTED/CANCELLED) est figée.
  // Écriture de contrepassation (montant négatif) → ni modifiable, ni annulable.
  const isReversal = data.amount < 0;
  const editable = sessionOpen && !data.isDeleted && !data.isLocked && !data.isPendingApproval && !data.isPendingCancellation && !data.hasWorkflowHistory && !isReversal;
  // L'annulation reste possible sur une opération encore PENDING_APPROVAL (annulation directe côté API).
  const cancellable = sessionOpen && !data.isDeleted && !data.isLocked && !data.isPendingCancellation && !isReversal;

  const lockedDetail = data.isLocked
    ? (data.lockedByType === 'CashTransfer'
        ? { label: 'transfert inter-caisses', ref: data.lockedByReference, path: data.lockedById ? `/cash-transfers/${data.lockedById}` : undefined }
        : data.lockedByType === 'BankDeposit'
          ? { label: 'dépôt banque', ref: data.lockedByReference, path: data.lockedById ? `/bank-deposits/${data.lockedById}` : undefined }
          : { label: 'une opération système', ref: data.lockedByReference, path: undefined })
    : null;

  return (
    <PageContainer maxWidth="md">
      <PageHeader
        title={`Opération ${data.operationRef}`}
        subtitle={`Session #${data.cashSessionId} — ${data.cashRegisterName}`}
        actions={
          <Stack direction="row" spacing={1}>
            <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/cash-operations')}>Retour</Button>
            <Button
              startIcon={<ReceiptLongIcon />}
              variant="outlined"
              onClick={async () => {
                try {
                  await downloadOperationReceipt(data.id);
                  useNotificationStore.getState().notifySuccess('Reçu PDF généré.');
                } catch (e) {
                  useNotificationStore.getState().notifyError(extractErrorMessage(e));
                }
              }}
            >
              Reçu PDF
            </Button>
            <Button
              startIcon={<EditIcon />}
              variant="contained"
              disabled={!editable}
              onClick={() => navigate(`/cash-operations/${data.id}/edit`)}
            >
              Modifier
            </Button>
            <Button
              startIcon={<CancelIcon />}
              color="error"
              variant="outlined"
              disabled={!cancellable}
              onClick={() => { setReason(''); setConfirm(true); }}
            >
              Annuler
            </Button>
          </Stack>
        }
      />

      {data.isPendingApproval && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Cette opération est <b>en attente d'approbation</b>. Elle n'impacte pas le solde théorique de la session
          tant que la décision n'est pas prise.
        </Alert>
      )}
      {data.isPendingCancellation && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Une <b>demande d'annulation</b> est en cours d'approbation pour cette opération.
        </Alert>
      )}
      {lockedDetail && (
        <Alert severity="info" sx={{ mb: 2 }}>
          Opération <b>verrouillée</b> car liée à {lockedDetail.label}{lockedDetail.ref ? <> <b>{lockedDetail.ref}</b></> : null}.
          {lockedDetail.path && (
            <Button size="small" sx={{ ml: 1 }} onClick={() => navigate(lockedDetail.path!)}>
              Voir
            </Button>
          )}
          {' '}Toute modification doit être faite sur l'entité d'origine.
        </Alert>
      )}
      {!sessionOpen && !data.isDeleted && !data.isLocked && (
        <Alert severity="info" sx={{ mb: 2 }}>
          La session de cette opération est fermée — édition et annulation désactivées.
        </Alert>
      )}
      {data.isDeleted && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Opération annulée le {formatDate(data.deletedAt, true)}{data.deleteReason ? ` — motif : ${data.deleteReason}` : ''}.
        </Alert>
      )}

      <Card>
        <CardContent>
          <Grid container spacing={2}>
            <Field label="Référence" value={data.operationRef} />
            <Field label="Date" value={formatDate(data.operationDate)} />
            <Field label="Statut session" value={<StatusChip status={data.cashSessionStatus} variant="session" />} />
            <Field label="Caisse" value={`${data.cashRegisterCode} — ${data.cashRegisterName}`} />
            <Field label="Direction" value={<StatusChip status={data.direction} variant="direction" />} />
            <Field label="Catégorie" value={`${data.categoryCode} — ${data.categoryLabel}`} />
            <Field label="Montant" value={<CurrencyDisplay value={data.amount} currency={data.currencyCode} />} />
            <Field label="Moyen de paiement" value={data.paymentMethod} />
            <Field label="Tiers" value={data.thirdPartyName} />
            <Field label="Référence externe" value={data.externalReference} />
            <Field label="Libellé" value={data.label} />
            <Grid item xs={12}>
              <Typography variant="caption" color="text.secondary">Description</Typography>
              <Typography>{data.description ?? '—'}</Typography>
            </Grid>
            <Field label="Créée le" value={formatDate(data.createdAt, true)} />
            <Field label="Modifiée le" value={formatDate(data.updatedAt, true)} />
          </Grid>
        </CardContent>
      </Card>

      <ConfirmDialog
        open={confirm}
        title="Annuler cette opération ?"
        description="L'opération sera supprimée logiquement et le solde de la session sera recalculé."
        destructive
        confirmLabel="Annuler l'opération"
        onClose={() => setConfirm(false)}
        onConfirm={async () => {
          const r = reason.trim() || window.prompt("Motif d'annulation ?") || '';
          if (!r) { setConfirm(false); return; }
          try {
            await cancel.mutateAsync({ id: data.id, reason: r });
            setConfirm(false);
            navigate('/cash-operations');
          } catch (e) {
            alert(extractErrorMessage(e));
          }
        }}
      />
    </PageContainer>
  );
}
