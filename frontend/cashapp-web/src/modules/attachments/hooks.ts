import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { attachmentsApi } from '@/api/attachmentsApi';

export function useAttachments(entityType: string, entityId: number | undefined) {
  return useQuery({
    queryKey: ['attachments', entityType, entityId],
    queryFn: () => attachmentsApi.list(entityType, entityId!),
    enabled: !!entityId
  });
}

export function useUploadAttachment(entityType: string, entityId: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ file, description }: { file: File; description?: string }) =>
      attachmentsApi.upload(entityType, entityId, file, description),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['attachments', entityType, entityId] })
  });
}

export function useDeleteAttachment(entityType: string, entityId: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => attachmentsApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['attachments', entityType, entityId] })
  });
}
