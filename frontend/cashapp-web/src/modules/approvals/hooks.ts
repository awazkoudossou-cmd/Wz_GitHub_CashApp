import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { approvalRequestsApi, approvalRulesApi } from '@/api/approvalsApi';
import type {
  ApprovalRequestFilter,
  CreateApprovalRulePayload,
  UpdateApprovalRulePayload
} from '@/types';

// === Rules ===
const RULES_KEY = ['approval-rules'] as const;

export function useApprovalRules() {
  return useQuery({ queryKey: RULES_KEY, queryFn: approvalRulesApi.list });
}

export function useCreateApprovalRule() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CreateApprovalRulePayload) => approvalRulesApi.create(p),
    onSuccess: () => qc.invalidateQueries({ queryKey: RULES_KEY })
  });
}

export function useUpdateApprovalRule() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: UpdateApprovalRulePayload }) =>
      approvalRulesApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: RULES_KEY })
  });
}

export function useUpdateApprovalRuleStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) =>
      approvalRulesApi.updateStatus(id, isActive),
    onSuccess: () => qc.invalidateQueries({ queryKey: RULES_KEY })
  });
}

// === Requests ===
export function useApprovalRequests(filter: ApprovalRequestFilter = {}) {
  return useQuery({
    queryKey: ['approval-requests', filter],
    queryFn: () => approvalRequestsApi.list(filter)
  });
}

export function useApprovalRequest(id: number | undefined) {
  return useQuery({
    queryKey: ['approval-request', id],
    queryFn: () => approvalRequestsApi.get(id!),
    enabled: !!id
  });
}

export function useApproveRequest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, comment }: { id: number; comment?: string }) =>
      approvalRequestsApi.approve(id, comment),
    onSuccess: async (_, { id }) => {
      // L'approbation déclenche côté backend l'auto-finalisation
      // du transfert/dépôt → on rafraîchit tout ce qui peut bouger.
      await Promise.all([
        qc.refetchQueries({ queryKey: ['approval-requests'] }),
        qc.refetchQueries({ queryKey: ['approval-request', id] }),
        qc.refetchQueries({ queryKey: ['cash-transfers'] }),
        qc.refetchQueries({ queryKey: ['bank-deposits'] }),
        qc.refetchQueries({ queryKey: ['cash-operations'] }),
        qc.refetchQueries({ queryKey: ['cash-sessions'] }),
        qc.refetchQueries({ queryKey: ['dashboard'] }),
        qc.refetchQueries({ queryKey: ['variances'] })
      ]);
      import('@/app/store/notificationStore').then(({ useNotificationStore }) => {
        useNotificationStore.getState().notifySuccess('Demande approuvée.');
      });
    }
  });
}

export function useRejectRequest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, comment }: { id: number; comment: string }) =>
      approvalRequestsApi.reject(id, comment),
    onSuccess: async (_, { id }) => {
      await Promise.all([
        qc.refetchQueries({ queryKey: ['approval-requests'] }),
        qc.refetchQueries({ queryKey: ['approval-request', id] }),
        qc.refetchQueries({ queryKey: ['cash-transfers'] }),
        qc.refetchQueries({ queryKey: ['bank-deposits'] }),
        qc.refetchQueries({ queryKey: ['variances'] })
      ]);
      import('@/app/store/notificationStore').then(({ useNotificationStore }) => {
        useNotificationStore.getState().notifyInfo('Demande rejetée.');
      });
    }
  });
}
