import { Fragment, useEffect, useMemo, useState } from 'react';
import {
  Box, Card, CardContent, IconButton, MenuItem, Paper, Stack,
  Table, TableBody, TableCell, TableContainer, TableHead, TablePagination,
  TableRow, TableSortLabel, TextField, Typography
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong';
import { useNavigate } from 'react-router-dom';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { EmptyState } from '@/components/common/EmptyState';
import { StatusBadge } from '@/components/common/StatusBadge';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { ApprovalRequestDialog } from '@/components/dialogs/ApprovalRequestDialog';
import { useApprovalRequests } from '@/modules/approvals/hooks';
import { downloadApprovalRequestPdf } from '@/api/exportsApi';
import { useNotificationStore } from '@/app/store/notificationStore';
import { extractErrorMessage } from '@/api/client';
import { ApprovalStatus, ApprovalTargetType } from '@/types/v2Enums';
import { formatDate } from '@/utils/format';
import type { ApprovalRequestListItem } from '@/types';

type SortField = 'date' | 'amount';
type GroupBy = '' | 'date' | 'target' | 'status' | 'caisse' | 'session';

const STORAGE_KEY = 'approvalReq:listState';

interface PersistedState {
  status: string;
  target: string;
  from: string;
  to: string;
  pageSize: number;
  sortBy: SortField;
  sortDir: 'asc' | 'desc';
  groupBy: GroupBy;
}
const DEFAULT: PersistedState = {
  status: ApprovalStatus.PENDING, target: '', from: '', to: '',
  pageSize: 50, sortBy: 'date', sortDir: 'desc', groupBy: ''
};
function loadState(): PersistedState {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return DEFAULT;
    return { ...DEFAULT, ...(JSON.parse(raw) as Partial<PersistedState>) };
  } catch { return DEFAULT; }
}

