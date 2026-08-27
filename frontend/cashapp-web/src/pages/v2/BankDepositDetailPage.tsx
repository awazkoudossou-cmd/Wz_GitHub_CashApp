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
import { useBankDeposit, useCancelBankDeposit, useCompleteBankDeposit } from '@/modules/bank-deposits/hooks';
import { BankDepositStatus } from '@/types/v2Enums';
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

export function BankDepositDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const dId = Number(id);
  const { data, isLoading } = useBankDeposit(dId);
  const complete = useCompleteBankDeposit();
  const cancel = useCancelBankDeposit();

  if (isLoading) return <LoadingScreen />;
  if (!data) return <PageContainer><Alert severity="error">Dépôt introuvable.</Alert></PageContainer>;

  const canComplete = data.status === BankDepositStatus.APPROVED || data.status === BankDepositStatus.DRAFT;
  const canCancel = data.status !== BankDepositStatus.COMPLETED && data.status !== BankDepositStatus.CANCELLED;

  return (
    <PageContainer maxWidth="md">
      <PageHeader
        title={`Dépôt ${data.depositRef}`}
        subtitle={`${data.cashRegisterCode} → ${data.bankName}`}
        actions={
          <Stack direction="row" spacing={1}>
            <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/bank-deposits')}>Retour</Button>
            <Button
              variant="contained" color="success" startIcon={<PlayArrowIcon />} disabled={!canComplete}
              onClick={async () => {
                try { await complete.mutateAsync(data.id); }
                catch (e) { alert(extractErrorMessage(e)); }
              }}
            >Finaliser</Button>
            <Button
              color="error" startIcon={<CancelIcon />} disabled={!canCancel}
              onClick={async () => {
                const reason = window.prompt('Motif d\'annulation ?');
                if (!reason) return;
                try { await cancel.mutateAsync({ id: data.id, reason }); }
                catch (e) { alert(extractErrorMessage(e)); }
              }}
            >Annuler</Button>
          </Stack>
        }
      />

      {data.status === BankDepositStatus.PENDING_APPROVAL && (
        <Alert severity="info" sx={{ mb: 2 }}>
          En attente d'approbation
          {data.approvalRequestId && (
            <> — <Button size="small" onClick={() => navigate(`/approval-requests/${data.approvalRequestId}`)}>Voir la demande</Button></>
          )}
        </Alert>
      )}

      <Card>
        <CardContent>
          <Grid container spacing={2}>
            <Field label="Statut" value={<StatusBadge value={data.status} />} />
            <Field label="Date" value={formatDate(data.depositDate)} />
            <Field label="Montant" value={<CurrencyDisplay value={data.amount} currency={data.currencyCode} />} />
            <Field label="Caisse" value={`${data.cashRegisterCode} — ${data.cashRegisterName}`} />
            <Field label="Banque" value={data.bankName} />
            <Field label="N° compte" value={data.accountReference} />
            <Field label="Réf. bordereau" value={data.depositSlipReference} />
            <Field label="Demandé par" value={`${data.requestedByName} — ${formatDate(data.createdAt, true)}`} />
            <Field label="Approuvé par" value={data.approvedByName ? `${data.approvedByName} — ${formatDate(data.approvedAt, true)}` : '—'} />
            <Field label="Finalisé le" value={formatDate(data.completedAt, true)} />
            {data.description && (
              <Grid item xs={12}>
                <Typography variant="caption" color="text.secondary">Description</Typography>
                <Typography>{data.description}</Typography>
              </Grid>
            )}
            {data.linkedOperationRef && (
              <Field label="Opération liée" value={data.linkedOperationRef} />
            )}
          </Grid>
        </CardContent>
      </Card>

      <AttachmentPanel entityType="BankDeposit" entityId={data.id} />
    </PageContainer>
  );
}
