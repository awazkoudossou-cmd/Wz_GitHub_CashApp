import {
  Box, Button, Card, CardContent, Grid, IconButton, MenuItem,
  Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead,
  TablePagination, TableRow, TableSortLabel, TextField, Tooltip, Typography
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import CancelIcon from '@mui/icons-material/Cancel';
import LockIcon from '@mui/icons-material/Lock';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import DownloadIcon from '@mui/icons-material/Download';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { Fragment, useEffect, useMemo, useState } from 'react';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { StatusChip } from '@/components/common/StatusChip';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { EmptyState } from '@/components/common/EmptyState';
import { OperationDetailDialog } from '@/components/dialogs/OperationDetailDialog';
import { useCancelCashOperation, useCashOperations } from '@/modules/cash-operations/hooks';
import { useCashSessions } from '@/modules/cash-sessions/hooks';
import { useGeneralSettings } from '@/modules/settings/hooks';
import { useExportOperations } from '@/modules/exports/hooks';
import { useNotificationStore } from '@/app/store/notificationStore';
import { extractErrorMessage } from '@/api/client';
import { useAuthStore } from '@/app/store/authStore';
import { CashSessionStatus, OperationDirection } from '@/types/enums';
import type { CashOperationListItem, OperationDirection as Dir } from '@/types';
import { formatDate } from '@/utils/format';

type SortField = 'date' | 'ref';
type GroupBy = '' | 'date' | 'direction' | 'caisse' | 'categorie' | 'session';

const STORAGE_KEY = 'cashOps:listState';

interface PersistedState {
  direction: Dir | '';
  from: string;
  to: string;
  pageSize: number;
  sortBy: SortField;
  sortDir: 'asc' | 'desc';
  groupBy: GroupBy;
}

const DEFAULT_STATE: PersistedState = {
  direction: '',
  from: '',
  to: '',
  pageSize: 100,
  sortBy: 'date',
  sortDir: 'desc',
  groupBy: ''
};

function loadPersisted(): PersistedState {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return DEFAULT_STATE;
    return { ...DEFAULT_STATE, ...(JSON.parse(raw) as Partial<PersistedState>) };
  } catch {
    return DEFAULT_STATE;
  }
}

