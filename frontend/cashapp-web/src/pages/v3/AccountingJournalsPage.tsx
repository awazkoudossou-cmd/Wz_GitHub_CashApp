import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useMemo, useState } from 'react';
import {
  Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle,
  IconButton, Stack, Switch, TextField, Tooltip
} from '@mui/material';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import CloseIcon from '@mui/icons-material/Close';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { ConfirmDialog } from '@/components/dialogs/ConfirmDialog';
import { useNotificationStore } from '@/app/store/notificationStore';
import { extractErrorMessage } from '@/api/client';
import {
  useAccountingJournals,
  useCreateAccountingJournal,
  useDeleteAccountingJournal,
  useSetAccountingJournalStatus,
  useUpdateAccountingJournal
} from '@/modules/accounting/hooks';
import type { AccountingJournalListItem } from '@/types';

const schema = z.object({
  code: z.string().min(1, 'Requis').max(20),
  name: z.string().min(1, 'Requis').max(150),
  description: z.string().max(500).optional()
});
type FormValues = z.infer<typeof schema>;

export function AccountingJournalsPage() {
  const journalsQ = useAccountingJournals();
  const setStatus = useSetAccountingJournalStatus();
  const del = useDeleteAccountingJournal();
  const notifyError = useNotificationStore((s) => s.notifyError);
  const [search, setSearch] = useState('');

  const [editing, setEditing] = useState<AccountingJournalListItem | null>(null);
  const [creating, setCreating] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<AccountingJournalListItem | null>(null);

  const rows = useMemo(() => {
    const all = journalsQ.data ?? [];
    if (!search.trim()) return all;
    const term = search.trim().toLowerCase();
    return all.filter((j) => j.code.toLowerCase().includes(term) || j.name.toLowerCase().includes(term));
  }, [journalsQ.data, search]);

  const doDelete = async () => {
    if (!deleteTarget) return;
    try { await del.mutateAsync(deleteTarget.id); setDeleteTarget(null); }
    catch (e) { notifyError(extractErrorMessage(e)); setDeleteTarget(null); }
  };

  const columns: Column<AccountingJournalListItem>[] = [
    { key: 'code', header: 'Code', render: (j) => j.code },
    { key: 'name', header: 'Nom', render: (j) => j.name },
    { key: 'description', header: 'Description', render: (j) => j.description ?? '—' },
    { key: 'status', header: 'État', render: (j) => (
      <Switch size="small" checked={j.isActive} onChange={(_, v) => setStatus.mutate({ id: j.id, isActive: v })} />
    ) },
    { key: 'actions', header: '', align: 'right', render: (j) => (
      <Stack direction="row" spacing={0.5} justifyContent="flex-end">
        <Tooltip title="Modifier"><IconButton size="small" onClick={() => setEditing(j)}><EditIcon fontSize="small" /></IconButton></Tooltip>
        <Tooltip title="Supprimer"><IconButton size="small" onClick={() => setDeleteTarget(j)}><DeleteIcon fontSize="small" /></IconButton></Tooltip>
      </Stack>
    ) }
  ];

  return (
    <PageContainer maxWidth="lg">
      <PageHeader
        title="Journaux comptables"
        subtitle="Liste des journaux comptables"
        actions={<Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreating(true)}>Nouveau journal</Button>}
      />

      <TextField
        fullWidth size="small" label="Recherche (code ou nom)" sx={{ mb: 2 }}
        value={search} onChange={(e) => setSearch(e.target.value)}
      />

      <AppTable columns={columns} rows={rows} rowKey={(j) => j.id} isLoading={journalsQ.isLoading} emptyTitle="Aucun journal" />

      <JournalFormDialog open={creating} onClose={() => setCreating(false)} mode="create" />
      <JournalFormDialog open={!!editing} onClose={() => setEditing(null)} mode="edit" journal={editing ?? undefined} />

      <ConfirmDialog
        open={!!deleteTarget}
        title="Supprimer ce journal ?"
        description={`Le journal ${deleteTarget?.code} sera définitivement supprimé s'il n'est utilisé par aucune caisse ni écriture.`}
        confirmLabel="Supprimer"
        destructive
        onConfirm={doDelete}
        onClose={() => setDeleteTarget(null)}
      />
    </PageContainer>
  );
}

function JournalFormDialog({ open, onClose, mode, journal }: {
  open: boolean; onClose: () => void; mode: 'create' | 'edit'; journal?: AccountingJournalListItem;
}) {
  const create = useCreateAccountingJournal();
  const update = useUpdateAccountingJournal();
  const notifySuccess = useNotificationStore((s) => s.notifySuccess);
  const notifyError = useNotificationStore((s) => s.notifyError);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { code: '', name: '', description: '' }
  });

  useEffect(() => {
    if (open) {
      form.reset(journal
        ? { code: journal.code, name: journal.name, description: journal.description ?? '' }
        : { code: '', name: '', description: '' });
    }
  }, [open, journal, form]);

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      if (mode === 'create') {
        await create.mutateAsync(values);
      } else if (journal) {
        await update.mutateAsync({ id: journal.id, p: { name: values.name, description: values.description } });
      }
      notifySuccess(mode === 'create' ? 'Journal créé.' : 'Journal mis à jour.');
      onClose();
    } catch (e) { notifyError(extractErrorMessage(e)); }
  });

  const pending = create.isPending || update.isPending;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle sx={{ pr: 6 }}>
        {mode === 'create' ? 'Nouveau journal' : 'Modifier le journal'}
        <IconButton onClick={onClose} sx={{ position: 'absolute', right: 8, top: 8 }}><CloseIcon /></IconButton>
      </DialogTitle>
      <form onSubmit={onSubmit}>
        <DialogContent dividers>
          <Stack spacing={2}>
            <TextField
              fullWidth size="small" label="Code" disabled={mode === 'edit'}
              {...form.register('code')}
              error={!!form.formState.errors.code}
              helperText={form.formState.errors.code?.message}
            />
            <TextField
              fullWidth size="small" label="Nom"
              {...form.register('name')}
              error={!!form.formState.errors.name}
              helperText={form.formState.errors.name?.message}
            />
            <TextField
              fullWidth size="small" label="Description" multiline rows={2}
              {...form.register('description')}
            />
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
