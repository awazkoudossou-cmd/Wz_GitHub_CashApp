import { useState } from 'react';
import { Alert, Button, Card, CardContent, Grid, Stack, Typography } from '@mui/material';
import LockIcon from '@mui/icons-material/Lock';
import DownloadIcon from '@mui/icons-material/Download';
import AddIcon from '@mui/icons-material/Add';
import { useNavigate, useParams } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatCard } from '@/components/common/StatCard';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { StatusChip } from '@/components/common/StatusChip';
import { CloseSessionDialog } from '@/components/dialogs/CloseSessionDialog';
import { OperationDetailDialog } from '@/components/dialogs/OperationDetailDialog';
import { useCashSession, useCloseCashSession } from '@/modules/cash-sessions/hooks';
import { useCashOperations } from '@/modules/cash-operations/hooks';
import { useExportCashState } from '@/modules/exports/hooks';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { formatDate } from '@/utils/format';
import { extractErrorMessage } from '@/api/client';
import type { CashOperationListItem } from '@/types';

export function CashSessionDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const sessionId = Number(id);
  const session = useCashSession(sessionId);
  const ops = useCashOperations({ cashSessionId: sessionId, pageSize: 100 });
  const closeMut = useCloseCashSession();
  const exportMut = useExportCashState();
  const [closeDialog, setCloseDialog] = useState(false);
  const [popupOpId, setPopupOpId] = useState<number | null>(null);

  if (session.isLoading) return <LoadingScreen />;
  if (!session.data) return <PageContainer><Alert severity="error">Session introuvable.</Alert></PageContainer>;

  const s = session.data;

  const cols: Column<CashOperationListItem>[] = [
    { key: 'ref', header: 'Réf.', render: (r) => r.operationRef },
    { key: 'date', header: 'Date', render: (r) => formatDate(r.operationDate) },
    { key: 'dir', header: 'Dir.', render: (r) => <StatusChip status={r.direction} variant="direction" /> },
    { key: 'cat', header: 'Catégorie', render: (r) => r.categoryLabel },
    { key: 'label', header: 'Libellé', render: (r) => r.label },
    { key: 'amount', header: 'Montant', align: 'right', render: (r) => <CurrencyDisplay value={r.amount} currency={r.currencyCode} /> },
    { key: 'status', header: '', render: (r) => (r.isDeleted ? <StatusChip status="ANNULÉE" /> : null) }
  ];

  return (
    <PageContainer>
      <PageHeader
        title={`Session #${s.id} — ${s.cashRegisterName}`}
        subtitle={`Ouvert par ${s.openedByName} le ${formatDate(s.openedAt, true)}`}
        actions={
          <Stack direction="row" spacing={1}>
            <Button startIcon={<DownloadIcon />} onClick={() => exportMut.mutate({ cashSessionId: s.id })}>État PDF</Button>
            <Button startIcon={<AddIcon />} variant="outlined" onClick={() => navigate(`/cash-operations/new?sessionId=${s.id}`)} disabled={s.status !== 'OPEN'}>
              Nouvelle opération
            </Button>
            <Button variant="contained" color="warning" startIcon={<LockIcon />} disabled={s.status !== 'OPEN'} onClick={() => setCloseDialog(true)}>
              Fermer la session
            </Button>
          </Stack>
        }
      />
      <Grid container spacing={2} mb={3}>
        <Grid item xs={6} md={3}><StatCard label="Solde d'ouverture" value={<CurrencyDisplay value={s.openingBalance} />} /></Grid>
        <Grid item xs={6} md={3}><StatCard label="Solde théorique" value={<CurrencyDisplay value={s.theoreticalBalance ?? undefined} />} /></Grid>
        <Grid item xs={6} md={3}><StatCard label="Solde physique" value={<CurrencyDisplay value={s.physicalBalance ?? undefined} />} /></Grid>
        <Grid item xs={6} md={3}>
          <StatCard
            label="Écart"
            value={<CurrencyDisplay value={s.varianceAmount ?? undefined} />}
            color={s.varianceAmount != null && Math.abs(s.varianceAmount) > 0 ? 'error' : 'success'}
          />
        </Grid>
        <Grid item xs={6} md={3}><StatCard label="Opérations" value={s.summary.operationCount} /></Grid>
        <Grid item xs={6} md={3}><StatCard label="Total entrées" value={<CurrencyDisplay value={s.summary.totalIn} />} color="success" /></Grid>
        <Grid item xs={6} md={3}><StatCard label="Total sorties" value={<CurrencyDisplay value={s.summary.totalOut} />} color="warning" /></Grid>
        <Grid item xs={6} md={3}><StatCard label="Statut" value={<StatusChip status={s.status} variant="session" />} /></Grid>
      </Grid>

      {(s.openComment || s.closeComment) && (
        <Card sx={{ mb: 2 }}>
          <CardContent>
            {s.openComment && <Typography variant="body2"><b>Commentaire d'ouverture :</b> {s.openComment}</Typography>}
            {s.closeComment && <Typography variant="body2" mt={1}><b>Commentaire de fermeture :</b> {s.closeComment}</Typography>}
          </CardContent>
        </Card>
      )}

      <AppTable
        columns={cols}
        rows={ops.data?.items}
        rowKey={(r) => r.id}
        isLoading={ops.isLoading}
        onRowClick={(r) => {
          // Session fermée → popup en lecture seule ; sinon page détail.
          if (s.status !== 'OPEN') setPopupOpId(r.id);
          else navigate(`/cash-operations/${r.id}`);
        }}
      />

      <OperationDetailDialog
        opId={popupOpId}
        open={popupOpId !== null}
        onClose={() => setPopupOpId(null)}
      />

      <CloseSessionDialog
        open={closeDialog}
        onClose={() => setCloseDialog(false)}
        sessionId={s.id}
        theoreticalHint={s.theoreticalBalance ?? undefined}
        onSubmit={async (payload) => {
          try {
            await closeMut.mutateAsync({ id: s.id, payload });
            setCloseDialog(false);
          } catch (e) {
            alert(extractErrorMessage(e));
          }
        }}
      />
    </PageContainer>
  );
}
