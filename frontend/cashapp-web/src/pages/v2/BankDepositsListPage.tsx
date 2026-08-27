import { useState } from 'react';
import { Button, Card, CardContent, Grid, MenuItem, TextField } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { StatusBadge } from '@/components/common/StatusBadge';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { useBankDeposits } from '@/modules/bank-deposits/hooks';
import { BankDepositStatus } from '@/types/v2Enums';
import { formatDate } from '@/utils/format';
import type { BankDepositListItem } from '@/types';

export function BankDepositsListPage() {
  const navigate = useNavigate();
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const { data, isLoading } = useBankDeposits({
    status: (status || undefined) as any, page, pageSize
  });

  const columns: Column<BankDepositListItem>[] = [
    { key: 'ref', header: 'Référence', render: (r) => r.depositRef },
    { key: 'date', header: 'Date', render: (r) => formatDate(r.depositDate) },
    { key: 'caisse', header: 'Caisse', render: (r) => r.cashRegisterCode },
    { key: 'bank', header: 'Banque', render: (r) => r.bankName },
    { key: 'amount', header: 'Montant', align: 'right', render: (r) => <CurrencyDisplay value={r.amount} currency={r.currencyCode} /> },
    { key: 'req', header: 'Demandé par', render: (r) => r.requestedByName },
    { key: 'status', header: 'Statut', render: (r) => <StatusBadge value={r.status} /> }
  ];

  return (
    <PageContainer>
      <PageHeader
        title="Dépôts banque"
        actions={<Button component={RouterLink} to="/bank-deposits/new" variant="contained" startIcon={<AddIcon />}>Nouveau dépôt</Button>}
      />
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={3}>
              <TextField select fullWidth size="small" label="Statut" value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}>
                <MenuItem value="">Tous</MenuItem>
                {Object.values(BankDepositStatus).map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
              </TextField>
            </Grid>
          </Grid>
        </CardContent>
      </Card>
      <AppTable
        columns={columns} rows={data?.items} rowKey={(r) => r.id} isLoading={isLoading}
        onRowClick={(r) => navigate(`/bank-deposits/${r.id}`)}
        maxHeight="calc(100vh - 360px)"
        pagination={{ page, pageSize, total: data?.totalCount ?? 0, onPageChange: setPage, onPageSizeChange: (s) => { setPageSize(s); setPage(1); } }}
      />
    </PageContainer>
  );
}