export function ApprovalRequestsPage() {
  const navigate = useNavigate();
  const notifyError = useNotificationStore((s) => s.notifyError);
  const notifySuccess = useNotificationStore((s) => s.notifySuccess);
  const initial = useMemo(loadState, []);

  const [status, setStatus] = useState(initial.status);
  const [target, setTarget] = useState(initial.target);
  const [from, setFrom] = useState(initial.from);
  const [to, setTo] = useState(initial.to);
  const [pageSize, setPageSize] = useState(initial.pageSize);
  const [sortBy, setSortBy] = useState<SortField>(initial.sortBy);
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>(initial.sortDir);
  const [groupBy, setGroupBy] = useState<GroupBy>(initial.groupBy);
  const [page, setPage] = useState(1);
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({});
  const [popupId, setPopupId] = useState<number | null>(null);

  useEffect(() => {
    const s: PersistedState = { status, target, from, to, pageSize, sortBy, sortDir, groupBy };
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(s)); } catch { /* ignore */ }
  }, [status, target, from, to, pageSize, sortBy, sortDir, groupBy]);

  const { data, isLoading } = useApprovalRequests({
    status: (status || undefined) as any,
    targetType: (target || undefined) as any,
    from: from || undefined,
    to: to || undefined,
    page, pageSize, sortBy, sortDir
  });
  const rows = data?.items ?? [];

  const resetPage = () => setPage(1);
  const toggleSort = (f: SortField) => {
    if (sortBy === f) setSortDir(sortDir === 'asc' ? 'desc' : 'asc');
    else { setSortBy(f); setSortDir('desc'); }
    resetPage();
  };

  const groups = useMemo(() => {
    if (!groupBy) return null;
    const getKey = (r: ApprovalRequestListItem): string => {
      switch (groupBy) {
        case 'date': return formatDate(r.requestedAt);
        case 'target': return `${r.targetType}`;
        case 'status': return r.status;
        case 'caisse': return r.cashRegisterCode ?? '— sans caisse —';
        case 'session': return r.cashSessionId ? `Session #${r.cashSessionId}` : '— sans session —';
      }
      return '';
    };
    const map = new Map<string, ApprovalRequestListItem[]>();
    for (const r of rows) {
      const k = getKey(r);
      if (!map.has(k)) map.set(k, []);
      map.get(k)!.push(r);
    }
    return Array.from(map.entries()).map(([key, items]) => {
      const totals = items.reduce((acc, r) => {
        if (r.amount != null) { acc.sum += r.amount; acc.cur = r.currencyCode ?? acc.cur; }
        return acc;
      }, { sum: 0, cur: undefined as string | undefined });
      return { key, items, total: totals.sum, currency: totals.cur };
    });
  }, [rows, groupBy]);

  const openRow = (r: ApprovalRequestListItem) => {
    if (r.status === ApprovalStatus.PENDING) navigate(`/approval-requests/${r.id}`);
    else setPopupId(r.id);
  };

  const printPdf = async (id: number) => {
    try { await downloadApprovalRequestPdf(id); notifySuccess('PDF généré.'); }
    catch (e) { notifyError(extractErrorMessage(e)); }
  };

  const Row = ({ r }: { r: ApprovalRequestListItem }) => (
    <TableRow hover sx={{ cursor: 'pointer' }} onClick={() => openRow(r)}>
      <TableCell>{r.requestRef}</TableCell>
      <TableCell>{r.targetType} #{r.targetEntityId}</TableCell>
      <TableCell>{r.cashRegisterCode ?? '—'}</TableCell>
      <TableCell>{r.cashSessionId ? `#${r.cashSessionId}` : '—'}</TableCell>
      <TableCell align="right"><CurrencyDisplay value={r.amount ?? undefined} currency={r.currencyCode ?? 'XOF'} /></TableCell>
      <TableCell>{r.requestedByName}</TableCell>
      <TableCell>{formatDate(r.requestedAt, true)}</TableCell>
      <TableCell><StatusBadge value={r.status} /></TableCell>
      <TableCell align="right">
        <IconButton size="small" title="Imprimer PDF" onClick={(e) => { e.stopPropagation(); printPdf(r.id); }}>
          <ReceiptLongIcon fontSize="small" />
        </IconButton>
      </TableCell>
    </TableRow>
  );

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 64px)', p: 3, gap: 2, overflow: 'hidden' }}>
      <PageHeader title="Demandes d'approbation" subtitle="File d'attente des demandes de validation" />
      <Card>
        <CardContent sx={{ py: 1.5 }}>
          <Stack direction="row" spacing={1.5} alignItems="center" sx={{ flexWrap: { xs: 'wrap', lg: 'nowrap' } }}>
            <TextField select size="small" label="Statut" value={status} onChange={(e) => { setStatus(e.target.value); resetPage(); }} sx={{ flex: 1, minWidth: 140 }}>
              <MenuItem value="">Tous</MenuItem>
              {Object.values(ApprovalStatus).map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
            </TextField>
            <TextField select size="small" label="Cible" value={target} onChange={(e) => { setTarget(e.target.value); resetPage(); }} sx={{ flex: 1, minWidth: 140 }}>
              <MenuItem value="">Toutes</MenuItem>
              {Object.values(ApprovalTargetType).map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
            </TextField>
            <TextField size="small" type="date" label="Du" InputLabelProps={{ shrink: true }} value={from} onChange={(e) => { setFrom(e.target.value); resetPage(); }} sx={{ flex: 1, minWidth: 140 }} />
            <TextField size="small" type="date" label="Au" InputLabelProps={{ shrink: true }} value={to} onChange={(e) => { setTo(e.target.value); resetPage(); }} sx={{ flex: 1, minWidth: 140 }} />
            <TextField select size="small" label="Grouper par" value={groupBy} onChange={(e) => { setGroupBy(e.target.value as GroupBy); setCollapsed({}); }} sx={{ flex: 1, minWidth: 160 }}>
              <MenuItem value="">Aucun</MenuItem>
              <MenuItem value="date">Date</MenuItem>
              <MenuItem value="target">Cible</MenuItem>
              <MenuItem value="status">Statut</MenuItem>
              <MenuItem value="caisse">Caisse</MenuItem>
              <MenuItem value="session">Session de caisse</MenuItem>
            </TextField>
          </Stack>
        </CardContent>
      </Card>

      {isLoading ? <LoadingScreen /> : rows.length === 0 ? (
        <Paper sx={{ flex: 1 }}><EmptyState title="Aucune demande" /></Paper>
      ) : (
        <Paper sx={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0, overflow: 'hidden' }}>
          <TableContainer sx={{ flex: 1, overflowY: 'auto' }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Référence</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Cible</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Caisse</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Session</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>
                    <TableSortLabel
                      active={sortBy === 'amount'}
                      direction={sortBy === 'amount' ? sortDir : 'desc'}
                      onClick={() => toggleSort('amount')}
                      sx={{ '& .MuiTableSortLabel-icon': { opacity: sortBy === 'amount' ? 1 : 0.4 } }}
                    >Montant</TableSortLabel>
                  </TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Demandé par</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>
                    <TableSortLabel
                      active={sortBy === 'date'}
                      direction={sortBy === 'date' ? sortDir : 'desc'}
                      onClick={() => toggleSort('date')}
                      sx={{ '& .MuiTableSortLabel-icon': { opacity: sortBy === 'date' ? 1 : 0.4 } }}
                    >Date</TableSortLabel>
                  </TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Statut</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 600, bgcolor: 'background.paper' }}></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {groups ? groups.map((g) => {
                  const isCollapsed = !!collapsed[g.key];
                  return (
                    <Fragment key={`g-${g.key}`}>
                      <TableRow sx={{ bgcolor: 'action.hover' }}>
                        <TableCell colSpan={9} sx={{ py: 0.5 }}>
                          <Stack direction="row" alignItems="center" spacing={1}>
                            <IconButton size="small" onClick={() => setCollapsed((c) => ({ ...c, [g.key]: !c[g.key] }))}>
                              {isCollapsed ? <ChevronRightIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
                            </IconButton>
                            <Typography variant="body2" fontWeight={600}>{g.key}</Typography>
                            <Typography variant="caption" color="text.secondary">({g.items.length})</Typography>
                            <Box sx={{ flex: 1 }} />
                            {g.currency && g.total > 0 && <Typography variant="body2" fontWeight={600}>Total : <CurrencyDisplay value={g.total} currency={g.currency} /></Typography>}
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

      <ApprovalRequestDialog
        requestId={popupId}
        open={popupId !== null}
        onClose={() => setPopupId(null)}
      />
    </Box>
  );
}
