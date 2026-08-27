import { useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  TextField,
  Typography
} from '@mui/material';
import DownloadIcon from '@mui/icons-material/Download';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { StatusBadge } from '@/components/common/StatusBadge';
import { useAuditLogs, useExportAuditLogs } from '@/modules/audit-logs/hooks';
import { useNotificationStore } from '@/app/store/notificationStore';
import { extractErrorMessage } from '@/api/client';
import { AuditAction } from '@/types/v2Enums';
import { formatDate } from '@/utils/format';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import type { AuditLogListItem } from '@/types';

export function AuditLogsPage() {
  const [actionType, setActionType] = useState('');
  const [entityType, setEntityType] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(100);
  const [selected, setSelected] = useState<AuditLogListItem | null>(null);
  const exportLogs = useExportAuditLogs();
  const notifyError = useNotificationStore((s) => s.notifyError);

  const filter = {
    actionType: (actionType || undefined) as any,
    entityType: entityType || undefined,
    from: from || undefined,
    to: to || undefined,
    page, pageSize
  };
  const { data, isLoading } = useAuditLogs(filter);

  const handleExport = async () => {
    try {
      await exportLogs.mutateAsync(filter);
    } catch (e) {
      notifyError(extractErrorMessage(e));
    }
  };

  const columns: Column<AuditLogListItem>[] = [
    { key: 'date', header: 'Date', render: (r) => formatDate(r.performedAt, true) },
    { key: 'action', header: 'Action', render: (r) => <StatusBadge value={r.actionType} /> },
    { key: 'entity', header: 'Entité', render: (r) => `${r.entityType}${r.entityId ? ` #${r.entityId}` : ''}` },
    { key: 'user', header: 'Utilisateur', render: (r) => r.performedByName ?? '—' },
    {
      key: 'amount', header: 'Montant', align: 'right',
      render: (r) => r.amount != null
        ? <CurrencyDisplay value={r.amount} currency={r.currencyCode ?? 'XOF'} />
        : '—'
    },
    { key: 'desc', header: 'Description', render: (r) => r.description ?? '—' }
  ];

  return (
    <PageContainer>
      <Box sx={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 112px)' }}>
      <PageHeader
        title="Journal d'audit"
        subtitle="Traçabilité des actions sensibles"
        actions={
          <Button variant="outlined" startIcon={<DownloadIcon />} onClick={handleExport} disabled={exportLogs.isPending}>
            Exporter (filtres)
          </Button>
        }
      />
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={3}>
              <TextField select fullWidth size="small" label="Action" value={actionType} onChange={(e) => { setActionType(e.target.value); setPage(1); }}>
                <MenuItem value="">Toutes</MenuItem>
                {Object.values(AuditAction).map((a) => <MenuItem key={a} value={a}>{a}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={3}>
              <TextField fullWidth size="small" label="Entité (ex. CashOperation)" value={entityType} onChange={(e) => setEntityType(e.target.value)} />
            </Grid>
            <Grid item xs={12} sm={3}>
              <TextField fullWidth size="small" type="date" label="Du" InputLabelProps={{ shrink: true }} value={from} onChange={(e) => setFrom(e.target.value)} />
            </Grid>
            <Grid item xs={12} sm={3}>
              <TextField fullWidth size="small" type="date" label="Au" InputLabelProps={{ shrink: true }} value={to} onChange={(e) => setTo(e.target.value)} />
            </Grid>
          </Grid>
        </CardContent>
      </Card>
      <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
        <AppTable
          columns={columns} rows={data?.items} rowKey={(r) => r.id} isLoading={isLoading}
          onRowClick={(r) => setSelected(r)}
          maxHeight="100%"
          pagination={{ page, pageSize, total: data?.totalCount ?? 0, onPageChange: setPage, onPageSizeChange: (s) => { setPageSize(s); setPage(1); } }}
        />
      </Box>
      </Box>

      <Dialog open={!!selected} onClose={() => setSelected(null)} maxWidth="md" fullWidth>
        <DialogTitle>Audit #{selected?.id}</DialogTitle>
        <DialogContent>
          {selected && <AuditDetails id={selected.id} />}
        </DialogContent>
      </Dialog>
    </PageContainer>
  );
}

import { useAuditLog } from '@/modules/audit-logs/hooks';

function AuditDetails({ id }: { id: number }) {
  const { data, isLoading } = useAuditLog(id);
  if (isLoading || !data) return <Typography>Chargement…</Typography>;
  return (
    <Grid container spacing={2} mt={0.5}>
      <Grid item xs={12}><StatusBadge value={data.actionType} /></Grid>
      <Grid item xs={12}><Typography variant="caption">Entité</Typography><Typography>{data.entityType}{data.entityId ? ` #${data.entityId}` : ''}</Typography></Grid>
      <Grid item xs={12}><Typography variant="caption">Quand</Typography><Typography>{formatDate(data.performedAt, true)}</Typography></Grid>
      <Grid item xs={12}><Typography variant="caption">Par</Typography><Typography>{data.performedByName ?? '—'} {data.ipAddress ? ` (IP ${data.ipAddress})` : ''}</Typography></Grid>
      {data.description && <Grid item xs={12}><Typography variant="caption">Description</Typography><Typography>{data.description}</Typography></Grid>}
      {data.metadataJson && <Grid item xs={12}><Typography variant="caption">Métadonnées</Typography><pre style={{ background: 'rgba(0,0,0,0.04)', padding: 8, borderRadius: 4, overflow: 'auto' }}>{data.metadataJson}</pre></Grid>}
      {data.oldValuesJson && <Grid item xs={12} sm={6}><Typography variant="caption">Anciennes valeurs</Typography><pre style={{ background: 'rgba(0,0,0,0.04)', padding: 8, borderRadius: 4, overflow: 'auto' }}>{data.oldValuesJson}</pre></Grid>}
      {data.newValuesJson && <Grid item xs={12} sm={6}><Typography variant="caption">Nouvelles valeurs</Typography><pre style={{ background: 'rgba(0,0,0,0.04)', padding: 8, borderRadius: 4, overflow: 'auto' }}>{data.newValuesJson}</pre></Grid>}
    </Grid>
  );
}
