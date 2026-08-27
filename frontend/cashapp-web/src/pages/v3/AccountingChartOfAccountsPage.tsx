import { zodResolver } from '@hookform/resolvers/zod';
import { Fragment, useEffect, useMemo, useState } from 'react';
import {
  Alert, Box, Button, Card, CardContent, Dialog, DialogActions, DialogContent, DialogTitle,
  Grid, IconButton, MenuItem, Paper, Stack, Switch, Table, TableBody, TableCell,
  TableContainer, TableHead, TablePagination, TableRow, TextField, Tooltip, Typography
} from '@mui/material';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import CloseIcon from '@mui/icons-material/Close';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import { useNavigate } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { EmptyState } from '@/components/common/EmptyState';
import { ConfirmDialog } from '@/components/dialogs/ConfirmDialog';
import { useNotificationStore } from '@/app/store/notificationStore';
import { extractErrorMessage } from '@/api/client';
import { useIsFeatureEnabled } from '@/hooks/useFeatures';
import {
  useAccountingAccounts,
  useCreateAccountingAccount,
  useDeleteAccountingAccount,
  useSetAccountingAccountStatus,
  useUpdateAccountingAccount
} from '@/modules/accounting/hooks';
import { AccountingAccountNature } from '@/types/v2Enums';
import { FeatureCodes } from '@/types/enums';
import type { AccountingAccountFilter, AccountingAccountListItem } from '@/types';

const NATURE_LABELS: Record<string, string> = {
  ASSET: 'Actif', LIABILITY: 'Passif', EXPENSE: 'Charge', REVENUE: 'Produit',
  BANK: 'Banque', CASH: 'Caisse', EQUITY: 'Capitaux propres', OTHER: 'Autre'
};
const NATURE_ORDER = Object.keys(NATURE_LABELS);

const schema = z.object({
  accountNumber: z.string().min(1, 'Requis').max(20),
  name: z.string().min(1, 'Requis').max(150),
  nature: z.nativeEnum(AccountingAccountNature)
});
type FormValues = z.infer<typeof schema>;

const EMPTY_FILTER: AccountingAccountFilter = { page: 1, pageSize: 25 };

