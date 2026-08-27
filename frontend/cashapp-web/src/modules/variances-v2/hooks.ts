import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { variancesApi } from '@/api/variancesApi';
import type { VarianceFilter } from '@/types';

export function useVariances(filter: VarianceFilter = {}) {
  return useQuery({ queryKey: ['variances', filter], queryFn: () => variancesApi.list(filter) });
}

export function useVariance(id: number | undefined) {
  return useQuery({ queryKey: ['variance', id], queryFn: () => variancesApi.get(id!), enabled: !!id });
}

export function useJustifyVariance() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, comment }: { id: number; comment: string }) => variancesApi.justify(id, comment),
    onSuccess: (_, { id }) => {
      qc.invalidateQueries({ queryKey: ['variances'] });
      qc.invalidateQueries({ queryKey: ['variance', id] });
    }
  });
}
