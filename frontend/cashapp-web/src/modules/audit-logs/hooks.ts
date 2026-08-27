import { useMutation, useQuery } from '@tanstack/react-query';
import { auditLogsApi } from '@/api/auditLogsApi';
import type { AuditLogFilter } from '@/types';

export function useAuditLogs(filter: AuditLogFilter = {}) {
  return useQuery({ queryKey: ['audit-logs', filter], queryFn: () => auditLogsApi.list(filter) });
}

export function useAuditLog(id: number | undefined) {
  return useQuery({ queryKey: ['audit-log', id], queryFn: () => auditLogsApi.get(id!), enabled: !!id });
}

export function useExportAuditLogs() {
  return useMutation({ mutationFn: (filter: AuditLogFilter) => auditLogsApi.export(filter) });
}
