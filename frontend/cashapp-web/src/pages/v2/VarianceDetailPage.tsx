import { useState } from 'react';
import { Alert, Box, Button, Card, CardContent, Grid, Stack, TextField, Typography } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useNavigate, useParams } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatusBadge } from '@/components/common/StatusBadge';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { useJustifyVariance, useVariance } from '@/modules/variances-v2/hooks';
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

export function VarianceDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const vId = Number(id);
  const { data, isLoading } = useVariance(vId);
  const justify = useJustifyVariance();
  const [comment, setComment] = useState('');

  if (isLoading) return <LoadingScreen />;
  if (!data) return <PageContainer><Alert severity="error">Écart introuvable.</Alert></PageContainer>;

  return (
    <PageContainer maxWidth="md">
      <PageHeader
        title={`Écart #${data.id}`}
        subtitle={`Caisse ${data.cashRegisterCode} — Session #${data.cashSessionId}`}
        actions={<Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/variances')}>Retour</Button>}
      />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Field label="Statut" value={<StatusBadge value={data.status} />} />
            <Field label="Écart" value={<CurrencyDisplay value={data.varianceAmount} currency={data.currencyCode} />} />
            <Field label="Détecté le" value={formatDate(data.detectedAt, true)} />
            <Field label="Demande d'approbation" value={data.approvalRequestId ? `#${data.approvalRequestId}` : '—'} />
            <Field label="Anomalie" value={data.anomalyCaseId ? `#${data.anomalyCaseId}` : '—'} />
            <Field label="Fermé le" value={formatDate(data.closedAt, true)} />
          </Grid>
        </CardContent>
      </Card>

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Box mb={1}><b>Session #{data.cashSessionId} — {data.cashRegisterCode} {data.cashRegisterName}</b></Box>
          <Grid container spacing={2}>
            <Field label="Ouverte le" value={formatDate(data.sessionOpenedAt, true)} />
            <Field label="Ouverte par" value={data.sessionOpenedByName} />
            <Field label="Fermée le" value={data.sessionClosedAt ? formatDate(data.sessionClosedAt, true) : '—'} />
            <Field label="Fermée par" value={data.sessionClosedByName} />
            <Field label="Solde d'ouverture" value={<CurrencyDisplay value={data.sessionOpeningBalance} currency={data.currencyCode} />} />
            <Field label="Solde théorique" value={<CurrencyDisplay value={data.sessionTheoreticalBalance ?? undefined} currency={data.currencyCode} />} />
            <Field label="Solde physique" value={<CurrencyDisplay value={data.sessionPhysicalBalance ?? undefined} currency={data.currencyCode} />} />
            {data.sessionCloseComment && (
              <Grid item xs={12}>
                <Typography variant="caption" color="text.secondary">Commentaire de clôture</Typography>
                <Typography sx={{ whiteSpace: 'pre-line' }}>{data.sessionCloseComment}</Typography>
              </Grid>
            )}
          </Grid>
        </CardContent>
      </Card>

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Box mb={1}><b>Justifications</b></Box>
          {data.justifications.length === 0 && <Typography variant="body2" color="text.secondary">Aucune justification.</Typography>}
          <Stack spacing={1.5}>
            {data.justifications.map((j) => (
              <Box key={j.id}>
                <Typography variant="body2"><b>{j.providedByName}</b> — {formatDate(j.providedAt, true)}</Typography>
                <Typography>{j.comment}</Typography>
              </Box>
            ))}
          </Stack>
          <Stack direction="row" spacing={1} mt={2}>
            <TextField fullWidth size="small" placeholder="Ajouter une justification…" value={comment} onChange={(e) => setComment(e.target.value)} multiline rows={2} />
            <Button variant="contained" disabled={justify.isPending || !comment.trim()}
              onClick={async () => {
                try { await justify.mutateAsync({ id: vId, comment: comment.trim() }); setComment(''); }
                catch (e) { alert(extractErrorMessage(e)); }
              }}>Justifier</Button>
          </Stack>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Box mb={1}><b>Historique des actions</b></Box>
          {data.actions.length === 0 && <Typography variant="body2" color="text.secondary">Aucune action.</Typography>}
          <Stack spacing={1.5}>
            {data.actions.map((a) => (
              <Box key={a.id}>
                <Typography variant="body2"><b>{a.performedByName}</b> — {a.actionType} — {formatDate(a.performedAt, true)}</Typography>
                {a.comment && <Typography>{a.comment}</Typography>}
              </Box>
            ))}
          </Stack>
        </CardContent>
      </Card>
    </PageContainer>
  );
}