export function CashOperationsListPage() {
  const navigate = useNavigate();
  const cashRegisterId = useAuthStore((s) => s.selectedCashRegisterId) ?? undefined;
  const initial = useMemo(loadPersisted, []);

  const [direction, setDirection] = useState<Dir | ''>(initial.direction);
  const [from, setFrom] = useState(initial.from);
  const [to, setTo] = useState(initial.to);
  const [pageSize, setPageSize] = useState(initial.pageSize);
  const [sortBy, setSortBy] = useState<SortField>(initial.sortBy);
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>(initial.sortDir);
  const [groupBy, setGroupBy] = useState<GroupBy>(initial.groupBy);
  const [page, setPage] = useState(1);
  // Le flag « inclure les ops rejetées/annulées » est désormais piloté par les paramètres généraux.
  const settings = useGeneralSettings();
  const includeDeleted = !!settings.data?.showDeletedOperationsInList;
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({});
  const [popupOpId, setPopupOpId] = useState<number | null>(null);

  // Persiste filtres + tri + groupement à chaque changement.
  useEffect(() => {
    const state: PersistedState = { direction, from, to, pageSize, sortBy, sortDir, groupBy };
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(state)); } catch { /* ignore */ }
  }, [direction, from, to, pageSize, sortBy, sortDir, groupBy]);

  const resetPage = () => setPage(1);

  const { data, isLoading } = useCashOperations({
    cashRegisterId,
    direction: direction || undefined,
    from: from || undefined,
    to: to || undefined,
    page,
    pageSize,
    sortBy,
    sortDir,
    includeDeleted
  });
  const cancel = useCancelCashOperation();
  const exportOps = useExportOperations();
  const notifySuccess = useNotificationStore((s) => s.notifySuccess);
  const notifyError = useNotificationStore((s) => s.notifyError);
  // Charge les sessions uniquement quand on groupe par session, pour enrichir le label avec dates d'ouverture/clôture.
  const sessions = useCashSessions(groupBy === 'session' ? cashRegisterId : undefined);
  const sessionMap = useMemo(() => {
    const m = new Map<number, { openedAt: string; closedAt?: string | null }>();
    sessions.data?.forEach((s) => m.set(s.id, { openedAt: s.openedAt, closedAt: s.closedAt }));
    return m;
  }, [sessions.data]);

  const onCancel = async (op: CashOperationListItem) => {
    const reason = window.prompt('Motif d\'annulation ?');
    if (!reason) return;
    await cancel.mutateAsync({ id: op.id, reason });
  };

  const toggleSort = (field: SortField) => {
    if (sortBy === field) setSortDir(sortDir === 'asc' ? 'desc' : 'asc');
    else { setSortBy(field); setSortDir('desc'); }
    setPage(1);
  };

  const onExportExcel = async () => {
    try {
      await exportOps.mutateAsync({
        // Plage large par défaut si aucune date n'est filtrée, pour tout exporter.
        from: from || '2000-01-01',
        to: to || new Date().toISOString().slice(0, 10),
        cashRegisterId,
        direction: direction || undefined,
        includeDeleted,
        format: 'xlsx'
      });
      notifySuccess('Export Excel généré.');
    } catch (e) {
      notifyError(extractErrorMessage(e));
    }
  };

  const rows = data?.items ?? [];

  const groups = useMemo(() => {
    if (!groupBy) return null;
    const getKey = (r: CashOperationListItem): string => {
      switch (groupBy) {
        case 'date': return formatDate(r.operationDate);
        case 'direction': return r.direction;
        case 'caisse': return r.cashRegisterCode;
        case 'categorie': return r.categoryLabel;
        case 'session': return `Session #${r.cashSessionId}`;
      }
      return '';
    };
    const map = new Map<string, CashOperationListItem[]>();
    for (const r of rows) {
      const k = getKey(r);
      if (!map.has(k)) map.set(k, []);
      map.get(k)!.push(r);
    }
    return Array.from(map.entries()).map(([key, items]) => {
      const total = items.reduce((s, r) => s + (r.direction === OperationDirection.IN ? r.amount : -r.amount), 0);
      const currency = items[0]?.currencyCode;
      return { key, items, total, currency };
    });
  }, [rows, groupBy]);

  const renderActions = (r: CashOperationListItem) => {
    if (r.isDeleted || r.isLocked || r.isPendingCancellation || r.amount < 0) return null;
    const sessionOpen = r.cashSessionStatus === CashSessionStatus.OPEN;
    if (!sessionOpen) return null;
    return (
      <Button size="small" color="error" startIcon={<CancelIcon />} onClick={(e) => { e.stopPropagation(); onCancel(r); }}>Annuler</Button>
    );
  };

  const renderStatus = (r: CashOperationListItem) =>
    r.isDeleted ? <StatusChip status="ANNULÉE" /> :
    r.isPendingApproval ? <StatusChip status="EN ATTENTE" /> :
    r.isPendingCancellation ? <StatusChip status="ANNUL. EN ATTENTE" /> :
    r.isLocked ? (
      <Tooltip title="Opération verrouillée (liée à un transfert ou dépôt banque)">
        <LockIcon color="primary" fontSize="small" />
      </Tooltip>
    ) : null;

  const openOp = (r: CashOperationListItem) => {
    // Session fermée → popup en lecture seule ; sinon page détail (avec actions).
    if (r.cashSessionStatus !== CashSessionStatus.OPEN) setPopupOpId(r.id);
    else navigate(`/cash-operations/${r.id}`);
  };

  const Row = ({ r }: { r: CashOperationListItem }) => (
    <TableRow hover sx={{ cursor: 'pointer' }} onClick={() => openOp(r)}>
      <TableCell>{r.operationRef}</TableCell>
      <TableCell>{formatDate(r.operationDate)}</TableCell>
      <TableCell>{r.cashRegisterCode}</TableCell>
      <TableCell><StatusChip status={r.direction} variant="direction" /></TableCell>
      <TableCell>{r.categoryLabel}</TableCell>
      <TableCell>{r.label}</TableCell>
      <TableCell align="right"><CurrencyDisplay value={r.amount} currency={r.currencyCode} /></TableCell>
      <TableCell>{r.thirdPartyName ?? '—'}</TableCell>
      <TableCell align="center">{renderStatus(r)}</TableCell>
      <TableCell align="right">{renderActions(r)}</TableCell>
    </TableRow>
  );

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 64px)', p: 3, gap: 2, overflow: 'hidden' }}>
      <PageHeader
        title="Opérations"
        actions={
          <Stack direction="row" spacing={1}>
            <Button
              variant="outlined"
              startIcon={<DownloadIcon />}
              onClick={onExportExcel}
              disabled={exportOps.isPending}
            >
              Export Excel
            </Button>
            <Button component={RouterLink} to="/cash-operations/new" variant="contained" startIcon={<AddIcon />}>
              Nouvelle opération
            </Button>
          </Stack>
        }
      />
      <Card>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={3}><TextField fullWidth size="small" type="date" label="Du" InputLabelProps={{ shrink: true }} value={from} onChange={(e) => { setFrom(e.target.value); resetPage(); }} /></Grid>
            <Grid item xs={12} sm={3}><TextField fullWidth size="small" type="date" label="Au" InputLabelProps={{ shrink: true }} value={to} onChange={(e) => { setTo(e.target.value); resetPage(); }} /></Grid>
            <Grid item xs={12} sm={3}>
              <TextField select fullWidth size="small" label="Direction" value={direction} onChange={(e) => { setDirection(e.target.value as Dir | ''); resetPage(); }}>
                <MenuItem value="">Toutes</MenuItem>
                <MenuItem value={OperationDirection.IN}>IN</MenuItem>
                <MenuItem value={OperationDirection.OUT}>OUT</MenuItem>
              </TextField>
            </Grid>
            <Grid item xs={12} sm={3}>
              <TextField select fullWidth size="small" label="Grouper par" value={groupBy} onChange={(e) => { setGroupBy(e.target.value as GroupBy); setCollapsed({}); }}>
                <MenuItem value="">Aucun</MenuItem>
                <MenuItem value="date">Date</MenuItem>
                <MenuItem value="direction">Direction</MenuItem>
                <MenuItem value="caisse">Caisse</MenuItem>
                <MenuItem value="categorie">Catégorie</MenuItem>
                <MenuItem value="session">Session de caisse</MenuItem>
              </TextField>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {isLoading ? <LoadingScreen /> : rows.length === 0 ? (
        <Paper sx={{ flex: 1 }}><EmptyState title="Aucune opération" /></Paper>
      ) : (
        <Paper sx={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0, overflow: 'hidden' }}>
          <TableContainer sx={{ flex: 1, overflowY: 'auto' }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>
                    <TableSortLabel
                      active={sortBy === 'ref'}
                      direction={sortBy === 'ref' ? sortDir : 'desc'}
                      onClick={() => toggleSort('ref')}
                      sx={{ '& .MuiTableSortLabel-icon': { opacity: sortBy === 'ref' ? 1 : 0.4 } }}
                    >Réf.</TableSortLabel>
                  </TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>
                    <TableSortLabel
                      active={sortBy === 'date'}
                      direction={sortBy === 'date' ? sortDir : 'desc'}
                      onClick={() => toggleSort('date')}
                      sx={{ '& .MuiTableSortLabel-icon': { opacity: sortBy === 'date' ? 1 : 0.4 } }}
                    >Date</TableSortLabel>
                  </TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Caisse</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Dir.</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Catégorie</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Libellé</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Montant</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Tiers</TableCell>
                  <TableCell align="center" sx={{ fontWeight: 600, bgcolor: 'background.paper' }}></TableCell>
                  <TableCell align="right" sx={{ fontWeight: 600, bgcolor: 'background.paper' }}></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {groups ? groups.map((g) => {
                  const isCollapsed = !!collapsed[g.key];
                  // Pour le groupement par session, ajoute les dates d'ouverture / clôture.
                  let sessionInfo: string | null = null;
                  if (groupBy === 'session') {
                    const sid = g.items[0]?.cashSessionId;
                    const info = sid ? sessionMap.get(sid) : undefined;
                    if (info) {
                      sessionInfo = `Ouverte le ${formatDate(info.openedAt, true)}` +
                        (info.closedAt ? ` — Fermée le ${formatDate(info.closedAt, true)}` : ' — en cours');
                    }
                  }
                  return (
                    <Fragment key={`g-${g.key}`}>
                      <TableRow sx={{ bgcolor: 'action.hover' }}>
                        <TableCell colSpan={10} sx={{ py: 0.5 }}>
                          <Stack direction="row" alignItems="center" spacing={1}>
                            <IconButton size="small" onClick={() => setCollapsed((c) => ({ ...c, [g.key]: !c[g.key] }))}>
                              {isCollapsed ? <ChevronRightIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
                            </IconButton>
                            <Typography variant="body2" fontWeight={600}>{g.key}</Typography>
                            <Typography variant="caption" color="text.secondary">({g.items.length})</Typography>
                            {sessionInfo && (
                              <Typography variant="caption" color="text.secondary" sx={{ ml: 1 }}>{sessionInfo}</Typography>
                            )}
                            <Box sx={{ flex: 1 }} />
                            {g.currency && <Typography variant="body2" fontWeight={600}>Net : <CurrencyDisplay value={g.total} currency={g.currency} /></Typography>}
                          </Stack>
                        </TableCell>
                      </TableRow>
                      {!isCollapsed && g.items.map((r) => <Row key={r.id} r={r} />)}
                    </Fragment>
                  );
                }) : rows.map((r) => <Row key={r.id} r={r} />)}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination
            component="div"
            count={data?.totalCount ?? 0}
            page={Math.max(0, page - 1)}
            onPageChange={(_, p) => setPage(p + 1)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={(e) => { setPageSize(Number(e.target.value)); setPage(1); }}
            rowsPerPageOptions={[25, 50, 100, 200]}
            labelRowsPerPage="Lignes par page"
            labelDisplayedRows={({ from, to, count }) => `${from}–${to} sur ${count}`}
            sx={{ borderTop: '1px solid', borderColor: 'divider' }}
          />
        </Paper>
      )}

      <OperationDetailDialog
        opId={popupOpId}
        open={popupOpId !== null}
        onClose={() => setPopupOpId(null)}
      />
    </Box>
  );
}
