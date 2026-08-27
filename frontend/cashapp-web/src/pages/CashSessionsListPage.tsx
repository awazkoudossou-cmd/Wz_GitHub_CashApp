import { useState } from 'react';
import { Button, Stack } from '@mui/material';
import LockOpenIcon from '@mui/icons-material/LockOpen';
import { useNavigate } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { StatusChip } from '@/components/common/StatusChip';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { OpenSessionDialog } from '@/components/dialogs/OpenSessionDialog';
import { useCashSessions, useOpenCashSession } from '@/modules/cash-sessions/hooks';
import { useAuthStore } from '@/app/store/authStore';
import { formatDate } from '@/utils/format';
import { extractErrorMessage } from '@/api/client';
import type { CashSessionListItem } from '@/types';

export function CashSessionsListPage() {
  const navigate = useNavigate();
  const selectedRegister = useAuthStore((s) => s.selectedCashRegisterId);
  const { data, isLoading } = useCashSessions(selectedRegister ?? undefined);
  const openMut = useOpenCashSession();
  const [openDialog, setOpenDialog] = useState(false);

  const columns: Column<CashSessionListItem>[] = [
    { key: 'id', header: 'ID', render: (r) => `#${r.id}` },
    { key: 'caisse', header: 'Caisse', render: (r) => r.cashRegisterCode },
    { key: 'opener', header: 'Ouverte par', render: (r) => r.openedByName },
    { key: 'openedAt', header: 'Ouverte le', render: (r) => formatDate(r.openedAt, true) },
    { key: 'opening', header: 'Solde ouv.', align: 'right', render: (r) => <CurrencyDisplay value={r.openingBalance} /> },
    { key: 'closedAt', header: 'Fermée le', render: (r) => formatDate(r.closedAt, true) },
    { key: 'theo', header: 'Théorique', align: 'right', render: (r) => <CurrencyDisplay value={r.theoreticalBalance ?? undefined} /> },
    { key: 'variance', header: 'Écart', align: 'right', render: (r) => <CurrencyDisplay value={r.varianceAmount ?? undefined} /> },
    { key: 'status', header: 'Statut', render: (r) => <StatusChip status={r.status} variant="session" /> }
  ];

  return (
    <PageContainer>
      <PageHeader
        title="Sessions de caisse"
        actions={
          <Stack direction="row" spacing={1}>
            <Button variant="contained" startIcon={<LockOpenIcon />} onClick={() => setOpenDialog(true)}>
              Ouvrir une session
            </Button>
          </Stack>
        }
      />
      <AppTable
        columns={columns}
        rows={data}
        rowKey={(r) => r.id}
        isLoading={isLoading}
        onRowClick={(r) => navigate(`/cash-sessions/${r.id}`)}
      />
      <OpenSessionDialog
        open={openDialog}
        defaultCashRegisterId={selectedRegister ?? undefined}
        onClose={() => setOpenDialog(false)}
        onSubmit={async (payload) => {
          try {
            const session = await openMut.mutateAsync(payload);
            setOpenDialog(false);
            navigate(`/cash-sessions/${session.id}`);
          } catch (e) {
            alert(extractErrorMessage(e));
          }
        }}
      />
    </PageContainer>
  );
}
