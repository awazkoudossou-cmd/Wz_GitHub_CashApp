import type { CategoryGroup } from '@/types';
import { apiClient } from './client';

export const categoryGroupsApi = {
  list: () => apiClient.get<CategoryGroup[]>('/api/category-groups').then((r) => r.data)
};
