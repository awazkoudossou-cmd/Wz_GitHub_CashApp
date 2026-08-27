import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { importsApi } from '@/api/importsApi';
import type { ImportBatchType } from '@/types';

export function useImports(page = 1, pageSize = 50) {
  return useQuery({ queryKey: ['imports', page, pageSize], queryFn: () => importsApi.list(page, pageSize) });
}

export function useImport(id: number | undefined) {
  return useQuery({ queryKey: ['import', id], queryFn: () => importsApi.get(id!), enabled: !!id });
}

export function useUploadImport() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ batchType, file, cashRegisterId }: { batchType: ImportBatchType; file: File; cashRegisterId?: number }) =>
      importsApi.upload(batchType, file, cashRegisterId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['imports'] })
  });
}

export function usePreviewImport() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => importsApi.preview(id),
    onSuccess: (_, id) => {
      qc.invalidateQueries({ queryKey: ['imports'] });
      qc.invalidateQueries({ queryKey: ['import', id] });
    }
  });
}

export function useConfirmImport() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, allowPartialSuccess }: { id: number; allowPartialSuccess: boolean }) =>
      importsApi.confirm(id, allowPartialSuccess),
    onSuccess: (_, { id }) => {
      qc.invalidateQueries({ queryKey: ['imports'] });
      qc.invalidateQueries({ queryKey: ['import', id] });
    }
  });
}
