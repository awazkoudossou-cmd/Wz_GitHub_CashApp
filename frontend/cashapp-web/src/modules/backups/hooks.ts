import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { backupsApi } from '@/api/backupsApi';

export function useBackups() {
  return useQuery({ queryKey: ['backups'], queryFn: backupsApi.list });
}

export function useCreateBackup() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => backupsApi.create(),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['backups'] })
  });
}

export function useRestoreBackup() {
  return useMutation({
    mutationFn: (fileName: string) => backupsApi.restore(fileName)
  });
}
