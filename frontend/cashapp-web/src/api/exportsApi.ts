import type {
  ExportCashStateRequest,
  ExportOperationsRequest,
  ExportSessionsRequest
} from '@/types';
import { apiClient } from './client';

async function downloadBlob(url: string, body: unknown, fallbackName: string) {
  const response = await apiClient.post(url, body, { responseType: 'blob' });
  const cd = response.headers['content-disposition'] as string | undefined;
  const match = cd?.match(/filename="?([^";]+)"?/i);
  const fileName = match?.[1] ?? fallbackName;
  const blob = response.data as Blob;
  const objectUrl = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = objectUrl;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(objectUrl);
}

export const exportsApi = {
  operations: (p: ExportOperationsRequest) =>
    downloadBlob('/api/exports/operations', p, `operations.${p.format}`),
  sessions: (p: ExportSessionsRequest) =>
    downloadBlob('/api/exports/sessions', p, `sessions.${p.format}`),
  cashState: (p: ExportCashStateRequest) =>
    downloadBlob('/api/exports/cash-state', p, `cash-state-${p.cashSessionId}.pdf`)
};

export async function downloadOperationReceipt(operationId: number) {
  const r = await apiClient.get<Blob>(`/api/exports/operations/${operationId}/receipt`, { responseType: 'blob' });
  const blob = r.data;
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `recu_op_${operationId}.pdf`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

export async function downloadApprovalRequestPdf(approvalRequestId: number) {
  const r = await apiClient.get<Blob>(`/api/exports/approval-requests/${approvalRequestId}/pdf`, { responseType: 'blob' });
  const url = URL.createObjectURL(r.data);
  const a = document.createElement('a');
  a.href = url;
  a.download = `approval_request_${approvalRequestId}.pdf`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
