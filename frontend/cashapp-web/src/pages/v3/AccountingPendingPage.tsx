import { useState } from 'react';
import { Button, Card, CardContent, Chip, Grid, IconButton, MenuItem, TextField, Tooltip } from '@mui/material';
import ReplayIcon from '@mui/icons-material/Replay';
import VisibilityIcon from '@mui/icons-material/Visibility';
import { useNavigate } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { ConfirmDialog } from '@/components/dialogs/ConfirmDialog';
import { useNotificationStore } from '@/app/store/notificationStore';
import { extractErrorMessage } from '@/api/client';
import { useAccountingPendingOps, useGeneratePendingAccountingEntries } from '@/modules/accounting/hooks';
import { formatDate } from '@/utils/format';
import type { AccountingPending, AccountingPendingFilter } from '@/types';

const EMPTY_FILTER: AccountingPendingFilter = { page: 1, pageSize: 25 };

export function AccountingPendingPage() {
  const navigate = useNavigate();
  const [filter, setFilter] = useState<AccountingPendingFilter>({ ...EMPTY_FILTER, resolved: false });
  const pendingQ = useAccountingPendingOps(filter);
  const retreat = useGeneratePendingAccountingEntries();
  const notifySuccess = useNotificationStore((s) => s.notifySuccess);
  const notifyError = useNotificationStore((s) => s.notifyError);
  const [confirmOpen, setConfirmOpen] = useState(false);

  const doRetreat = async () => {
    try {
      const result = await retreat.mutateAsync();
      notifySuccess(`Relance ${result.reference} : ${result.entries.length} écriture(s) générée(s).`);
    } catch (e) {
      notifyError(extractErrorMessage(e));
    } finally {
      setConfirmOpen(false);
    }
  };

  const columns: Column<AccountingPending>[] = [
    { key: 'date', header: 'Date', render: (p) => formatDate(p.operationDate) },
    { key: 'operation', header: 'Opération', render: (p) => p.cashOperationRef },
    { key: 'reason', header: 'Motif', render: (p) => p.reason },
    { key: 'state', header: 'État', render: (p) => (
      <Chip size="small" label={p.resolved ? 'Résolue' : 'En attente'} color={p.resolved ? 'success' : 'warning'} />
    ) },
    { key: 'actions', header: 'Actions', align: 'right', render: (p) => (
      <Tooltip title="Voir l'opération">
        <IconButton size="small" onClick={() => navigate(`/cash-operations/${p.cashOperationId}`)}>
          <VisibilityIcon fontSize="small" />
        </IconButton>
      </Tooltip>
    ) }
  ];

  return (
    <PageContainer>
      <PageHeader
        title="Opérations comptables en attente"
        subtitle="Opérations que le moteur n'a pas pu comptabiliser (configuration incomplète)"
        actions={
          <Button variant="contained" startIcon={<ReplayIcon />} onClick={() => setConfirmOpen(true)} disabled={retreat.isPending}>
            Retraiter les opérations en attente
          </Button>
        }
      />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={4}>
              <TextField select fullWidth size="small" label="État"
                value={filter.resolved === undefined ? '' : String(filter.resolved)}
                onChange={(e) => setFilter((f) => ({ ...f, resolved: e.target.value === '' ? undefined : e.target.value === 'true', page: 1 }))}>
                <MenuItem value="">Toutes</MenuItem>
                <MenuItem value="false">En attente</MenuItem>
                <MenuItem value="true">Résolues</MenuItem>
              </TextField>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <AppTable
        columns={columns}
        rows={pendingQ.data?.items}
        rowKey={(p) => p.id}
        isLoading={pendingQ.isLoading}
        emptyTitle="Aucune opération en attente"
        emptyDescription="Toutes les opérations éligibles ont été comptabilisées."
        pagination={{
          page: filter.page ?? 1,
          pageSize: filter.pageSize ?? 25,
          total: pendingQ.data?.totalCount ?? 0,
          onPageChange: (page) => setFilter((f) => ({ ...f, page })),
          onPageSizeChange: (pageSize) => setFilter((f) => ({ ...f, pageSize, page: 1 }))
        }}
      />

      <ConfirmDialog
        open={confirmOpen}
        title="Retraiter les opérations en attente ?"
        description="Le moteur va tenter de comptabiliser uniquement les opérations actuellement en attente, en utilisant la configuration actuelle."
        confirmLabel="Retraiter"
        onConfirm={doRetreat}
        onClose={() => setConfirmOpen(false)}
      />
    </PageContainer>
  );
}
