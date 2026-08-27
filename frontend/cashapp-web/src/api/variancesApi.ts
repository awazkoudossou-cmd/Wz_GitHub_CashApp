import type {
  PagedResponse,
  VarianceDetail,
  VarianceFilter,
  VarianceListItem
} from '@/types';
import { apiClient } from './client';

export const variancesApi = {
  list: (filter: VarianceFilter = {}) =>
    apiClient
      .get<PagedResponse<VarianceListItem>>('/api/variances', { params: filter })
      .then((r) => r.data),
  get: (id: number) => apiClient.get<VarianceDetail>(`/api/variances/${id}`).then((r) => r.data),
  justify: (id: number, comment: string) =>
    apiClient.post<VarianceDetail>(`/api/variances/${id}/justify`, { comment }).then((r) => r.data)
};
