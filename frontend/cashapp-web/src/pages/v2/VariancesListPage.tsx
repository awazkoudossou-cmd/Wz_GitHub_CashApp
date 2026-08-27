import { useState } from 'react';
import { Card, CardContent, Grid, MenuItem, TextField } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { StatusBadge } from '@/components/common/StatusBadge';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { useVariances } from '@/modules/variances-v2/hooks';
import { VarianceStatus } from '@/types/v2Enums';
import { formatDate } from '@/utils/format';
import type { VarianceListItem } from '@/types';

export function VariancesListPage() {
  const navigate = useNavigate();
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const { data, isLoading } = useVariances({ status: (status || undefined) as any, page, pageSize });

  const columns: Column<VarianceListItem>[] = [
    { key: 'id', header: 'ID', render: (r) => `#${r.id}` },
    { key: 'caisse', header: 'Caisse', render: (r) => `${r.cashRegisterCode} — ${r.cashRegisterName}` },
    { key: 'session', header: 'Session', render: (r) => `#${r.cashSessionId}` },
    { key: 'openedAt', header: 'Ouvert', render: (r) => `${formatDate(r.sessionOpenedAt, true)}\n${r.sessionOpenedByName}` },
    { key: 'closedAt', header: 'Fermé', render: (r) => r.sessionClosedAt ? `${formatDate(r.sessionClosedAt, true)}\n${r.sessionClosedByName ?? ''}` : '—' },
    { key: 'theo', header: 'Théorique', align: 'right', render: (r) => <CurrencyDisplay value={r.sessionTheoreticalBalance ?? undefined} currency={r.currencyCode} /> },
    { key: 'phys', header: 'Physique', align: 'right', render: (r) => <CurrencyDisplay value={r.sessionPhysicalBalance ?? undefined} currency={r.currencyCode} /> },
    { key: 'amt', header: 'Écart', align: 'right', render: (r) => <CurrencyDisplay value={r.varianceAmount} currency={r.currencyCode} /> },
    { key: 'status', header: 'Statut', render: (r) => <StatusBadge value={r.status} /> },
    { key: 'approval', header: 'Approbation', render: (r) => r.approvalRequestId ? `#${r.approvalRequestId}` : '—' },
    { key: 'anomaly', header: 'Anomalie', render: (r) => r.anomalyCaseId ? `#${r.anomalyCaseId}` : '—' }
  ];

  return (
    <PageContainer>
      <PageHeader title="Écarts de caisse" subtitle="Dossiers d'écarts générés à la clôture des sessions" />
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={3}>
              <TextField select fullWidth size="small" label="Statut" value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}>
                <MenuItem value="">Tous</MenuItem>
                {Object.values(VarianceStatus).map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
              </TextField>
            </Grid>
          </Grid>
        </CardContent>
      </Card>
      <AppTable
        columns={columns} rows={data?.items} rowKey={(r) => r.id} isLoading={isLoading}
        onRowClick={(r) => navigate(`/variances/${r.id}`)}
        maxHeight="calc(100vh - 360px)"
        pagination={{ page, pageSize, total: data?.totalCount ?? 0, onPageChange: setPage, onPageSizeChange: (s) => { setPageSize(s); setPage(1); } }}
      />
    </PageContainer>
  );
}
