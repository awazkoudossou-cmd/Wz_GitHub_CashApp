import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { categoriesApi } from '@/api/categoriesApi';
import type { CreateCategoryPayload, UpdateCategoryPayload } from '@/types';

const KEY = ['categories'] as const;

export function useCategories() {
  return useQuery({ queryKey: KEY, queryFn: categoriesApi.list });
}

export function useCreateCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CreateCategoryPayload) => categoriesApi.create(p),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY })
  });
}

export function useUpdateCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: UpdateCategoryPayload }) =>
      categoriesApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY })
  });
}

export function useUpdateCategoryStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) =>
      categoriesApi.updateStatus(id, isActive),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY })
  });
}
