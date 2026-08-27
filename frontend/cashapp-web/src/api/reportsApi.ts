import type {
  AnomalyReportFilter,
  AnomalyReportResult,
  ApprovalReportFilter,
  ApprovalReportResult,
  CashReportFilter,
  CashReportResult,
  CategoryReportFilter,
  CategoryReportResult,
  DepositReportFilter,
  DepositReportResult,
  TransferReportFilter,
  TransferReportResult,
  VarianceReportFilter,
  VarianceReportResult
} from '@/types';
import { apiClient } from './client';

export const reportsApi = {
  cash: (p: CashReportFilter) =>
    apiClient.get<CashReportResult>('/api/reports/cash', { params: p }).then((r) => r.data),
  categories: (p: CategoryReportFilter) =>
    apiClient.get<CategoryReportResult>('/api/reports/categories', { params: p }).then((r) => r.data),
  variances: (p: VarianceReportFilter) =>
    apiClient.get<VarianceReportResult>('/api/reports/variances', { params: p }).then((r) => r.data),
  transfers: (p: TransferReportFilter) =>
    apiClient.get<TransferReportResult>('/api/reports/transfers', { params: p }).then((r) => r.data),
  deposits: (p: DepositReportFilter) =>
    apiClient.get<DepositReportResult>('/api/reports/deposits', { params: p }).then((r) => r.data),
  anomalies: (p: AnomalyReportFilter) =>
    apiClient.get<AnomalyReportResult>('/api/reports/anomalies', { params: p }).then((r) => r.data),
  approvals: (p: ApprovalReportFilter) =>
    apiClient.get<ApprovalReportResult>('/api/reports/approvals', { params: p }).then((r) => r.data)
};
