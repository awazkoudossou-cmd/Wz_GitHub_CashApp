import { useState } from 'react';
import {
  Alert,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
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
import { useAnomalies, useCreateAnomaly } from '@/modules/anomalies-v2/hooks';
import { AnomalySeverity, AnomalyStatus } from '@/types/v2Enums';
import { formatDate } from '@/utils/format';
import { extractErrorMessage } from '@/api/client';
import type { AnomalyListItem } from '@/types';

export function AnomaliesListPage() {
  const navigate = useNavigate();
  const [status, setStatus] = useState('');
  const [severity, setSeverity] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [open, setOpen] = useState(false);

  const { data, isLoading } = useAnomalies({
    status: (status || undefined) as any,
    severity: (severity || undefined) as any,
    page, pageSize
  });

  const columns: Column<AnomalyListItem>[] = [
    { key: 'ref', header: 'Référence', render: (r) => r.reference },
    { key: 'sev', header: 'Sévérité', render: (r) => <StatusBadge value={r.severity} /> },
    { key: 'status', header: 'Statut', render: (r) => <StatusBadge value={r.status} /> },
    { key: 'title', header: 'Titre', render: (r) => r.title },
    { key: 'caisse', header: 'Caisse', render: (r) => r.cashRegisterCode ?? '—' },
    { key: 'date', header: 'Détectée le', render: (r) => formatDate(r.detectedAt, true) },
    { key: 'assigned', header: 'Assignée à', render: (r) => r.assignedToName ?? '—' }
  ];

  return (
    <PageContainer>
      <PageHeader
        title="Anomalies"
        actions={<Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>Nouvelle anomalie</Button>}
      />
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={3}>
              <TextField select fullWidth size="small" label="Statut" value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}>
                <MenuItem value="">Tous</MenuItem>
                {Object.values(AnomalyStatus).map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={3}>
              <TextField select fullWidth size="small" label="Sévérité" value={severity} onChange={(e) => { setSeverity(e.target.value); setPage(1); }}>
                <MenuItem value="">Toutes</MenuItem>
                {Object.values(AnomalySeverity).map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
              </TextField>
            </Grid>
          </Grid>
        </CardContent>
      </Card>
      <AppTable
        columns={columns} rows={data?.items} rowKey={(r) => r.id} isLoading={isLoading}
        onRowClick={(r) => navigate(`/anomalies/${r.id}`)}
        maxHeight="calc(100vh - 360px)"
        pagination={{ page, pageSize, total: data?.totalCount ?? 0, onPageChange: setPage, onPageSizeChange: (s) => { setPageSize(s); setPage(1); } }}
      />
      <CreateAnomalyDialog open={open} onClose={() => setOpen(false)} />
    </PageContainer>
  );
}

function CreateAnomalyDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const create = useCreateAnomaly();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [severity, setSeverity] = useState<keyof typeof AnomalySeverity>('MEDIUM');

  const submit = async () => {
    try {
      await create.mutateAsync({ title, description: description || undefined, severity });
      onClose(); setTitle(''); setDescription('');
    } catch (e) { alert(extractErrorMessage(e)); }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Nouvelle anomalie</DialogTitle>
      <DialogContent>
        <Stack spacing={2} mt={1}>
          <TextField size="small" fullWidth label="Titre" value={title} onChange={(e) => setTitle(e.target.value)} />
          <TextField select size="small" fullWidth label="Sévérité" value={severity} onChange={(e) => setSeverity(e.target.value as keyof typeof AnomalySeverity)}>
            {Object.values(AnomalySeverity).map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
          </TextField>
          <TextField multiline rows={3} size="small" fullWidth label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
          {create.isError && <Alert severity="error">{extractErrorMessage(create.error)}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Annuler</Button>
        <Button variant="contained" onClick={submit} disabled={create.isPending || !title.trim()}>Créer</Button>
      </DialogActions>
    </Dialog>
  );
}
