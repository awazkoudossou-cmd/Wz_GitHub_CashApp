import { Fragment, useEffect, useMemo, useState } from 'react';
import {
  Alert, Box, Button, Card, CardContent, Chip, Divider, Drawer, Grid, IconButton,
  MenuItem, Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead,
  TablePagination, TableRow, TextField, Typography
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import DownloadIcon from '@mui/icons-material/Download';
import SaveIcon from '@mui/icons-material/Save';
import RestartAltIcon from '@mui/icons-material/RestartAlt';
import LockIcon from '@mui/icons-material/Lock';
import FilterListIcon from '@mui/icons-material/FilterList';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { StatCard } from '@/components/common/StatCard';
import { StatusBadge } from '@/components/common/StatusBadge';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { EmptyState } from '@/components/common/EmptyState';
import { useNotificationStore } from '@/app/store/notificationStore';
import { extractErrorMessage } from '@/api/client';
import { useAuthStore } from '@/app/store/authStore';
import { useCategories } from '@/modules/categories/hooks';
import { useUsers } from '@/modules/users/hooks';
import {
  useAccountingAccounts,
  useAccountingEntries,
  useAccountingEntry,
  useAccountingGenerations,
  useAccountingJournals,
  useAccountingLedgerStats,
  useExportAccountingEntries,
  useUpdateAccountingEntry
} from '@/modules/accounting/hooks';
import { formatDate } from '@/utils/format';
import type { AccountingEntryFilter, AccountingEntryListItem } from '@/types';

// Par défaut, les écritures sont groupées par batch (date de génération), du plus récent au plus ancien.
const EMPTY_FILTER: AccountingEntryFilter = { page: 1, pageSize: 25, sortBy: 'generationdate', sortDescending: true };

export function AccountingLedgerPage() {
  const [filter, setFilter] = useState<AccountingEntryFilter>(EMPTY_FILTER);
  const [draftFilter, setDraftFilter] = useState<AccountingEntryFilter>(EMPTY_FILTER);
  const [selectedId, setSelectedId] = useState<number | undefined>(undefined);
  const [filtersVisible, setFiltersVisible] = useState(true);
  const [collapsed, setCollapsed] = useState<Record<number, boolean>>({});

  const registers = useAuthStore((s) => s.cashRegisters);
  const journalsQ = useAccountingJournals();
  const accountsQ = useAccountingAccounts({ isActive: true, pageSize: 500 });
  const categoriesQ = useCategories();
  const usersQ = useUsers();
  const batchesQ = useAccountingGenerations({ pageSize: 200 });
  const statsQ = useAccountingLedgerStats();
  const entriesQ = useAccountingEntries(filter);
  const exportEntries = useExportAccountingEntries();
  const notifyError = useNotificationStore((s) => s.notifyError);

  const items = entriesQ.data?.items ?? [];

  // Groupe les écritures de la page courante par batch, dans l'ordre où elles arrivent
  // (déjà trié par date de génération décroissante côté serveur par défaut).
  const grouped = useMemo(() => {
    const map = new Map<number, { reference: string; status: string; items: AccountingEntryListItem[] }>();
    for (const e of items) {
      if (!map.has(e.generationId)) map.set(e.generationId, { reference: e.batchReference, status: e.batchStatus, items: [] });
      map.get(e.generationId)!.items.push(e);
    }
    return Array.from(map.entries()).map(([generationId, g]) => ({
      generationId,
      reference: g.reference,
      status: g.status,
      items: g.items,
      totalDebit: g.items.reduce((sum, e) => sum + e.debit, 0),
      totalCredit: g.items.reduce((sum, e) => sum + e.credit, 0)
    }));
  }, [items]);

  const applyFilters = () => setFilter({ ...draftFilter, page: 1, pageSize: filter.pageSize });
  const resetFilters = () => { setDraftFilter(EMPTY_FILTER); setFilter(EMPTY_FILTER); };

  const handleExport = async () => {
    try {
      await exportEntries.mutateAsync(filter);
    } catch (e) {
      notifyError(extractErrorMessage(e));
    }
  };

  return (
    <PageContainer maxWidth={false}>
      <Box sx={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 112px)' }}>
      <PageHeader
        title="Brouillard comptable"
        subtitle="Toutes les écritures générées, consultables et filtrables"
        actions={
          <Stack direction="row" spacing={1}>
            <Button variant="outlined" startIcon={<FilterListIcon />} onClick={() => setFiltersVisible((v) => !v)}>
              {filtersVisible ? 'Masquer les filtres' : 'Afficher les filtres'}
            </Button>
            <Button variant="outlined" startIcon={<DownloadIcon />} onClick={handleExport} disabled={exportEntries.isPending}>
              Exporter (filtres)
            </Button>
          </Stack>
        }
      />

      <Grid container spacing={2} mb={2}>
        <Grid item xs={6} md={3}>
          <StatCard label="Écritures" value={statsQ.data?.entryCount ?? '—'} />
        </Grid>
        <Grid item xs={6} md={3}>
          <StatCard label="Batchs" value={statsQ.data?.batchCount ?? '—'} />
        </Grid>
        <Grid item xs={6} md={3}>
          <StatCard label="Batchs exportés" value={statsQ.data?.exportedBatchCount ?? '—'} color="success" />
        </Grid>
        <Grid item xs={6} md={3}>
          <StatCard label="Opérations en attente" value={statsQ.data?.pendingCount ?? '—'} color={statsQ.data?.pendingCount ? 'warning' : 'text.primary'} />
        </Grid>
      </Grid>

      {filtersVisible && (
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6} md={2}>
              <TextField fullWidth size="small" type="date" label="Date début" InputLabelProps={{ shrink: true }}
                value={draftFilter.from ?? ''} onChange={(e) => setDraftFilter((f) => ({ ...f, from: e.target.value || undefined }))} />
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField fullWidth size="small" type="date" label="Date fin" InputLabelProps={{ shrink: true }}
                value={draftFilter.to ?? ''} onChange={(e) => setDraftFilter((f) => ({ ...f, to: e.target.value || undefined }))} />
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField select fullWidth size="small" label="Journal"
                value={draftFilter.journalId ?? ''} onChange={(e) => setDraftFilter((f) => ({ ...f, journalId: e.target.value ? Number(e.target.value) : undefined }))}>
                <MenuItem value="">Tous</MenuItem>
                {(journalsQ.data ?? []).map((j) => <MenuItem key={j.id} value={j.id}>{j.code} — {j.name}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField select fullWidth size="small" label="Compte"
                value={draftFilter.accountId ?? ''} onChange={(e) => setDraftFilter((f) => ({ ...f, accountId: e.target.value ? Number(e.target.value) : undefined }))}>
                <MenuItem value="">Tous</MenuItem>
                {(accountsQ.data?.items ?? []).map((a) => <MenuItem key={a.id} value={a.id}>{a.accountNumber} — {a.name}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField select fullWidth size="small" label="Caisse"
                value={draftFilter.cashRegisterId ?? ''} onChange={(e) => setDraftFilter((f) => ({ ...f, cashRegisterId: e.target.value ? Number(e.target.value) : undefined }))}>
                <MenuItem value="">Toutes</MenuItem>
                {registers.map((r) => <MenuItem key={r.id} value={r.id}>{r.code} — {r.name}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField select fullWidth size="small" label="Catégorie"
                value={draftFilter.categoryId ?? ''} onChange={(e) => setDraftFilter((f) => ({ ...f, categoryId: e.target.value ? Number(e.target.value) : undefined }))}>
                <MenuItem value="">Toutes</MenuItem>
                {(categoriesQ.data ?? []).map((c) => <MenuItem key={c.id} value={c.id}>{c.code} — {c.label}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField select fullWidth size="small" label="Utilisateur"
                value={draftFilter.userId ?? ''} onChange={(e) => setDraftFilter((f) => ({ ...f, userId: e.target.value ? Number(e.target.value) : undefined }))}>
                <MenuItem value="">Tous</MenuItem>
                {(usersQ.data ?? []).map((u) => <MenuItem key={u.id} value={u.id}>{u.fullName}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField select fullWidth size="small" label="Batch"
                value={draftFilter.generationId ?? ''} onChange={(e) => setDraftFilter((f) => ({ ...f, generationId: e.target.value ? Number(e.target.value) : undefined }))}>
                <MenuItem value="">Tous</MenuItem>
                {(batchesQ.data?.items ?? []).map((b) => <MenuItem key={b.id} value={b.id}>{b.reference}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField select fullWidth size="small" label="État"
                value={draftFilter.locked === undefined ? '' : String(draftFilter.locked)}
                onChange={(e) => setDraftFilter((f) => ({ ...f, locked: e.target.value === '' ? undefined : e.target.value === 'true' }))}>
                <MenuItem value="">Tous</MenuItem>
                <MenuItem value="false">Modifiable</MenuItem>
                <MenuItem value="true">Verrouillé</MenuItem>
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField fullWidth size="small" label="Référence"
                value={draftFilter.reference ?? ''} onChange={(e) => setDraftFilter((f) => ({ ...f, reference: e.target.value || undefined }))} />
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField fullWidth size="small" label="N° pièce"
                value={draftFilter.pieceNumber ?? ''} onChange={(e) => setDraftFilter((f) => ({ ...f, pieceNumber: e.target.value || undefined }))} />
            </Grid>
            <Grid item xs={12} sm={8} md={6}>
              <TextField fullWidth size="small" label="Recherche plein texte"
                placeholder="Libellé, référence, pièce, compte, journal, batch…"
                value={draftFilter.search ?? ''} onChange={(e) => setDraftFilter((f) => ({ ...f, search: e.target.value || undefined }))} />
            </Grid>
            <Grid item xs={12} sm={4} md={2}>
              <Stack direction="row" spacing={1} height="100%" alignItems="center">
                <Button fullWidth variant="contained" onClick={applyFilters}>Filtrer</Button>
                <IconButton onClick={resetFilters} title="Réinitialiser"><RestartAltIcon /></IconButton>
              </Stack>
            </Grid>
          </Grid>
        </CardContent>
      </Card>
      )}

      {entriesQ.isLoading ? <LoadingScreen /> : items.length === 0 ? (
        <Paper><EmptyState title="Aucune écriture" description="Aucune écriture ne correspond aux filtres sélectionnés." /></Paper>
      ) : (
        <Paper sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, overflow: 'hidden' }}>
          <TableContainer sx={{ flex: 1, maxHeight: '100%', overflowY: 'auto' }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Date opération</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Date comptable</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Journal</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Référence</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>N° pièce</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Compte</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Libellé</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }} align="right">Débit</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }} align="right">Crédit</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Utilisateur</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>État</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {grouped.map((g) => {
                  const isCollapsed = !!collapsed[g.generationId];
                  return (
                    <Fragment key={g.generationId}>
                      <TableRow sx={{ bgcolor: 'action.hover' }}>
                        <TableCell colSpan={11} sx={{ py: 0.5 }}>
                          <Stack direction="row" alignItems="center" spacing={1.5}>
                            <IconButton size="small" onClick={() => setCollapsed((c) => ({ ...c, [g.generationId]: !c[g.generationId] }))}>
                              {isCollapsed ? <ChevronRightIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
                            </IconButton>
                            <Typography variant="body2" fontWeight={600}>Batch {g.reference}</Typography>
                            <StatusBadge value={g.status} />
                            <Typography variant="caption" color="text.secondary">({g.items.length} écriture{g.items.length > 1 ? 's' : ''})</Typography>
                            <Box flex={1} />
                            <Typography variant="caption" color="text.secondary">
                              Débit <CurrencyDisplay value={g.totalDebit || undefined} /> · Crédit <CurrencyDisplay value={g.totalCredit || undefined} />
                            </Typography>
                          </Stack>
                        </TableCell>
                      </TableRow>
                      {!isCollapsed && g.items.map((e) => (
                        <TableRow key={e.id} hover onDoubleClick={() => setSelectedId(e.id)} sx={{ cursor: 'pointer' }}>
                          <TableCell>{formatDate(e.operationDate)}</TableCell>
                          <TableCell>{formatDate(e.entryDate, true)}</TableCell>
                          <TableCell>{e.journalCode}</TableCell>
                          <TableCell>{e.reference}</TableCell>
                          <TableCell>{e.pieceNumber ?? '—'}</TableCell>
                          <TableCell>{e.accountNumber} — {e.accountName}</TableCell>
                          <TableCell>{e.description}</TableCell>
                          <TableCell align="right"><CurrencyDisplay value={e.debit || undefined} /></TableCell>
                          <TableCell align="right"><CurrencyDisplay value={e.credit || undefined} /></TableCell>
                          <TableCell>{e.userName}</TableCell>
                          <TableCell>
                            {e.isLocked
                              ? <Chip size="small" icon={<LockIcon fontSize="small" />} label="Verrouillé" color="default" />
                              : <Chip size="small" label="Modifiable" color="success" variant="outlined" />}
                          </TableCell>
                        </TableRow>
                      ))}
                    </Fragment>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination
            component="div"
            count={entriesQ.data?.totalCount ?? 0}
            page={(filter.page ?? 1) - 1}
            onPageChange={(_, p) => setFilter((f) => ({ ...f, page: p + 1 }))}
            rowsPerPage={filter.pageSize ?? 25}
            onRowsPerPageChange={(e) => setFilter((f) => ({ ...f, pageSize: Number(e.target.value), page: 1 }))}
            rowsPerPageOptions={[25, 50, 100]}
            labelRowsPerPage="Lignes par page"
            labelDisplayedRows={({ from, to, count }) => `${from}–${to} sur ${count}`}
            sx={{ borderTop: '1px solid', borderColor: 'divider' }}
          />
        </Paper>
      )}
      </Box>

      <EntryDetailDrawer id={selectedId} onClose={() => setSelectedId(undefined)} />
    </PageContainer>
  );
}

function EntryDetailDrawer({ id, onClose }: { id: number | undefined; onClose: () => void }) {
  const entryQ = useAccountingEntry(id);
  const updateEntry = useUpdateAccountingEntry();
  const notifySuccess = useNotificationStore((s) => s.notifySuccess);
  const notifyError = useNotificationStore((s) => s.notifyError);
  const [description, setDescription] = useState('');
  const [comment, setComment] = useState('');

  useEffect(() => {
    if (entryQ.data) {
      setDescription(entryQ.data.description);
      setComment(entryQ.data.comment ?? '');
    }
  }, [entryQ.data]);

  const save = async () => {
    if (!id) return;
    try {
      await updateEntry.mutateAsync({ id, p: { description, comment } });
      notifySuccess('Écriture mise à jour.');
    } catch (e) {
      notifyError(extractErrorMessage(e));
    }
  };

  const e = entryQ.data;

  return (
    <Drawer anchor="right" open={!!id} onClose={onClose}>
      <Box sx={{ width: { xs: '100vw', sm: 480 }, p: 3 }} role="presentation">
        <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
          <Typography variant="h6">Détail de l'écriture {e ? `#${e.id}` : ''}</Typography>
          <IconButton onClick={onClose}><CloseIcon /></IconButton>
        </Stack>
        <Divider sx={{ mb: 2 }} />

        {!e ? (
          <Typography color="text.secondary">Chargement…</Typography>
        ) : (
          <Stack spacing={2}>
            {e.isLocked && (
              <Alert severity="info" icon={<LockIcon fontSize="small" />}>
                Cette écriture est verrouillée (batch exporté) : lecture seule.
              </Alert>
            )}

            <Box>
              <Typography variant="overline" color="text.secondary">Batch</Typography>
              <Typography>{e.batchReference} — <StatusBadge value={e.batchStatus} /></Typography>
              <Typography variant="caption" color="text.secondary">Généré le {formatDate(e.generationDate, true)}</Typography>
            </Box>

            <Box>
              <Typography variant="overline" color="text.secondary">Opération de caisse</Typography>
              {e.cashOperationId ? (
                <>
                  <Typography>{e.cashOperationRef}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Caisse : {e.cashRegisterCode ?? '—'} · Session : #{e.cashSessionId ?? '—'} · Catégorie : {e.categoryLabel ?? '—'}
                  </Typography>
                </>
              ) : (
                <Typography variant="body2" color="text.secondary">Écriture de synthèse (mode centralisé) — pas d'opération unique associée.</Typography>
              )}
              <Typography variant="body2" color="text.secondary">Utilisateur : {e.userName || '—'}</Typography>
            </Box>

            <Grid container spacing={2}>
              <Grid item xs={6}>
                <Typography variant="overline" color="text.secondary">Journal</Typography>
                <Typography>{e.journalCode} — {e.journalName}</Typography>
              </Grid>
              <Grid item xs={6}>
                <Typography variant="overline" color="text.secondary">Compte</Typography>
                <Typography>{e.accountNumber} — {e.accountName}</Typography>
              </Grid>
            </Grid>

            <Grid container spacing={2}>
              <Grid item xs={6}>
                <Typography variant="overline" color="text.secondary">Débit</Typography>
                <Typography><CurrencyDisplay value={e.debit || undefined} /></Typography>
              </Grid>
              <Grid item xs={6}>
                <Typography variant="overline" color="text.secondary">Crédit</Typography>
                <Typography><CurrencyDisplay value={e.credit || undefined} /></Typography>
              </Grid>
            </Grid>

            <TextField
              label="Libellé" fullWidth multiline minRows={2}
              value={description} onChange={(ev) => setDescription(ev.target.value)}
              disabled={e.isLocked}
            />
            <TextField
              label="Commentaire" fullWidth multiline minRows={3}
              value={comment} onChange={(ev) => setComment(ev.target.value)}
              disabled={e.isLocked}
              helperText="Seuls le libellé et le commentaire sont modifiables."
            />

            {!e.isLocked && (
              <Button variant="contained" startIcon={<SaveIcon />} onClick={save} disabled={updateEntry.isPending}>
                Enregistrer
              </Button>
            )}
          </Stack>
        )}
      </Box>
    </Drawer>
  );
}
