import type {
  CategoryDetail,
  CategoryListItem,
  CreateCategoryPayload,
  UpdateCategoryPayload
} from '@/types';
import { apiClient } from './client';

export const categoriesApi = {
  list: () => apiClient.get<CategoryListItem[]>('/api/categories').then((r) => r.data),
  create: (p: CreateCategoryPayload) =>
    apiClient.post<CategoryDetail>('/api/categories', p).then((r) => r.data),
  update: (id: number, p: UpdateCategoryPayload) =>
    apiClient.put<CategoryDetail>(`/api/categories/${id}`, p).then((r) => r.data),
  updateStatus: (id: number, isActive: boolean) =>
    apiClient.patch(`/api/categories/${id}/status`, { isActive }).then((r) => r.data)
};
