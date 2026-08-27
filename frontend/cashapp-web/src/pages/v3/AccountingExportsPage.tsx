import { useState } from 'react';
import {
  Alert, Box, Button, Card, CardContent, Dialog, DialogActions, DialogContent, DialogTitle,
  Divider, Grid, IconButton, MenuItem, Stack, Tab, Tabs, TextField, Tooltip, Typography
} from '@mui/material';
import DownloadIcon from '@mui/icons-material/Download';
import VisibilityIcon from '@mui/icons-material/Visibility';
import ReplayIcon from '@mui/icons-material/Replay';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { StatusBadge } from '@/components/common/StatusBadge';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { ConfirmDialog } from '@/components/dialogs/ConfirmDialog';
import { useNotificationStore } from '@/app/store/notificationStore';
import { extractErrorMessage } from '@/api/client';
import { useAuthStore } from '@/app/store/authStore';
import { useCategories } from '@/modules/categories/hooks';
import { useUsers } from '@/modules/users/hooks';
import {
  useAccountingAccounts,
  useAccountingExportDetail,
  useAccountingExportLogs,
  useAccountingGenerations,
  useAccountingJournals,
  useDeleteExportLog,
  useDownloadExportLog,
  useExportAccountingEntries,
  useExportAccountingGeneration,
  usePreviewAccountingExport,
  useReexportLog
} from '@/modules/accounting/hooks';
import { formatDate } from '@/utils/format';
import type { AccountingEntryFilter, AccountingExportLog, AccountingExportLogFilter, AccountingExportPreview } from '@/types';

const EMPTY_FILTER: AccountingExportLogFilter = { page: 1, pageSize: 25 };
const EMPTY_ENTRY_FILTER: AccountingEntryFilter = {};

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} o`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} Ko`;
  return `${(bytes / (1024 * 1024)).toFixed(2)} Mo`;
}

