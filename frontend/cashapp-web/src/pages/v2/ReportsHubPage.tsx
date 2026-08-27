import { useState } from 'react';
import { Box, Card, CardContent, Grid, MenuItem, Tab, Tabs, TextField, Typography } from '@mui/material';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { ReportFiltersPanel } from '@/components/common/ReportFiltersPanel';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { StatusBadge } from '@/components/common/StatusBadge';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { StatCard } from '@/components/common/StatCard';
import {
  useAnomalyReport,
  useApprovalReport,
  useCashReport,
  useCategoryReport,
  useDepositReport,
  useTransferReport,
  useVarianceReport
} from '@/modules/reports-v2/hooks';
import type {
  AnomalyReportRow,
  ApprovalReportRow,
  CashReportRow,
  CategoryReportRow,
  DepositReportRow,
  TransferReportRow,
  VarianceReportRow
} from '@/types';
import { formatDate } from '@/utils/format';

const tabs = [
  { value: 'cash', label: 'Caisse' },
  { value: 'categories', label: 'Catégories' },
  { value: 'variances', label: 'Écarts' },
  { value: 'transfers', label: 'Transferts' },
  { value: 'deposits', label: 'Dépôts' },
  { value: 'anomalies', label: 'Anomalies' },
  { value: 'approvals', label: 'Validations' }
] as const;

type Tab = (typeof tabs)[number]['value'];

const today = new Date().toISOString().slice(0, 10);
const monthStart = new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().slice(0, 10);

export function ReportsHubPage() {
  const [tab, setTab] = useState<Tab>('cash');
  const [from, setFrom] = useState(monthStart);
  const [to, setTo] = useState(today);
  const [cashRegisterId, setCashRegisterId] = useState<number | undefined>(undefined);
  const [enabled, setEnabled] = useState(true);

  const apply = () => setEnabled(true);

  return (
    <PageContainer>
      <PageHeader title="Rapports avancés" subtitle="Analyses par caisse, catégorie, écart, transfert, dépôt, anomalie, validation" />

      <Tabs value={tab} onChange={(_, v) => { setTab(v); setEnabled(false); }} sx={{ mb: 2 }}>
        {tabs.map((t) => <Tab key={t.value} value={t.value} label={t.label} />)}
      </Tabs>

      <ReportFiltersPanel
        from={from} to={to} cashRegisterId={cashRegisterId}
        onFromChange={setFrom} onToChange={setTo} onCashRegisterChange={setCashRegisterId}
        showCashRegister={tab !== 'approvals'}
        onApply={apply}
      />

      {tab === 'cash' && <CashReportTab from={from} to={to} cashRegisterId={cashRegisterId} enabled={enabled} />}
      {tab === 'categories' && <CategoryReportTab from={from} to={to} cashRegisterId={cashRegisterId} enabled={enabled} />}
      {tab === 'variances' && <VarianceReportTab from={from} to={to} cashRegisterId={cashRegisterId} enabled={enabled} />}
      {tab === 'transfers' && <TransferReportTab from={from} to={to} cashRegisterId={cashRegisterId} enabled={enabled} />}
      {tab === 'deposits' && <DepositReportTab from={from} to={to} cashRegisterId={cashRegisterId} enabled={enabled} />}
      {tab === 'anomalies' && <AnomalyReportTab from={from} to={to} cashRegisterId={cashRegisterId} enabled={enabled} />}
      {tab === 'approvals' && <ApprovalReportTab from={from} to={to} enabled={enabled} />}
    </PageContainer>
  );
}

interface BaseProps { from: string; to: string; cashRegisterId?: number; enabled: boolean; }

function CashReportTab({ from, to, cashRegisterId, enabled }: BaseProps) {
  const { data, isLoading } = useCashReport({ from, to, cashRegisterId }, enabled);
  const cols: Column<CashReportRow>[] = [
    { key: 'code', header: 'Caisse', render: (r) => r.cashRegisterCode },
    { key: 'in', header: 'Entrées', align: 'right', render: (r) => <CurrencyDisplay value={r.totalIn} /> },
    { key: 'out', header: 'Sorties', align: 'right', render: (r) => <CurrencyDisplay value={r.totalOut} /> },
    { key: 'net', header: 'Net', align: 'right', render: (r) => <CurrencyDisplay value={r.netMovement} /> },
    { key: 'count', header: 'Opérations', align: 'right', render: (r) => r.operationCount }
  ];
  return (
    <>
      {data && (
        <Grid container spacing={2} mb={2}>
          <Grid item xs={6} md={3}><StatCard label="Entrées" value={<CurrencyDisplay value={data.summary.totalIn} />} color="success" /></Grid>
          <Grid item xs={6} md={3}><StatCard label="Sorties" value={<CurrencyDisplay value={data.summary.totalOut} />} color="warning" /></Grid>
          <Grid item xs={6} md={3}><StatCard label="Net" value={<CurrencyDisplay value={data.summary.net} />} /></Grid>
          <Grid item xs={6} md={3}><StatCard label="Opérations" value={data.summary.operationCount} /></Grid>
        </Grid>
      )}
      <AppTable columns={cols} rows={data?.rows} rowKey={(r) => r.cashRegisterId} isLoading={isLoading} />
    </>
  );
}

