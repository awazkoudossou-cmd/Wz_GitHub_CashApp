import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { anomaliesApi } from '@/api/anomaliesApi';
import type { AnomalyFilter, CreateAnomalyPayload } from '@/types';

export function useAnomalies(filter: AnomalyFilter = {}) {
  return useQuery({ queryKey: ['anomalies', filter], queryFn: () => anomaliesApi.list(filter) });
}

export function useAnomaly(id: number | undefined) {
  return useQuery({ queryKey: ['anomaly', id], queryFn: () => anomaliesApi.get(id!), enabled: !!id });
}

export function useCreateAnomaly() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CreateAnomalyPayload) => anomaliesApi.create(p),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['anomalies'] })
  });
}

export function useAssignAnomaly() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, userId }: { id: number; userId: number }) => anomaliesApi.assign(id, userId),
    onSuccess: (_, { id }) => {
      qc.invalidateQueries({ queryKey: ['anomalies'] });
      qc.invalidateQueries({ queryKey: ['anomaly', id] });
    }
  });
}

export function useResolveAnomaly() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, comment }: { id: number; comment: string }) => anomaliesApi.resolve(id, comment),
    onSuccess: (_, { id }) => {
      qc.invalidateQueries({ queryKey: ['anomalies'] });
      qc.invalidateQueries({ queryKey: ['anomaly', id] });
    }
  });
}

export function useAddAnomalyComment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: number; body: string }) => anomaliesApi.addComment(id, body),
    onSuccess: (_, { id }) => qc.invalidateQueries({ queryKey: ['anomaly', id] })
  });
}
