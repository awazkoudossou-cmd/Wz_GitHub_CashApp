import { useState } from 'react';
import { Alert, Box, Button, Card, CardContent, Grid, Stack, TextField, Typography } from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useNavigate, useParams } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatusBadge } from '@/components/common/StatusBadge';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { ApprovalTimeline } from '@/components/common/ApprovalTimeline';
import { AttachmentPanel } from '@/components/common/AttachmentPanel';
import { useApprovalRequest, useApproveRequest, useRejectRequest } from '@/modules/approvals/hooks';
import { formatDate } from '@/utils/format';
import { extractErrorMessage } from '@/api/client';
import { ApprovalStatus } from '@/types/v2Enums';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Grid item xs={12} sm={6} md={4}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography>{value ?? '—'}</Typography>
    </Grid>
  );
}

export function ApprovalRequestDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const reqId = Number(id);
  const { data, isLoading } = useApprovalRequest(reqId);
  const approve = useApproveRequest();
  const reject = useRejectRequest();
  const [comment, setComment] = useState('');

  if (isLoading) return <LoadingScreen />;
  if (!data) return <PageContainer><Alert severity="error">Demande introuvable.</Alert></PageContainer>;

  const pending = data.status === ApprovalStatus.PENDING;

  return (
    <PageContainer maxWidth="md">
      <PageHeader
        title={`Demande ${data.requestRef}`}
        subtitle={`${data.targetType} sur ${data.targetEntityType} #${data.targetEntityId}`}
        actions={
          <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/approval-requests')}>
            Retour
          </Button>
        }
      />
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Field label="Statut" value={<StatusBadge value={data.status} />} />
            <Field label="Règle" value={data.approvalRuleCode} />
            <Field label="Cible" value={`${data.targetEntityType} #${data.targetEntityId}`} />
            <Field label="Caisse" value={data.cashRegisterCode} />
            <Field label="Montant" value={<CurrencyDisplay value={data.amount ?? undefined} currency={data.currencyCode ?? 'XOF'} />} />
            <Field label="Demandé par" value={`${data.requestedByName} — ${formatDate(data.requestedAt, true)}`} />
            <Field label="Décidé par" value={data.decidedByName ? `${data.decidedByName} — ${formatDate(data.decidedAt, true)}` : '—'} />
            <Grid item xs={12}>
              <Typography variant="caption" color="text.secondary">Motif de la demande</Typography>
              <Typography>{data.reason}</Typography>
            </Grid>
            {data.decisionComment && (
              <Grid item xs={12}>
                <Typography variant="caption" color="text.secondary">Commentaire de décision</Typography>
                <Typography>{data.decisionComment}</Typography>
              </Grid>
            )}
          </Grid>
        </CardContent>
      </Card>

      {pending && (
        <Card sx={{ mb: 2 }}>
          <CardContent>
            <Stack spacing={2}>
              <TextField
                multiline rows={2} fullWidth size="small"
                label="Commentaire (obligatoire en cas de rejet)"
                value={comment}
                onChange={(e) => setComment(e.target.value)}
              />
              <Stack direction="row" spacing={2}>
                <Button
                  variant="contained" color="success" startIcon={<CheckCircleIcon />}
                  disabled={approve.isPending}
                  onClick={async () => {
                    try { await approve.mutateAsync({ id: data.id, comment: comment.trim() || undefined }); }
                    catch (e) { alert(extractErrorMessage(e)); }
                  }}
                >
                  Approuver
                </Button>
                <Button
                  variant="outlined" color="error" startIcon={<CancelIcon />}
                  disabled={reject.isPending || comment.trim().length === 0}
                  onClick={async () => {
                    try { await reject.mutateAsync({ id: data.id, comment: comment.trim() }); }
                    catch (e) { alert(extractErrorMessage(e)); }
                  }}
                >
                  Rejeter
                </Button>
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardContent>
          <Box mb={1}><b>Historique</b></Box>
          <ApprovalTimeline actions={data.actions} />
        </CardContent>
      </Card>

      <AttachmentPanel entityType="ApprovalRequest" entityId={data.id} />
    </PageContainer>
  );
}
