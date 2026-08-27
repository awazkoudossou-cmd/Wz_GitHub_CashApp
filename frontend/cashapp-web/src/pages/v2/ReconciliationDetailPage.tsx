import { useState } from 'react';
import { Alert, Box, Button, Card, CardContent, Grid, Stack, TextField, Typography } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import AddLinkIcon from '@mui/icons-material/AddLink';
import { useNavigate, useParams } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatusBadge } from '@/components/common/StatusBadge';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { useMatchReconciliation, useReconciliation } from '@/modules/reconciliation/hooks';
import { formatDate } from '@/utils/format';
import { extractErrorMessage } from '@/api/client';
import type { ReconciliationItemDto } from '@/types';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Grid item xs={12} sm={6} md={4}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography>{value ?? '—'}</Typography>
    </Grid>
  );
}

export function ReconciliationDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const rId = Number(id);
  const { data, isLoading } = useReconciliation(rId);
  const match = useMatchReconciliation();

  const [leftType, setLeftType] = useState('BankDeposit');
  const [leftId, setLeftId] = useState('');
  const [rightType, setRightType] = useState('CashOperation');
  const [rightId, setRightId] = useState('');
  const [amount, setAmount] = useState('');
  const [notes, setNotes] = useState('');

  if (isLoading) return <LoadingScreen />;
  if (!data) return <PageContainer><Alert severity="error">Batch introuvable.</Alert></PageContainer>;

  const submitMatch = async () => {
    if (!leftType || !leftId) return;
    try {
      await match.mutateAsync({
        id: rId,
        payload: {
          pairs: [{
            leftEntityType: leftType,
            leftEntityId: Number(leftId),
            rightEntityType: rightType || undefined,
            rightEntityId: rightId ? Number(rightId) : undefined,
            matchedAmount: amount ? Number(amount) : undefined,
            notes: notes || undefined
          }],
          closeAfter: false
        }
      });
      setLeftId(''); setRightId(''); setAmount(''); setNotes('');
    } catch (e) { alert(extractErrorMessage(e)); }
  };

  const cols: Column<ReconciliationItemDto>[] = [
    { key: 'left', header: 'Élément gauche', render: (r) => `${r.leftEntityType} #${r.leftEntityId}` },
    { key: 'right', header: 'Élément droit', render: (r) => r.rightEntityType ? `${r.rightEntityType} #${r.rightEntityId}` : '—' },
    { key: 'amt', header: 'Montant', align: 'right', render: (r) => <CurrencyDisplay value={r.matchedAmount ?? undefined} /> },
    { key: 'status', header: 'Statut', render: (r) => <StatusBadge value={r.matchStatus} /> },
    { key: 'notes', header: 'Notes', render: (r) => r.notes ?? '—' }
  ];

  return (
    <PageContainer>
      <PageHeader
        title={`Batch ${data.reference}`}
        subtitle={`${data.batchType}${data.cashRegisterCode ? ` — caisse ${data.cashRegisterCode}` : ''}`}
        actions={<Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/reconciliation')}>Retour</Button>}
      />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Field label="Statut" value={<StatusBadge value={data.status} />} />
            <Field label="Créé par" value={`${data.createdByName} — ${formatDate(data.createdAt, true)}`} />
            <Field label="Items" value={data.items.length} />
            {data.notes && (
              <Grid item xs={12}>
                <Typography variant="caption" color="text.secondary">Notes</Typography>
                <Typography>{data.notes}</Typography>
              </Grid>
            )}
          </Grid>
        </CardContent>
      </Card>

      {data.status !== 'CLOSED' && (
        <Card sx={{ mb: 2 }}>
          <CardContent>
            <Box mb={1}><b>Ajouter un rapprochement manuel</b></Box>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={3}><TextField fullWidth size="small" label="Type gauche" value={leftType} onChange={(e) => setLeftType(e.target.value)} /></Grid>
              <Grid item xs={12} sm={3}><TextField fullWidth size="small" type="number" label="Id gauche" value={leftId} onChange={(e) => setLeftId(e.target.value)} /></Grid>
              <Grid item xs={12} sm={3}><TextField fullWidth size="small" label="Type droit" value={rightType} onChange={(e) => setRightType(e.target.value)} /></Grid>
              <Grid item xs={12} sm={3}><TextField fullWidth size="small" type="number" label="Id droit" value={rightId} onChange={(e) => setRightId(e.target.value)} /></Grid>
              <Grid item xs={12} sm={3}><TextField fullWidth size="small" type="number" label="Montant" value={amount} onChange={(e) => setAmount(e.target.value)} /></Grid>
              <Grid item xs={12} sm={6}><TextField fullWidth size="small" label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} /></Grid>
              <Grid item xs={12} sm={3}>
                <Stack direction="row" alignItems="center" justifyContent="flex-end">
                  <Button variant="contained" startIcon={<AddLinkIcon />} onClick={submitMatch} disabled={match.isPending}>Ajouter</Button>
                </Stack>
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      )}

      <AppTable columns={cols} rows={data.items} rowKey={(r) => r.id} maxHeight="calc(100vh - 500px)" />
    </PageContainer>
  );
}