export function AccountingExportsPage() {
  const [filter, setFilter] = useState<AccountingExportLogFilter>(EMPTY_FILTER);
  const [newExportOpen, setNewExportOpen] = useState(false);
  const [detailId, setDetailId] = useState<number | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<AccountingExportLog | null>(null);

  const logsQ = useAccountingExportLogs(filter);
  const download = useDownloadExportLog();
  const reexport = useReexportLog();
  const deleteLog = useDeleteExportLog();
  const notifySuccess = useNotificationStore((s) => s.notifySuccess);
  const notifyError = useNotificationStore((s) => s.notifyError);

  const doDownload = async (log: AccountingExportLog) => {
    try {
      await download.mutateAsync({ id: log.id, fileName: log.fileName });
    } catch (e) {
      notifyError(extractErrorMessage(e));
    }
  };

  const doReexport = async (log: AccountingExportLog) => {
    try {
      await reexport.mutateAsync({ id: log.id, fileName: log.fileName });
      notifySuccess(`Réexport de ${log.exportNumber} effectué.`);
    } catch (e) {
      notifyError(extractErrorMessage(e));
    }
  };

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      await deleteLog.mutateAsync(deleteTarget.id);
    } catch (e) {
      notifyError(extractErrorMessage(e));
    } finally {
      setDeleteTarget(null);
    }
  };

  const columns: Column<AccountingExportLog>[] = [
    { key: 'exportNumber', header: 'N° Export', render: (l) => l.exportNumber },
    { key: 'date', header: 'Date', render: (l) => formatDate(l.exportedAt, true) },
    { key: 'user', header: 'Utilisateur', render: (l) => l.exportedByName },
    { key: 'batch', header: 'Batch', render: (l) => l.generationReference ?? '—' },
    { key: 'lines', header: 'Nombre lignes', align: 'right', render: (l) => l.lineCount },
    { key: 'fileName', header: 'Nom fichier', render: (l) => l.fileName },
    { key: 'status', header: 'Statut', render: (l) => <StatusBadge value={l.status} /> },
    {
      key: 'actions', header: 'Actions', align: 'right', render: (l) => (
        <Stack direction="row" spacing={0.5} justifyContent="flex-end">
          <Tooltip title="Détail">
            <IconButton size="small" onClick={() => setDetailId(l.id)}><VisibilityIcon fontSize="small" /></IconButton>
          </Tooltip>
          <Tooltip title="Télécharger">
            <span>
              <IconButton size="small" onClick={() => doDownload(l)} disabled={download.isPending || l.status === 'DELETED'}>
                <DownloadIcon fontSize="small" />
              </IconButton>
            </span>
          </Tooltip>
          <Tooltip title="Réexporter">
            <span>
              <IconButton size="small" onClick={() => doReexport(l)} disabled={reexport.isPending || l.status === 'DELETED'}>
                <ReplayIcon fontSize="small" />
              </IconButton>
            </span>
          </Tooltip>
          <Tooltip title="Supprimer le fichier">
            <span>
              <IconButton size="small" color="error" onClick={() => setDeleteTarget(l)} disabled={l.status === 'DELETED'}>
                <DeleteIcon fontSize="small" />
              </IconButton>
            </span>
          </Tooltip>
        </Stack>
      )
    }
  ];

  return (
    <PageContainer>
      <PageHeader
        title="Centre d'Exports Comptables"
        subtitle="Chaque export est prévisualisé, vérifié (Débit = Crédit) puis historisé"
        actions={
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setNewExportOpen(true)}>
            Nouvel export
          </Button>
        }
      />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={4} md={3}>
              <TextField select fullWidth size="small" label="Statut"
                value={filter.status ?? ''}
                onChange={(e) => setFilter((f) => ({ ...f, status: (e.target.value || undefined) as AccountingExportLogFilter['status'], page: 1 }))}>
                <MenuItem value="">Tous</MenuItem>
                <MenuItem value="GENERATED">Généré</MenuItem>
                <MenuItem value="DOWNLOADED">Téléchargé</MenuItem>
                <MenuItem value="DELETED">Supprimé</MenuItem>
              </TextField>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <AppTable
        columns={columns}
        rows={logsQ.data?.items}
        rowKey={(l) => l.id}
        isLoading={logsQ.isLoading}
        emptyTitle="Aucun export"
        emptyDescription="Créez un export depuis un Batch ou avec des filtres personnalisés."
        pagination={{
          page: filter.page ?? 1,
          pageSize: filter.pageSize ?? 25,
          total: logsQ.data?.totalCount ?? 0,
          onPageChange: (page) => setFilter((f) => ({ ...f, page })),
          onPageSizeChange: (pageSize) => setFilter((f) => ({ ...f, pageSize, page: 1 }))
        }}
      />

      <NewExportDialog open={newExportOpen} onClose={() => setNewExportOpen(false)} />
      <ExportDetailDialog id={detailId} onClose={() => setDetailId(null)} />

      <ConfirmDialog
        open={!!deleteTarget}
        title="Supprimer ce fichier d'export ?"
        description={deleteTarget ? `Seul le fichier "${deleteTarget.fileName}" sera supprimé. Le batch et les écritures comptables ne sont jamais affectés.` : undefined}
        confirmLabel="Supprimer"
        destructive
        onConfirm={confirmDelete}
        onClose={() => setDeleteTarget(null)}
      />
    </PageContainer>
  );
}

function NewExportDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const [mode, setMode] = useState<'batch' | 'filter'>('batch');
  const [generationId, setGenerationId] = useState<number | ''>('');
  const [entryFilter, setEntryFilter] = useState<AccountingEntryFilter>(EMPTY_ENTRY_FILTER);
  const [preview, setPreview] = useState<AccountingExportPreview | null>(null);

  const registers = useAuthStore((s) => s.cashRegisters);
  const journalsQ = useAccountingJournals();
  const accountsQ = useAccountingAccounts({ isActive: true, pageSize: 500 });
  const categoriesQ = useCategories();
  const usersQ = useUsers();
  const batchesQ = useAccountingGenerations({ pageSize: 200 });

  const previewMutation = usePreviewAccountingExport();
  const exportGeneration = useExportAccountingGeneration();
  const exportEntries = useExportAccountingEntries();
  const notifySuccess = useNotificationStore((s) => s.notifySuccess);
  const notifyError = useNotificationStore((s) => s.notifyError);

  const reset = () => {
    setMode('batch');
    setGenerationId('');
    setEntryFilter(EMPTY_ENTRY_FILTER);
    setPreview(null);
  };

  const handleClose = () => {
    reset();
    onClose();
  };

  const currentFilter: AccountingEntryFilter = mode === 'batch'
    ? { generationId: generationId === '' ? undefined : generationId }
    : entryFilter;

  const doPreview = async () => {
    if (mode === 'batch' && generationId === '') return;
    try {
      const result = await previewMutation.mutateAsync(currentFilter);
      setPreview(result);
    } catch (e) {
      notifyError(extractErrorMessage(e));
    }
  };

  const doConfirm = async () => {
    try {
      if (mode === 'batch' && generationId !== '') {
        await exportGeneration.mutateAsync(generationId);
      } else {
        await exportEntries.mutateAsync(entryFilter);
      }
      notifySuccess('Export créé et téléchargé.');
      handleClose();
    } catch (e) {
      notifyError(extractErrorMessage(e));
    }
  };

  const isPending = previewMutation.isPending || exportGeneration.isPending || exportEntries.isPending;

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>Nouvel export comptable</DialogTitle>
      <DialogContent dividers>
        <Tabs
          value={mode}
          onChange={(_, v) => { setMode(v); setPreview(null); }}
          sx={{ mb: 2 }}
        >
          <Tab label="Depuis un Batch" value="batch" />
          <Tab label="Par filtres" value="filter" />
        </Tabs>

        {mode === 'batch' ? (
          <TextField select fullWidth size="small" label="Batch"
            value={generationId}
            onChange={(e) => { setGenerationId(e.target.value ? Number(e.target.value) : ''); setPreview(null); }}>
            <MenuItem value="">— Sélectionner —</MenuItem>
            {(batchesQ.data?.items ?? []).map((b) => (
              <MenuItem key={b.id} value={b.id}>{b.reference} ({b.entryCount} écriture(s)){b.exported ? ' — déjà exporté' : ''}</MenuItem>
            ))}
          </TextField>
        ) : (
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6} md={3}>
              <TextField fullWidth size="small" type="date" label="Date début" InputLabelProps={{ shrink: true }}
                value={entryFilter.from ?? ''} onChange={(e) => { setEntryFilter((f) => ({ ...f, from: e.target.value || undefined })); setPreview(null); }} />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField fullWidth size="small" type="date" label="Date fin" InputLabelProps={{ shrink: true }}
                value={entryFilter.to ?? ''} onChange={(e) => { setEntryFilter((f) => ({ ...f, to: e.target.value || undefined })); setPreview(null); }} />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField select fullWidth size="small" label="Journal"
                value={entryFilter.journalId ?? ''} onChange={(e) => { setEntryFilter((f) => ({ ...f, journalId: e.target.value ? Number(e.target.value) : undefined })); setPreview(null); }}>
                <MenuItem value="">Tous</MenuItem>
                {(journalsQ.data ?? []).map((j) => <MenuItem key={j.id} value={j.id}>{j.code} — {j.name}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField select fullWidth size="small" label="Compte"
                value={entryFilter.accountId ?? ''} onChange={(e) => { setEntryFilter((f) => ({ ...f, accountId: e.target.value ? Number(e.target.value) : undefined })); setPreview(null); }}>
                <MenuItem value="">Tous</MenuItem>
                {(accountsQ.data?.items ?? []).map((a) => <MenuItem key={a.id} value={a.id}>{a.accountNumber} — {a.name}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField select fullWidth size="small" label="Caisse"
                value={entryFilter.cashRegisterId ?? ''} onChange={(e) => { setEntryFilter((f) => ({ ...f, cashRegisterId: e.target.value ? Number(e.target.value) : undefined })); setPreview(null); }}>
                <MenuItem value="">Toutes</MenuItem>
                {registers.map((r) => <MenuItem key={r.id} value={r.id}>{r.code} — {r.name}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField select fullWidth size="small" label="Catégorie"
                value={entryFilter.categoryId ?? ''} onChange={(e) => { setEntryFilter((f) => ({ ...f, categoryId: e.target.value ? Number(e.target.value) : undefined })); setPreview(null); }}>
                <MenuItem value="">Toutes</MenuItem>
                {(categoriesQ.data ?? []).map((c) => <MenuItem key={c.id} value={c.id}>{c.code} — {c.label}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField select fullWidth size="small" label="Utilisateur"
                value={entryFilter.userId ?? ''} onChange={(e) => { setEntryFilter((f) => ({ ...f, userId: e.target.value ? Number(e.target.value) : undefined })); setPreview(null); }}>
                <MenuItem value="">Tous</MenuItem>
                {(usersQ.data ?? []).map((u) => <MenuItem key={u.id} value={u.id}>{u.fullName}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField select fullWidth size="small" label="État"
                value={entryFilter.locked === undefined ? '' : String(entryFilter.locked)}
                onChange={(e) => { setEntryFilter((f) => ({ ...f, locked: e.target.value === '' ? undefined : e.target.value === 'true' })); setPreview(null); }}>
                <MenuItem value="">Tous</MenuItem>
                <MenuItem value="false">Modifiable</MenuItem>
                <MenuItem value="true">Verrouillé</MenuItem>
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField select fullWidth size="small" label="Type génération"
                value={entryFilter.generationType ?? ''} onChange={(e) => { setEntryFilter((f) => ({ ...f, generationType: (e.target.value || undefined) as AccountingEntryFilter['generationType'] })); setPreview(null); }}>
                <MenuItem value="">Tous</MenuItem>
                <MenuItem value="CENTRALIZED">Centralisé</MenuItem>
                <MenuItem value="DETAILED">Détaillé</MenuItem>
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField select fullWidth size="small" label="Mode génération"
                value={entryFilter.generationMode ?? ''} onChange={(e) => { setEntryFilter((f) => ({ ...f, generationMode: (e.target.value || undefined) as AccountingEntryFilter['generationMode'] })); setPreview(null); }}>
                <MenuItem value="">Tous</MenuItem>
                <MenuItem value="MANUAL">Manuel</MenuItem>
                <MenuItem value="ON_CASH_CLOSING">À la clôture de caisse</MenuItem>
              </TextField>
            </Grid>
          </Grid>
        )}

        <Divider sx={{ my: 2 }} />

        {preview && (
          <Box>
            <Typography variant="subtitle2" gutterBottom>Prévisualisation</Typography>
            <Grid container spacing={2} mb={1}>
              <Grid item xs={6} sm={3}><Typography variant="caption" color="text.secondary">Écritures</Typography><Typography>{preview.entryCount}</Typography></Grid>
              <Grid item xs={6} sm={3}><Typography variant="caption" color="text.secondary">Batchs</Typography><Typography>{preview.batchCount}</Typography></Grid>
              <Grid item xs={6} sm={3}><Typography variant="caption" color="text.secondary">Comptes</Typography><Typography>{preview.accountCount}</Typography></Grid>
              <Grid item xs={6} sm={3}><Typography variant="caption" color="text.secondary">Journaux</Typography><Typography>{preview.journalCount}</Typography></Grid>
              <Grid item xs={6} sm={3}>
                <Typography variant="caption" color="text.secondary">Période</Typography>
                <Typography>{preview.periodStart ? `${formatDate(preview.periodStart)} → ${formatDate(preview.periodEnd!)}` : '—'}</Typography>
              </Grid>
              <Grid item xs={6} sm={3}><Typography variant="caption" color="text.secondary">Taille estimée</Typography><Typography>{formatBytes(preview.estimatedSizeBytes)}</Typography></Grid>
              <Grid item xs={6} sm={3}><Typography variant="caption" color="text.secondary">Total débit</Typography><Typography>{preview.totalDebit.toLocaleString('fr-FR', { minimumFractionDigits: 2 })}</Typography></Grid>
              <Grid item xs={6} sm={3}><Typography variant="caption" color="text.secondary">Total crédit</Typography><Typography>{preview.totalCredit.toLocaleString('fr-FR', { minimumFractionDigits: 2 })}</Typography></Grid>
            </Grid>
            {preview.isBalanced ? (
              <Alert severity="success">Débit = Crédit : l'export peut être confirmé.</Alert>
            ) : (
              <Alert severity="error">Déséquilibre détecté (Débit ≠ Crédit) : l'export est interdit tant que le brouillard n'est pas équilibré.</Alert>
            )}
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>Annuler</Button>
        <Button variant="outlined" onClick={doPreview} disabled={isPending || (mode === 'batch' && generationId === '')}>
          Prévisualiser
        </Button>
        <Button variant="contained" onClick={doConfirm} disabled={isPending || !preview || !preview.isBalanced}>
          Confirmer l'export
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function ExportDetailDialog({ id, onClose }: { id: number | null; onClose: () => void }) {
  const detailQ = useAccountingExportDetail(id);
  const d = detailQ.data;

  return (
    <Dialog open={id !== null} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Détail de l'export {d ? d.exportNumber : ''}</DialogTitle>
      <DialogContent dividers>
        {!d ? (
          <Typography color="text.secondary">Chargement…</Typography>
        ) : (
          <Stack spacing={2}>
            <Box>
              <Typography variant="overline" color="text.secondary">Statut</Typography>
              <Box><StatusBadge value={d.status} /></Box>
            </Box>
            <Grid container spacing={2}>
              <Grid item xs={6}>
                <Typography variant="overline" color="text.secondary">Utilisateur</Typography>
                <Typography>{d.exportedByName}</Typography>
              </Grid>
              <Grid item xs={6}>
                <Typography variant="overline" color="text.secondary">Date</Typography>
                <Typography>{formatDate(d.exportedAt, true)}</Typography>
              </Grid>
              <Grid item xs={6}>
                <Typography variant="overline" color="text.secondary">Nombre d'écritures</Typography>
                <Typography>{d.lineCount}</Typography>
              </Grid>
              <Grid item xs={6}>
                <Typography variant="overline" color="text.secondary">Temps de génération</Typography>
                <Typography>{d.processingTimeMs} ms</Typography>
              </Grid>
              {d.generationReference && (
                <Grid item xs={12}>
                  <Typography variant="overline" color="text.secondary">Batch</Typography>
                  <Typography>{d.generationReference}</Typography>
                </Grid>
              )}
              {d.downloadedAt && (
                <Grid item xs={12}>
                  <Typography variant="overline" color="text.secondary">Téléchargé le</Typography>
                  <Typography>{formatDate(d.downloadedAt, true)}</Typography>
                </Grid>
              )}
            </Grid>
            <Box>
              <Typography variant="overline" color="text.secondary">Filtres</Typography>
              <Typography variant="body2">{d.filterDescription ?? 'Aucun filtre (toutes les écritures)'}</Typography>
            </Box>
            {d.remarks && (
              <Box>
                <Typography variant="overline" color="text.secondary">Remarques</Typography>
                <Typography variant="body2">{d.remarks}</Typography>
              </Box>
            )}
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Fermer</Button>
      </DialogActions>
    </Dialog>
  );
}