export function AccountingChartOfAccountsPage() {
  const navigate = useNavigate();
  const importsEnabled = useIsFeatureEnabled(FeatureCodes.ADV_IMPORTS);
  const [filter, setFilter] = useState<AccountingAccountFilter>(EMPTY_FILTER);
  const [search, setSearch] = useState('');
  const accountsQ = useAccountingAccounts(filter);
  const setStatus = useSetAccountingAccountStatus();
  const del = useDeleteAccountingAccount();
  const notifyError = useNotificationStore((s) => s.notifyError);

  const [editing, setEditing] = useState<AccountingAccountListItem | null>(null);
  const [creating, setCreating] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<AccountingAccountListItem | null>(null);
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({});

  const applySearch = () => setFilter((f) => ({ ...f, search: search || undefined, page: 1 }));

  const doDelete = async () => {
    if (!deleteTarget) return;
    try { await del.mutateAsync(deleteTarget.id); setDeleteTarget(null); }
    catch (e) { notifyError(extractErrorMessage(e)); setDeleteTarget(null); }
  };

  const items = accountsQ.data?.items ?? [];

  // Groupe les comptes de la page courante par Nature, avec sections repliables.
  // La pagination reste serveur (25/50/100) : une Nature peut donc réapparaître sur une autre page.
  const grouped = useMemo(() => {
    const map = new Map<string, AccountingAccountListItem[]>();
    for (const a of items) {
      if (!map.has(a.nature)) map.set(a.nature, []);
      map.get(a.nature)!.push(a);
    }
    return NATURE_ORDER
      .filter((n) => map.has(n))
      .map((n) => ({ key: n, label: NATURE_LABELS[n], items: map.get(n)! }));
  }, [items]);

  return (
    <PageContainer maxWidth={false}>
      <Box sx={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 112px)' }}>
      <PageHeader
        title="Plan comptable"
        subtitle="Liste des comptes comptables"
        actions={
          <Stack direction="row" spacing={1}>
            {importsEnabled && (
              <Button variant="outlined" startIcon={<UploadFileIcon />} onClick={() => navigate('/imports?type=ACCOUNTING_ACCOUNTS')}>
                Importer
              </Button>
            )}
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreating(true)}>Nouveau compte</Button>
          </Stack>
        }
      />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} sm={5}>
              <TextField fullWidth size="small" label="Recherche (numéro ou libellé)" value={search}
                onChange={(e) => setSearch(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') applySearch(); }} />
            </Grid>
            <Grid item xs={12} sm={3}>
              <TextField select fullWidth size="small" label="Nature"
                value={filter.nature ?? ''} onChange={(e) => setFilter((f) => ({ ...f, nature: (e.target.value || undefined) as AccountingAccountNature | undefined, page: 1 }))}>
                <MenuItem value="">Toutes</MenuItem>
                {Object.values(AccountingAccountNature).map((n) => <MenuItem key={n} value={n}>{NATURE_LABELS[n]}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={2}>
              <TextField select fullWidth size="small" label="État"
                value={filter.isActive === undefined ? '' : String(filter.isActive)}
                onChange={(e) => setFilter((f) => ({ ...f, isActive: e.target.value === '' ? undefined : e.target.value === 'true', page: 1 }))}>
                <MenuItem value="">Tous</MenuItem>
                <MenuItem value="true">Actif</MenuItem>
                <MenuItem value="false">Inactif</MenuItem>
              </TextField>
            </Grid>
            <Grid item xs={12} sm={2}>
              <Button fullWidth variant="outlined" onClick={applySearch}>Rechercher</Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {accountsQ.isLoading ? <LoadingScreen /> : items.length === 0 ? (
        <Paper><EmptyState title="Aucun compte" /></Paper>
      ) : (
        <Paper sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, overflow: 'hidden' }}>
          <TableContainer sx={{ flex: 1, maxHeight: '100%', overflowY: 'auto' }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Compte</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Libellé</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>État</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 600, bgcolor: 'background.paper' }}></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {grouped.map((g) => {
                  const isCollapsed = !!collapsed[g.key];
                  return (
                    <Fragment key={g.key}>
                      <TableRow sx={{ bgcolor: 'action.hover' }}>
                        <TableCell colSpan={4} sx={{ py: 0.5 }}>
                          <Stack direction="row" alignItems="center" spacing={1}>
                            <IconButton size="small" onClick={() => setCollapsed((c) => ({ ...c, [g.key]: !c[g.key] }))}>
                              {isCollapsed ? <ChevronRightIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
                            </IconButton>
                            <Typography variant="body2" fontWeight={600}>{g.label}</Typography>
                            <Typography variant="caption" color="text.secondary">({g.items.length})</Typography>
                          </Stack>
                        </TableCell>
                      </TableRow>
                      {!isCollapsed && g.items.map((a) => (
                        <TableRow key={a.id} hover>
                          <TableCell>{a.accountNumber}</TableCell>
                          <TableCell>{a.name}</TableCell>
                          <TableCell>
                            <Switch size="small" checked={a.isActive} onChange={(_, v) => setStatus.mutate({ id: a.id, isActive: v })} />
                          </TableCell>
                          <TableCell align="right">
                            <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                              <Tooltip title="Modifier"><IconButton size="small" onClick={() => setEditing(a)}><EditIcon fontSize="small" /></IconButton></Tooltip>
                              <Tooltip title="Supprimer"><IconButton size="small" onClick={() => setDeleteTarget(a)}><DeleteIcon fontSize="small" /></IconButton></Tooltip>
                            </Stack>
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
            count={accountsQ.data?.totalCount ?? 0}
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

      <AccountFormDialog open={creating} onClose={() => setCreating(false)} mode="create" />
      <AccountFormDialog open={!!editing} onClose={() => setEditing(null)} mode="edit" account={editing ?? undefined} />

      <ConfirmDialog
        open={!!deleteTarget}
        title="Supprimer ce compte ?"
        description={`Le compte ${deleteTarget?.accountNumber} sera définitivement supprimé s'il n'est utilisé par aucune caisse, catégorie ou écriture.`}
        confirmLabel="Supprimer"
        destructive
        onConfirm={doDelete}
        onClose={() => setDeleteTarget(null)}
      />
    </PageContainer>
  );
}

function AccountFormDialog({ open, onClose, mode, account }: {
  open: boolean; onClose: () => void; mode: 'create' | 'edit'; account?: AccountingAccountListItem;
}) {
  const create = useCreateAccountingAccount();
  const update = useUpdateAccountingAccount();
  const notifySuccess = useNotificationStore((s) => s.notifySuccess);
  const notifyError = useNotificationStore((s) => s.notifyError);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { accountNumber: '', name: '', nature: AccountingAccountNature.ASSET }
  });

  useEffect(() => {
    if (open) {
      form.reset(account
        ? { accountNumber: account.accountNumber, name: account.name, nature: account.nature }
        : { accountNumber: '', name: '', nature: AccountingAccountNature.ASSET });
    }
  }, [open, account, form]);

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      if (mode === 'create') {
        await create.mutateAsync(values);
      } else if (account) {
        await update.mutateAsync({ id: account.id, p: { name: values.name, nature: values.nature } });
      }
      notifySuccess(mode === 'create' ? 'Compte créé.' : 'Compte mis à jour.');
      onClose();
    } catch (e) { notifyError(extractErrorMessage(e)); }
  });

  const pending = create.isPending || update.isPending;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle sx={{ pr: 6 }}>
        {mode === 'create' ? 'Nouveau compte' : 'Modifier le compte'}
        <IconButton onClick={onClose} sx={{ position: 'absolute', right: 8, top: 8 }}><CloseIcon /></IconButton>
      </DialogTitle>
      <form onSubmit={onSubmit}>
        <DialogContent dividers>
          <Stack spacing={2}>
            <TextField
              fullWidth size="small" label="Numéro de compte" disabled={mode === 'edit'}
              {...form.register('accountNumber')}
              error={!!form.formState.errors.accountNumber}
              helperText={form.formState.errors.accountNumber?.message}
            />
            <TextField
              fullWidth size="small" label="Libellé"
              {...form.register('name')}
              error={!!form.formState.errors.name}
              helperText={form.formState.errors.name?.message}
            />
            <TextField
              select fullWidth size="small" label="Nature"
              {...form.register('nature')}
            >
              {Object.values(AccountingAccountNature).map((n) => <MenuItem key={n} value={n}>{NATURE_LABELS[n]}</MenuItem>)}
            </TextField>
            {(create.isError || update.isError) && (
              <Alert severity="error">{extractErrorMessage((create.error ?? update.error) as unknown)}</Alert>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Annuler</Button>
          <Button type="submit" variant="contained" disabled={pending}>Enregistrer</Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
