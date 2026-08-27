import { Button, Switch } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { StatusChip } from '@/components/common/StatusChip';
import { useCashRegisters, useUpdateCashRegisterStatus } from '@/modules/cash-registers/hooks';
import type { CashRegisterListItem } from '@/types';

export function CashRegistersListPage() {
  const navigate = useNavigate();
  const { data, isLoading } = useCashRegisters();
  const updateStatus = useUpdateCashRegisterStatus();

  const columns: Column<CashRegisterListItem>[] = [
    { key: 'code', header: 'Code', render: (r) => r.code },
    { key: 'name', header: 'Nom', render: (r) => r.name },
    { key: 'currency', header: 'Devise', render: (r) => r.currencyCode },
    { key: 'dir', header: 'Dir. par défaut', render: (r) => r.defaultDirection },
    { key: 'pay', header: 'Paiement par défaut', render: (r) => r.defaultPaymentMethod },
    { key: 'status', header: 'Statut', render: (r) => <StatusChip status={String(r.isActive)} variant="active" /> },
    {
      key: 'toggle', header: '', align: 'right',
      render: (r) => (
        <Switch
          size="small" checked={r.isActive}
          onClick={(e) => e.stopPropagation()}
          onChange={(_, v) => updateStatus.mutate({ id: r.id, isActive: v })}
        />
      )
    }
  ];

  return (
    <PageContainer>
      <PageHeader
        title="Caisses"
        actions={<Button component={RouterLink} to="/cash-registers/new" variant="contained" startIcon={<AddIcon />}>Nouvelle caisse</Button>}
      />
      <AppTable columns={columns} rows={data} rowKey={(r) => r.id} isLoading={isLoading}
        onRowClick={(r) => navigate(`/cash-registers/${r.id}`)} />
    </PageContainer>
  );
}
