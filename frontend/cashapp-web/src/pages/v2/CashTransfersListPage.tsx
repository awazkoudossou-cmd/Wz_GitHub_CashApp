import { useState } from 'react';
import { Button, Card, CardContent, Grid, MenuItem, TextField } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { StatusBadge } from '@/components/common/StatusBadge';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { useCashTransfers } from '@/modules/cash-transfers-v2/hooks';
import { CashTransferStatus } from '@/types/v2Enums';
import { formatDate } from '@/utils/format';
import type { CashTransferListItem } from '@/types';

export function CashTransfersListPage() {
  const navigate = useNavigate();
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const { data, isLoading } = useCashTransfers({
    status: (status || undefined) as any,
    page, pageSize
  });

  const columns: Column<CashTransferListItem>[] = [
    { key: 'ref', header: 'Référence', render: (r) => r.transferRef },
    { key: 'date', header: 'Date', render: (r) => formatDate(r.transferDate) },
    { key: 'src', header: 'Source', render: (r) => r.sourceCashRegisterCode },
    { key: 'dst', header: 'Destination', render: (r) => r.destinationCashRegisterCode },
    { key: 'amount', header: 'Montant', align: 'right', render: (r) => <CurrencyDisplay value={r.amount} currency={r.currencyCode} /> },
    { key: 'req', header: 'Demandé par', render: (r) => r.requestedByName },
    { key: 'status', header: 'Statut', render: (r) => <StatusBadge value={r.status} /> }
  ];

  return (
    <PageContainer>
      <PageHeader
        title="Transferts inter-caisses"
        actions={<Button component={RouterLink} to="/cash-transfers/new" variant="contained" startIcon={<AddIcon />}>Nouveau transfert</Button>}
      />
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={3}>
              <TextField select fullWidth size="small" label="Statut" value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}>
                <MenuItem value="">Tous</MenuItem>
                {Object.values(CashTransferStatus).map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
              </TextField>
            </Grid>
          </Grid>
        </CardContent>
      </Card>
      <AppTable
        columns={columns}
        rows={data?.items}
        rowKey={(r) => r.id}
        isLoading={isLoading}
        onRowClick={(r) => navigate(`/cash-transfers/${r.id}`)}
        maxHeight="calc(100vh - 360px)"
        pagination={{
          page, pageSize, total: data?.totalCount ?? 0,
          onPageChange: setPage, onPageSizeChange: (s) => { setPageSize(s); setPage(1); }
        }}
      />
    </PageContainer>
  );
}