function CategoryReportTab({ from, to, cashRegisterId, enabled }: BaseProps) {
  const { data, isLoading } = useCategoryReport({ from, to, cashRegisterId }, enabled);
  const cols: Column<CategoryReportRow>[] = [
    { key: 'code', header: 'Code', render: (r) => r.categoryCode },
    { key: 'label', header: 'Libellé', render: (r) => r.categoryLabel },
    { key: 'dir', header: 'Direction', render: (r) => <StatusBadge value={r.direction} /> },
    { key: 'total', header: 'Total', align: 'right', render: (r) => <CurrencyDisplay value={r.total} /> },
    { key: 'count', header: 'Nb', align: 'right', render: (r) => r.count }
  ];
  return <AppTable columns={cols} rows={data?.rows} rowKey={(r) => r.categoryId} isLoading={isLoading} />;
}

function VarianceReportTab({ from, to, cashRegisterId, enabled }: BaseProps) {
  const { data, isLoading } = useVarianceReport({ from, to, cashRegisterId }, enabled);
  const cols: Column<VarianceReportRow>[] = [
    { key: 'id', header: 'ID', render: (r) => `#${r.varianceCaseId}` },
    { key: 'session', header: 'Session', render: (r) => `#${r.cashSessionId}` },
    { key: 'caisse', header: 'Caisse', render: (r) => r.cashRegisterCode },
    { key: 'amt', header: 'Écart', align: 'right', render: (r) => <CurrencyDisplay value={r.varianceAmount} /> },
    { key: 'status', header: 'Statut', render: (r) => <StatusBadge value={r.status} /> },
    { key: 'date', header: 'Détecté le', render: (r) => formatDate(r.detectedAt, true) }
  ];
  return <AppTable columns={cols} rows={data?.rows} rowKey={(r) => r.varianceCaseId} isLoading={isLoading} />;
}

function TransferReportTab({ from, to, cashRegisterId, enabled }: BaseProps) {
  const { data, isLoading } = useTransferReport({ from, to, cashRegisterId }, enabled);
  const cols: Column<TransferReportRow>[] = [
    { key: 'ref', header: 'Référence', render: (r) => r.transferRef },
    { key: 'src', header: 'Source', render: (r) => r.sourceCode },
    { key: 'dst', header: 'Destination', render: (r) => r.destinationCode },
    { key: 'amt', header: 'Montant', align: 'right', render: (r) => <CurrencyDisplay value={r.amount} currency={r.currencyCode} /> },
    { key: 'status', header: 'Statut', render: (r) => <StatusBadge value={r.status} /> },
    { key: 'date', header: 'Date', render: (r) => formatDate(r.transferDate) }
  ];
  return <AppTable columns={cols} rows={data?.rows} rowKey={(r) => r.id} isLoading={isLoading} />;
}

function DepositReportTab({ from, to, cashRegisterId, enabled }: BaseProps) {
  const { data, isLoading } = useDepositReport({ from, to, cashRegisterId }, enabled);
  const cols: Column<DepositReportRow>[] = [
    { key: 'ref', header: 'Référence', render: (r) => r.depositRef },
    { key: 'caisse', header: 'Caisse', render: (r) => r.cashRegisterCode },
    { key: 'bank', header: 'Banque', render: (r) => r.bankName },
    { key: 'amt', header: 'Montant', align: 'right', render: (r) => <CurrencyDisplay value={r.amount} currency={r.currencyCode} /> },
    { key: 'status', header: 'Statut', render: (r) => <StatusBadge value={r.status} /> },
    { key: 'date', header: 'Date', render: (r) => formatDate(r.depositDate) }
  ];
  return <AppTable columns={cols} rows={data?.rows} rowKey={(r) => r.id} isLoading={isLoading} />;
}

function AnomalyReportTab({ from, to, cashRegisterId, enabled }: BaseProps) {
  const { data, isLoading } = useAnomalyReport({ from, to, cashRegisterId }, enabled);
  const cols: Column<AnomalyReportRow>[] = [
    { key: 'ref', header: 'Référence', render: (r) => r.reference },
    { key: 'sev', header: 'Sévérité', render: (r) => <StatusBadge value={r.severity} /> },
    { key: 'status', header: 'Statut', render: (r) => <StatusBadge value={r.status} /> },
    { key: 'caisse', header: 'Caisse', render: (r) => r.cashRegisterCode ?? '—' },
    { key: 'date', header: 'Détectée le', render: (r) => formatDate(r.detectedAt, true) }
  ];
  return <AppTable columns={cols} rows={data?.rows} rowKey={(r) => r.id} isLoading={isLoading} />;
}

function ApprovalReportTab({ from, to, enabled }: { from: string; to: string; enabled: boolean; }) {
  const { data, isLoading } = useApprovalReport({ from, to }, enabled);
  const cols: Column<ApprovalReportRow>[] = [
    { key: 'ref', header: 'Référence', render: (r) => r.requestRef },
    { key: 'target', header: 'Cible', render: (r) => `${r.targetType} (${r.targetEntityType})` },
    { key: 'amt', header: 'Montant', align: 'right', render: (r) => <CurrencyDisplay value={r.amount ?? undefined} /> },
    { key: 'status', header: 'Statut', render: (r) => <StatusBadge value={r.status} /> },
    { key: 'date', header: 'Demandé le', render: (r) => formatDate(r.requestedAt, true) }
  ];
  return <AppTable columns={cols} rows={data?.rows} rowKey={(r) => r.id} isLoading={isLoading} />;
}
