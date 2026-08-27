import { Button, Stack } from '@mui/material';
import BackupIcon from '@mui/icons-material/Backup';
import RestoreIcon from '@mui/icons-material/Restore';
import { useState } from 'react';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { ConfirmDialog } from '@/components/dialogs/ConfirmDialog';
import { useBackups, useCreateBackup, useRestoreBackup } from '@/modules/backups/hooks';
import { formatDate } from '@/utils/format';
import type { BackupListItem } from '@/types';

export function BackupsPage() {
  const { data, isLoading } = useBackups();
  const create = useCreateBackup();
  const restore = useRestoreBackup();
  const [target, setTarget] = useState<BackupListItem | null>(null);

  const columns: Column<BackupListItem>[] = [
    { key: 'name', header: 'Fichier', render: (r) => r.fileName },
    { key: 'createdAt', header: 'Créée le', render: (r) => formatDate(r.createdAt, true) },
    { key: 'createdBy', header: 'Par', render: (r) => r.createdByName ?? '—' },
    { key: 'size', header: 'Taille', align: 'right', render: (r) => (r.sizeBytes ? `${Math.round(r.sizeBytes / 1024)} Ko` : '—') },
    {
      key: 'action', header: '', align: 'right',
      render: (r) => <Button size="small" startIcon={<RestoreIcon />} onClick={() => setTarget(r)}>Restaurer</Button>
    }
  ];

  return (
    <PageContainer>
      <PageHeader
        title="Sauvegardes"
        actions={<Button variant="contained" startIcon={<BackupIcon />} onClick={() => create.mutate()} disabled={create.isPending}>Créer une sauvegarde</Button>}
      />
      <Stack spacing={2}>
        <AppTable columns={columns} rows={data} rowKey={(r) => r.id} isLoading={isLoading} />
      </Stack>
      <ConfirmDialog
        open={!!target}
        title="Restaurer cette sauvegarde ?"
        description={target ? `Le fichier actuel sera remplacé par "${target.fileName}". Une copie de sécurité sera conservée.` : ''}
        destructive
        confirmLabel="Restaurer"
        onClose={() => setTarget(null)}
        onConfirm={async () => {
          if (target) await restore.mutateAsync(target.fileName);
          setTarget(null);
        }}
      />
    </PageContainer>
  );
}
