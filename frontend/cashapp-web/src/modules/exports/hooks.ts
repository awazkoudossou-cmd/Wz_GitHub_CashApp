import { useMutation } from '@tanstack/react-query';
import { exportsApi } from '@/api/exportsApi';
import type {
  ExportCashStateRequest,
  ExportOperationsRequest,
  ExportSessionsRequest
} from '@/types';

export function useExportOperations() {
  return useMutation({ mutationFn: (p: ExportOperationsRequest) => exportsApi.operations(p) });
}

export function useExportSessions() {
  return useMutation({ mutationFn: (p: ExportSessionsRequest) => exportsApi.sessions(p) });
}

export function useExportCashState() {
  return useMutation({ mutationFn: (p: ExportCashStateRequest) => exportsApi.cashState(p) });
}
