import { useQuery } from '@tanstack/react-query';
import { reportsApi } from '@/api/reportsApi';
import type {
  AnomalyReportFilter,
  ApprovalReportFilter,
  CashReportFilter,
  CategoryReportFilter,
  DepositReportFilter,
  TransferReportFilter,
  VarianceReportFilter
} from '@/types';

export function useCashReport(p: CashReportFilter, enabled: boolean) {
  return useQuery({ queryKey: ['report-cash', p], queryFn: () => reportsApi.cash(p), enabled });
}
export function useCategoryReport(p: CategoryReportFilter, enabled: boolean) {
  return useQuery({ queryKey: ['report-cat', p], queryFn: () => reportsApi.categories(p), enabled });
}
export function useVarianceReport(p: VarianceReportFilter, enabled: boolean) {
  return useQuery({ queryKey: ['report-var', p], queryFn: () => reportsApi.variances(p), enabled });
}
export function useTransferReport(p: TransferReportFilter, enabled: boolean) {
  return useQuery({ queryKey: ['report-tr', p], queryFn: () => reportsApi.transfers(p), enabled });
}
export function useDepositReport(p: DepositReportFilter, enabled: boolean) {
  return useQuery({ queryKey: ['report-dep', p], queryFn: () => reportsApi.deposits(p), enabled });
}
export function useAnomalyReport(p: AnomalyReportFilter, enabled: boolean) {
  return useQuery({ queryKey: ['report-an', p], queryFn: () => reportsApi.anomalies(p), enabled });
}
export function useApprovalReport(p: ApprovalReportFilter, enabled: boolean) {
  return useQuery({ queryKey: ['report-ap', p], queryFn: () => reportsApi.approvals(p), enabled });
}
