import type {
  ApprovalRequestDetail,
  ApprovalRequestFilter,
  ApprovalRequestListItem,
  ApprovalRule,
  CreateApprovalRulePayload,
  PagedResponse,
  UpdateApprovalRulePayload
} from '@/types';
import { apiClient } from './client';

export const approvalRulesApi = {
  list: () => apiClient.get<ApprovalRule[]>('/api/approval-rules').then((r) => r.data),
  create: (p: CreateApprovalRulePayload) =>
    apiClient.post<ApprovalRule>('/api/approval-rules', p).then((r) => r.data),
  update: (id: number, p: UpdateApprovalRulePayload) =>
    apiClient.put<ApprovalRule>(`/api/approval-rules/${id}`, p).then((r) => r.data),
  updateStatus: (id: number, isActive: boolean) =>
    apiClient.patch(`/api/approval-rules/${id}/status`, { isActive }).then((r) => r.data)
};

export const approvalRequestsApi = {
  list: (filter: ApprovalRequestFilter = {}) =>
    apiClient
      .get<PagedResponse<ApprovalRequestListItem>>('/api/approval-requests', { params: filter })
      .then((r) => r.data),
  get: (id: number) =>
    apiClient.get<ApprovalRequestDetail>(`/api/approval-requests/${id}`).then((r) => r.data),
  approve: (id: number, comment?: string) =>
    apiClient
      .post<ApprovalRequestDetail>(`/api/approval-requests/${id}/approve`, { comment })
      .then((r) => r.data),
  reject: (id: number, comment: string) =>
    apiClient
      .post<ApprovalRequestDetail>(`/api/approval-requests/${id}/reject`, { comment })
      .then((r) => r.data)
};
