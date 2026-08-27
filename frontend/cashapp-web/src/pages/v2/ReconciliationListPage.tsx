import { useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { useNavigate } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { StatusBadge } from '@/components/common/StatusBadge';
import { useCreateReconciliation, useReconciliations } from '@/modules/reconciliation/hooks';
import { useAuthStore } from '@/app/store/authStore';
import { ReconciliationBatchType } from '@/types/v2Enums';
import { formatDate } from '@/utils/format';
import { extractErrorMessage } from '@/api/client';
import type { ReconciliationBatchListItem } from '@/types';

export function ReconciliationListPage() {
  const navigate = useNavigate();
  const registers = useAuthStore((s) => s.cashRegisters);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [open, setOpen] = useState(false);
  const { data, isLoading } = useReconciliations(page, pageSize);
  const create = useCreateReconciliation();

  const [batchType, setBatchType] = useState<keyof typeof ReconciliationBatchType>('GENERIC');
  const [cashRegisterId, setCashRegisterId] = useState<number | ''>('');
  const [notes, setNotes] = useState('');

  const columns: Column<ReconciliationBatchListItem>[] = [
    { key: 'ref', header: 'Référence', render: (r) => r.reference },
    { key: 'type', header: 'Type', render: (r) => r.batchType },
    { key: 'caisse', header: 'Caisse', render: (r) => r.cashRegisterCode ?? '—' },
    { key: 'created', header: 'Créé par', render: (r) => `${r.createdByName} — ${formatDate(r.createdAt, true)}` },
    { key: 'status', header: 'Statut', render: (r) => <StatusBadge value={r.status} /> }
  ];

  return (
    <PageContainer>
      <PageHeader
        title="Rapprochement"
        actions={<Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>Nouveau batch</Button>}
      />
      <AppTable
        columns={columns} rows={data?.items} rowKey={(r) => r.id} isLoading={isLoading}
        onRowClick={(r) => navigate(`/reconciliation/${r.id}`)}
        maxHeight="calc(100vh - 280px)"
        pagination={{ page, pageSize, total: data?.totalCount ?? 0, onPageChange: setPage, onPageSizeChange: (s) => { setPageSize(s); setPage(1); } }}
      />

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Nouveau batch de rapprochement</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            <TextField select size="small" fullWidth label="Type" value={batchType} onChange={(e) => setBatchType(e.target.value as keyof typeof ReconciliationBatchType)}>
              {Object.values(ReconciliationBatchType).map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
            </TextField>
            <TextField select size="small" fullWidth label="Caisse (optionnel)" value={cashRegisterId} onChange={(e) => setCashRegisterId(e.target.value ? Number(e.target.value) : '')}>
              <MenuItem value="">Aucune (générique)</MenuItem>
              {registers.map((r) => <MenuItem key={r.id} value={r.id}>{r.code} — {r.name}</MenuItem>)}
            </TextField>
            <TextField multiline rows={2} size="small" fullWidth label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
            {create.isError && <Alert severity="error">{extractErrorMessage(create.error)}</Alert>}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Annuler</Button>
          <Button variant="contained" disabled={create.isPending}
            onClick={async () => {
              try {
                const r = await create.mutateAsync({
                  batchType,
                  cashRegisterId: cashRegisterId ? Number(cashRegisterId) : undefined,
                  notes: notes || undefined
                });
                setOpen(false); setNotes('');
                navigate(`/reconciliation/${r.id}`);
              } catch (e) { alert(extractErrorMessage(e)); }
            }}>Créer</Button>
        </DialogActions>
      </Dialog>
    </PageContainer>
  );
}
