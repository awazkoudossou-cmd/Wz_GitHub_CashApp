import { Alert, Button, Card, CardContent, Grid, Stack, Typography } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import CancelIcon from '@mui/icons-material/Cancel';
import { useNavigate, useParams } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatusBadge } from '@/components/common/StatusBadge';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { AttachmentPanel } from '@/components/common/AttachmentPanel';
import { useCancelCashTransfer, useCashTransfer, useCompleteCashTransfer } from '@/modules/cash-transfers-v2/hooks';
import { CashTransferStatus } from '@/types/v2Enums';
import { formatDate } from '@/utils/format';
import { extractErrorMessage } from '@/api/client';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Grid item xs={12} sm={6} md={4}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography>{value ?? '—'}</Typography>
    </Grid>
  );
}

export function CashTransferDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const trId = Number(id);
  const { data, isLoading } = useCashTransfer(trId);
  const complete = useCompleteCashTransfer();
  const cancel = useCancelCashTransfer();

  if (isLoading) return <LoadingScreen />;
  if (!data) return <PageContainer><Alert severity="error">Transfert introuvable.</Alert></PageContainer>;

  const canComplete = data.status === CashTransferStatus.APPROVED || data.status === CashTransferStatus.DRAFT;
  const canCancel = data.status !== CashTransferStatus.COMPLETED && data.status !== CashTransferStatus.CANCELLED;

  return (
    <PageContainer maxWidth="md">
      <PageHeader
        title={`Transfert ${data.transferRef}`}
        subtitle={`${data.sourceCashRegisterCode} → ${data.destinationCashRegisterCode}`}
        actions={
          <Stack direction="row" spacing={1}>
            <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/cash-transfers')}>Retour</Button>
            <Button
              variant="contained" color="success" startIcon={<PlayArrowIcon />} disabled={!canComplete}
              onClick={async () => {
                try { await complete.mutateAsync(data.id); }
                catch (e) { alert(extractErrorMessage(e)); }
              }}
            >
              Finaliser
            </Button>
            <Button
              color="error" startIcon={<CancelIcon />} disabled={!canCancel}
              onClick={async () => {
                const reason = window.prompt('Motif d\'annulation ?');
                if (!reason) return;
                try { await cancel.mutateAsync({ id: data.id, reason }); }
                catch (e) { alert(extractErrorMessage(e)); }
              }}
            >
              Annuler
            </Button>
          </Stack>
        }
      />

      {data.status === CashTransferStatus.PENDING_APPROVAL && (
        <Alert severity="info" sx={{ mb: 2 }}>
          En attente d'approbation
          {data.approvalRequestId && (
            <> — <Button size="small" onClick={() => navigate(`/approval-requests/${data.approvalRequestId}`)}>Voir la demande #{data.approvalRequestId}</Button></>
          )}
        </Alert>
      )}

      <Card>
        <CardContent>
          <Grid container spacing={2}>
            <Field label="Statut" value={<StatusBadge value={data.status} />} />
            <Field label="Date" value={formatDate(data.transferDate)} />
            <Field label="Montant" value={<CurrencyDisplay value={data.amount} currency={data.currencyCode} />} />
            <Field label="Source" value={`${data.sourceCashRegisterCode} — ${data.sourceCashRegisterName}`} />
            <Field label="Destination" value={`${data.destinationCashRegisterCode} — ${data.destinationCashRegisterName}`} />
            <Field label="Demandé par" value={`${data.requestedByName} — ${formatDate(data.createdAt, true)}`} />
            <Field label="Approuvé par" value={data.approvedByName ? `${data.approvedByName} — ${formatDate(data.approvedAt, true)}` : '—'} />
            <Field label="Finalisé le" value={formatDate(data.completedAt, true)} />
            <Field label="Annulé le" value={formatDate(data.cancelledAt, true)} />
            <Grid item xs={12}>
              <Typography variant="caption" color="text.secondary">Motif</Typography>
              <Typography>{data.reason}</Typography>
            </Grid>
            {data.sourceOperationRef && (
              <Field label="Opération source" value={data.sourceOperationRef} />
            )}
            {data.destinationOperationRef && (
              <Field label="Opération destination" value={data.destinationOperationRef} />
            )}
          </Grid>
        </CardContent>
      </Card>

      <AttachmentPanel entityType="CashTransfer" entityId={data.id} />
    </PageContainer>
  );
}
