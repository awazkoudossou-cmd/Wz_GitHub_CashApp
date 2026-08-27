import { useState } from 'react';
import {
  Alert, Autocomplete, Box, Button, Card, CardContent, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  Divider, Grid, IconButton, LinearProgress, MenuItem, Stack, Tab, Tabs, TextField, Typography
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import DownloadIcon from '@mui/icons-material/Download';
import ReplayIcon from '@mui/icons-material/Replay';
import DeleteIcon from '@mui/icons-material/Delete';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import AddIcon from '@mui/icons-material/Add';
import BlockIcon from '@mui/icons-material/Block';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { StatusBadge } from '@/components/common/StatusBadge';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { ConfirmDialog } from '@/components/dialogs/ConfirmDialog';
import { useNotificationStore } from '@/app/store/notificationStore';
import { extractErrorMessage } from '@/api/client';
import { useAuthStore } from '@/app/store/authStore';
import {
  useAccountingGeneration,
  useAccountingGenerations,
  useAccountingQueue,
  useCancelQueueItem,
  useDeleteAccountingGeneration,
  useEnqueueManualGeneration,
  useExportAccountingGeneration,
  usePreviewAccountingGeneration,
  useRegenerateAccountingGeneration,
  useRetryQueueItem
} from '@/modules/accounting/hooks';
import { formatDate } from '@/utils/format';
import type {
  AccountingEntry, AccountingGenerationFilter, AccountingGenerationListItem,
  AccountingQueueFilter, AccountingQueueItem, QueueStatus
} from '@/types';

const EMPTY_FILTER: AccountingGenerationFilter = { page: 1, pageSize: 50 };

export function AccountingGenerationsPage() {
  const [tab, setTab] = useState<'batches' | 'queue'>('batches');

  return (
    <PageContainer maxWidth="xl">
      <PageHeader title="Générations" subtitle="Batchs de génération comptable et file d'attente" />
      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
        <Tab value="batches" label="Batchs" />
        <Tab value="queue" label="File d'attente" />
      </Tabs>
      {tab === 'batches' ? <BatchesTab /> : <QueueMonitorTab />}
    </PageContainer>
  );
}

function BatchesTab() {
  const [filter, setFilter] = useState<AccountingGenerationFilter>(EMPTY_FILTER);
  const [selectedId, setSelectedId] = useState<number | undefined>(undefined);
  const batchesQ = useAccountingGenerations(filter);

  const columns: Column<AccountingGenerationListItem>[] = [
    { key: 'reference', header: 'Batch Number', render: (b) => b.reference },
    { key: 'date', header: 'Date', render: (b) => formatDate(b.generatedAt, true) },
    { key: 'mode', header: 'Mode', render: (b) => b.generationMode },
    { key: 'type', header: 'Type', render: (b) => b.generationType },
    { key: 'ops', header: 'Nb opérations', align: 'right', render: (b) => b.totalOperations },
    { key: 'entries', header: 'Nb écritures', align: 'right', render: (b) => b.entryCount },
    { key: 'user', header: 'Utilisateur', render: (b) => b.generatedByName },
    { key: 'status', header: 'État', render: (b) => <StatusBadge value={b.status} /> },
    { key: 'exported', header: 'Exporté', render: (b) => <Chip size="small" label={b.exported ? 'Oui' : 'Non'} color={b.exported ? 'success' : 'default'} /> }
  ];

  return (
    <Box>
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={3}>
              <TextField fullWidth size="small" type="date" label="Du" InputLabelProps={{ shrink: true }}
                value={filter.from ?? ''} onChange={(e) => setFilter((f) => ({ ...f, from: e.target.value || undefined, page: 1 }))} />
            </Grid>
            <Grid item xs={12} sm={3}>
              <TextField fullWidth size="small" type="date" label="Au" InputLabelProps={{ shrink: true }}
                value={filter.to ?? ''} onChange={(e) => setFilter((f) => ({ ...f, to: e.target.value || undefined, page: 1 }))} />
            </Grid>
            <Grid item xs={12} sm={3}>
              <TextField select fullWidth size="small" label="État"
                value={filter.status ?? ''} onChange={(e) => setFilter((f) => ({ ...f, status: e.target.value || undefined, page: 1 }))}>
                <MenuItem value="">Tous</MenuItem>
                <MenuItem value="DRAFT">Brouillon</MenuItem>
                <MenuItem value="GENERATED">Généré</MenuItem>
                <MenuItem value="EXPORTED">Exporté</MenuItem>
                <MenuItem value="CANCELLED">Annulé</MenuItem>
              </TextField>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <AppTable
        columns={columns}
        rows={batchesQ.data?.items}
        rowKey={(b) => b.id}
        isLoading={batchesQ.isLoading}
        onRowClick={(b) => setSelectedId(b.id)}
        emptyTitle="Aucun batch"
        pagination={{
          page: filter.page ?? 1,
          pageSize: filter.pageSize ?? 50,
          total: batchesQ.data?.totalCount ?? 0,
          onPageChange: (page) => setFilter((f) => ({ ...f, page })),
          onPageSizeChange: (pageSize) => setFilter((f) => ({ ...f, pageSize, page: 1 }))
        }}
      />

      <BatchDetailDialog id={selectedId} onClose={() => setSelectedId(undefined)} />
    </Box>
  );
}

function BatchDetailDialog({ id, onClose }: { id: number | undefined; onClose: () => void }) {
  const batchQ = useAccountingGeneration(id);
  const regenerate = useRegenerateAccountingGeneration();
  const del = useDeleteAccountingGeneration();
  const exportBatch = useExportAccountingGeneration();
  const notifyError = useNotificationStore((s) => s.notifyError);
  const [confirmRegenerate, setConfirmRegenerate] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);

  const b = batchQ.data;
  const difference = b ? Math.round((b.totalDebit - b.totalCredit) * 100) / 100 : 0;

  const entryColumns: Column<AccountingEntry>[] = [
    { key: 'journal', header: 'Journal', render: (e) => e.journalCode },
    { key: 'account', header: 'Compte', render: (e) => `${e.accountNumber} — ${e.accountName}` },
    { key: 'ref', header: 'Référence', render: (e) => e.reference },
    { key: 'label', header: 'Libellé', render: (e) => e.description },
    { key: 'debit', header: 'Débit', align: 'right', render: (e) => <CurrencyDisplay value={e.debit || undefined} /> },
    { key: 'credit', header: 'Crédit', align: 'right', render: (e) => <CurrencyDisplay value={e.credit || undefined} /> }
  ];

  const doRegenerate = async () => {
    if (!id) return;
    try { await regenerate.mutateAsync(id); setConfirmRegenerate(false); }
    catch (e) { notifyError(extractErrorMessage(e)); setConfirmRegenerate(false); }
  };

  const doDelete = async () => {
    if (!id) return;
    try { await del.mutateAsync(id); setConfirmDelete(false); onClose(); }
    catch (e) { notifyError(extractErrorMessage(e)); setConfirmDelete(false); }
  };

  const doExport = async () => {
    if (!id) return;
    try { await exportBatch.mutateAsync(id); }
    catch (e) { notifyError(extractErrorMessage(e)); }
  };

  return (
    <Dialog open={!!id} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle sx={{ pr: 6 }}>
        Batch {b?.reference}
        <IconButton onClick={onClose} sx={{ position: 'absolute', right: 8, top: 8 }}><CloseIcon /></IconButton>
      </DialogTitle>
      <DialogContent dividers>
        {!b ? (
          <Typography color="text.secondary">Chargement…</Typography>
        ) : (
          <Stack spacing={2}>
            <Stack direction="row" spacing={1} alignItems="center">
              <StatusBadge value={b.status} />
              <Chip size="small" label={b.exported ? 'Exporté' : 'Non exporté'} color={b.exported ? 'success' : 'default'} />
              <Typography variant="body2" color="text.secondary">Généré le {formatDate(b.generatedAt, true)} par {b.generatedByName}</Typography>
            </Stack>

            <Grid container spacing={2}>
              <Grid item xs={6} sm={3}><Typography variant="overline" color="text.secondary">Nb opérations</Typography><Typography>{b.totalOperations}</Typography></Grid>
              <Grid item xs={6} sm={3}><Typography variant="overline" color="text.secondary">Nb écritures</Typography><Typography>{b.totalEntries}</Typography></Grid>
              <Grid item xs={6} sm={3}><Typography variant="overline" color="text.secondary">Temps de génération</Typography><Typography>{b.processingTimeMs != null ? `${b.processingTimeMs} ms` : '—'}</Typography></Grid>
              <Grid item xs={6} sm={3}><Typography variant="overline" color="text.secondary">Remarques</Typography><Typography>{b.remarks ?? '—'}</Typography></Grid>
            </Grid>

            <Divider />

            <Grid container spacing={2}>
              <Grid item xs={4}><Typography variant="overline" color="text.secondary">Total Débit</Typography><Typography><CurrencyDisplay value={b.totalDebit} /></Typography></Grid>
              <Grid item xs={4}><Typography variant="overline" color="text.secondary">Total Crédit</Typography><Typography><CurrencyDisplay value={b.totalCredit} /></Typography></Grid>
              <Grid item xs={4}>
                <Typography variant="overline" color="text.secondary">Différence</Typography>
                <Typography color={difference !== 0 ? 'error.main' : 'success.main'} fontWeight={600}>
                  <CurrencyDisplay value={difference} />
                </Typography>
              </Grid>
            </Grid>
            {difference !== 0 && (
              <Alert severity="error" icon={<WarningAmberIcon />}>
                Le batch n'est pas équilibré (débit ≠ crédit). Vérifiez la configuration comptable.
              </Alert>
            )}

            <AppTable columns={entryColumns} rows={b.entries} rowKey={(e) => e.id} maxHeight={300} emptyTitle="Aucune écriture" />
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button startIcon={<DownloadIcon />} onClick={doExport} disabled={!b}>Exporter Excel</Button>
        <Button startIcon={<ReplayIcon />} onClick={() => setConfirmRegenerate(true)} disabled={!b || b.exported}>Régénérer</Button>
        <Button color="error" startIcon={<DeleteIcon />} onClick={() => setConfirmDelete(true)} disabled={!b || b.exported}>Supprimer</Button>
      </DialogActions>

      <ConfirmDialog
        open={confirmRegenerate}
        title="Régénérer ce batch ?"
        description="Les écritures existantes de ce batch seront supprimées puis recréées à partir des opérations de la même période."
        confirmLabel="Régénérer"
        onConfirm={doRegenerate}
        onClose={() => setConfirmRegenerate(false)}
      />
      <ConfirmDialog
        open={confirmDelete}
        title="Supprimer ce batch ?"
        description="Toutes les écritures de ce batch seront définitivement supprimées. Les opérations de caisse d'origine ne sont jamais affectées. Cette action est irréversible."
        confirmLabel="Supprimer"
        destructive
        onConfirm={doDelete}
        onClose={() => setConfirmDelete(false)}
      />
    </Dialog>
  );
}

const QUEUE_EMPTY_FILTER: AccountingQueueFilter = { page: 1, pageSize: 50 };

const QUEUE_STATUS_COLOR: Record<QueueStatus, 'default' | 'info' | 'success' | 'error' | 'warning'> = {
  PENDING: 'warning',
  PROCESSING: 'info',
  COMPLETED: 'success',
  FAILED: 'error',
  CANCELLED: 'default'
};

function QueueMonitorTab() {
  const [filter, setFilter] = useState<AccountingQueueFilter>(QUEUE_EMPTY_FILTER);
  const queueQ = useAccountingQueue(filter);
  const cancelItem = useCancelQueueItem();
  const retryItem = useRetryQueueItem();
  const notifyError = useNotificationStore((s) => s.notifyError);
  const [newOpen, setNewOpen] = useState(false);

  const doCancel = async (id: number) => {
    try { await cancelItem.mutateAsync(id); } catch (e) { notifyError(extractErrorMessage(e)); }
  };
  const doRetry = async (id: number) => {
    try { await retryItem.mutateAsync(id); } catch (e) { notifyError(extractErrorMessage(e)); }
  };

  const columns: Column<AccountingQueueItem>[] = [
    { key: 'id', header: 'Queue', render: (q) => `#${q.id}` },
    { key: 'status', header: 'Statut', render: (q) => <Chip size="small" label={q.status} color={QUEUE_STATUS_COLOR[q.status]} /> },
    { key: 'start', header: 'Début', render: (q) => formatDate(q.startDate) },
    { key: 'end', header: 'Fin', render: (q) => formatDate(q.endDate) },
    { key: 'user', header: 'Utilisateur', render: (q) => q.requestedByName },
    {
      key: 'progress', header: 'Progression', width: 160,
      render: (q) => {
        if (q.status === 'PROCESSING') return <LinearProgress />;
        if (q.status === 'COMPLETED') return <LinearProgress variant="determinate" value={100} color="success" />;
        if (q.status === 'FAILED') return <LinearProgress variant="determinate" value={100} color="error" />;
        return <Typography variant="caption" color="text.secondary">—</Typography>;
      }
    },
    { key: 'ops', header: "Nb opérations", render: (q) => q.resultGenerationReference ?? '—' },
    { key: 'errors', header: 'Erreurs', render: (q) => q.remarks ?? '—' },
    {
      key: 'actions', header: 'Actions', render: (q) => (
        <Stack direction="row" spacing={1}>
          {q.status === 'PENDING' && (
            <IconButton size="small" title="Annuler" onClick={() => doCancel(q.id)}><BlockIcon fontSize="small" /></IconButton>
          )}
          {q.status === 'FAILED' && q.retryCount < 3 && (
            <IconButton size="small" title="Relancer" onClick={() => doRetry(q.id)}><ReplayIcon fontSize="small" /></IconButton>
          )}
        </Stack>
      )
    }
  ];

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
        <TextField select size="small" label="Statut" sx={{ minWidth: 200 }}
          value={filter.status ?? ''} onChange={(e) => setFilter((f) => ({ ...f, status: (e.target.value || undefined) as QueueStatus | undefined, page: 1 }))}>
          <MenuItem value="">Tous</MenuItem>
          <MenuItem value="PENDING">En attente</MenuItem>
          <MenuItem value="PROCESSING">En cours</MenuItem>
          <MenuItem value="COMPLETED">Terminé</MenuItem>
          <MenuItem value="FAILED">Échec</MenuItem>
          <MenuItem value="CANCELLED">Annulé</MenuItem>
        </TextField>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setNewOpen(true)}>Nouvelle génération manuelle</Button>
      </Stack>

      <AppTable
        columns={columns}
        rows={queueQ.data?.items}
        rowKey={(q) => q.id}
        isLoading={queueQ.isLoading}
        emptyTitle="Aucune demande en file d'attente"
        pagination={{
          page: filter.page ?? 1,
          pageSize: filter.pageSize ?? 50,
          total: queueQ.data?.totalCount ?? 0,
          onPageChange: (page) => setFilter((f) => ({ ...f, page })),
          onPageSizeChange: (pageSize) => setFilter((f) => ({ ...f, pageSize, page: 1 }))
        }}
      />

      <NewManualGenerationDialog open={newOpen} onClose={() => setNewOpen(false)} />
    </Box>
  );
}

function NewManualGenerationDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const registers = useAuthStore((s) => s.cashRegisters);
  const preview = usePreviewAccountingGeneration();
  const enqueue = useEnqueueManualGeneration();
  const notifyError = useNotificationStore((s) => s.notifyError);
  const notifySuccess = useNotificationStore((s) => s.notifySuccess);

  const today = new Date().toISOString().slice(0, 10);
  const monthStart = new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().slice(0, 10);
  const [from, setFrom] = useState(monthStart);
  const [to, setTo] = useState(today);
  const [registerIds, setRegisterIds] = useState<number[]>([]);
  const [priority, setPriority] = useState(0);
  const [confirmOpen, setConfirmOpen] = useState(false);

  const doPreview = async () => {
    try {
      await preview.mutateAsync({
        startDate: `${from}T00:00:00Z`, endDate: `${to}T23:59:59Z`,
        cashRegisterIds: registerIds.length > 0 ? registerIds : null
      });
    } catch (e) { notifyError(extractErrorMessage(e)); }
  };

  const doEnqueue = async () => {
    try {
      await enqueue.mutateAsync({
        startDate: `${from}T00:00:00Z`, endDate: `${to}T23:59:59Z`,
        cashRegisterIds: registerIds.length > 0 ? registerIds : null, priority
      });
      notifySuccess('Demande de génération mise en file — traitée par le worker en arrière-plan.');
      setConfirmOpen(false);
      onClose();
    } catch (e) { notifyError(extractErrorMessage(e)); setConfirmOpen(false); }
  };

  const p = preview.data;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ pr: 6 }}>
        Nouvelle génération manuelle
        <IconButton onClick={onClose} sx={{ position: 'absolute', right: 8, top: 8 }}><CloseIcon /></IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2}>
          <Grid container spacing={2}>
            <Grid item xs={6}>
              <TextField fullWidth size="small" type="date" label="Date début" InputLabelProps={{ shrink: true }}
                value={from} onChange={(e) => setFrom(e.target.value)} />
            </Grid>
            <Grid item xs={6}>
              <TextField fullWidth size="small" type="date" label="Date fin" InputLabelProps={{ shrink: true }}
                value={to} onChange={(e) => setTo(e.target.value)} />
            </Grid>
            <Grid item xs={12}>
              <Autocomplete
                multiple
                size="small"
                options={registers}
                getOptionLabel={(r) => `${r.code} — ${r.name}`}
                isOptionEqualToValue={(a, b) => a.id === b.id}
                value={registers.filter((r) => registerIds.includes(r.id))}
                onChange={(_, vals) => setRegisterIds(vals.map((v) => v.id))}
                renderInput={(params) => (
                  <TextField {...params} label="Caisses (optionnel)"
                    helperText="Aucune sélection = toutes les caisses." />
                )}
              />
            </Grid>
            <Grid item xs={6}>
              <TextField fullWidth size="small" type="number" label="Priorité" value={priority}
                onChange={(e) => setPriority(Number(e.target.value) || 0)} />
            </Grid>
          </Grid>

          <Button variant="outlined" onClick={doPreview} disabled={preview.isPending}>Prévisualiser</Button>

          {p && (
            <Card variant="outlined">
              <CardContent>
                <Grid container spacing={1}>
                  <Grid item xs={6}><Typography variant="caption" color="text.secondary">Opérations à traiter</Typography><Typography>{p.operationCount}</Typography></Grid>
                  <Grid item xs={6}><Typography variant="caption" color="text.secondary">Écritures estimées</Typography><Typography>{p.estimatedEntryCount}</Typography></Grid>
                  <Grid item xs={6}><Typography variant="caption" color="text.secondary">Déjà comptabilisées</Typography><Typography>{p.alreadyAccountedCount}</Typography></Grid>
                  <Grid item xs={6}><Typography variant="caption" color="text.secondary">Ignorées</Typography><Typography>{p.ignoredCount}</Typography></Grid>
                  <Grid item xs={6}><Typography variant="caption" color="text.secondary">En attente (config)</Typography><Typography>{p.pendingCount}</Typography></Grid>
                  <Grid item xs={6}><Typography variant="caption" color="text.secondary">Temps estimé</Typography><Typography>{p.estimatedProcessingMs} ms</Typography></Grid>
                </Grid>
              </CardContent>
            </Card>
          )}

          {p && p.operationCount === 0 && (
            <Alert severity="info">Aucune opération à traiter sur cette période : rien à mettre en file d'attente.</Alert>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Annuler</Button>
        <Button variant="contained" onClick={() => setConfirmOpen(true)} disabled={!p || p.operationCount === 0 || enqueue.isPending}>
          Mettre en file d'attente
        </Button>
      </DialogActions>

      <ConfirmDialog
        open={confirmOpen}
        title="Confirmer la génération"
        description="La demande sera traitée de façon asynchrone par le worker comptable (pas de génération directe)."
        confirmLabel="Confirmer"
        onConfirm={doEnqueue}
        onClose={() => setConfirmOpen(false)}
      />
    </Dialog>
  );
}
