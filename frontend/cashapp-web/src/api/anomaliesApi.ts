import type {
  AnomalyDetail,
  AnomalyFilter,
  AnomalyListItem,
  CreateAnomalyPayload,
  PagedResponse
} from '@/types';
import { apiClient } from './client';

export const anomaliesApi = {
  list: (filter: AnomalyFilter = {}) =>
    apiClient
      .get<PagedResponse<AnomalyListItem>>('/api/anomalies', { params: filter })
      .then((r) => r.data),
  get: (id: number) => apiClient.get<AnomalyDetail>(`/api/anomalies/${id}`).then((r) => r.data),
  create: (p: CreateAnomalyPayload) =>
    apiClient.post<AnomalyDetail>('/api/anomalies', p).then((r) => r.data),
  assign: (id: number, assignToUserId: number) =>
    apiClient
      .post<AnomalyDetail>(`/api/anomalies/${id}/assign`, { assignToUserId })
      .then((r) => r.data),
  resolve: (id: number, resolutionComment: string) =>
    apiClient
      .post<AnomalyDetail>(`/api/anomalies/${id}/resolve`, { resolutionComment })
      .then((r) => r.data),
  addComment: (id: number, body: string) =>
    apiClient
      .post<AnomalyDetail>(`/api/anomalies/${id}/comments`, { body })
      .then((r) => r.data)
};
