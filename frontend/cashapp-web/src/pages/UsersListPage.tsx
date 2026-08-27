import { Button, Switch } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { StatusChip } from '@/components/common/StatusChip';
import { useUpdateUserStatus, useUsers } from '@/modules/users/hooks';
import type { UserListItem } from '@/types';

export function UsersListPage() {
  const navigate = useNavigate();
  const { data, isLoading } = useUsers();
  const updateStatus = useUpdateUserStatus();

  const columns: Column<UserListItem>[] = [
    { key: 'username', header: 'Username', render: (r) => r.username },
    { key: 'fullName', header: 'Nom', render: (r) => r.fullName },
    { key: 'roleCode', header: 'Rôle', render: (r) => r.roleCode },
    { key: 'status', header: 'Statut', render: (r) => <StatusChip status={String(r.isActive)} variant="active" /> },
    {
      key: 'toggle', header: '', align: 'right',
      render: (r) => (
        <Switch
          size="small"
          checked={r.isActive}
          onClick={(e) => e.stopPropagation()}
          onChange={(_, v) => updateStatus.mutate({ id: r.id, isActive: v })}
        />
      )
    }
  ];

  return (
    <PageContainer>
      <PageHeader
        title="Utilisateurs"
        actions={
          <Button component={RouterLink} to="/users/new" startIcon={<AddIcon />} variant="contained">
            Nouvel utilisateur
          </Button>
        }
      />
      <AppTable
        columns={columns}
        rows={data}
        rowKey={(r) => r.id}
        isLoading={isLoading}
        onRowClick={(r) => navigate(`/users/${r.id}`)}
      />
    </PageContainer>
  );
}
