import { useQuery } from '@tanstack/react-query';
import { categoryGroupsApi } from '@/api/categoryGroupsApi';

export function useCategoryGroups() {
  return useQuery({
    queryKey: ['category-groups'],
    queryFn: categoryGroupsApi.list,
    staleTime: 60_000
  });
}
