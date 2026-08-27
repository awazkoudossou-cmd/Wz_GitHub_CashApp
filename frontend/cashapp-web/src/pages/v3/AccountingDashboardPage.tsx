import { Grid, Typography } from '@mui/material';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatCard } from '@/components/common/StatCard';
import { AccountingDailyBarChart } from '@/components/dashboard/AccountingDailyBarChart';
import { AccountingDistributionPieChart } from '@/components/dashboard/AccountingDistributionPieChart';
import { useAccountingDashboard } from '@/modules/accounting/hooks';
import { formatDate } from '@/utils/format';

export function AccountingDashboardPage() {
  const dashQ = useAccountingDashboard();
  const d = dashQ.data;

  if (dashQ.isLoading) return <LoadingScreen />;

  return (
    <PageContainer>
      <PageHeader title="Tableau de bord comptable" subtitle="Vue d'ensemble du module Comptabilité" />

      <Grid container spacing={2} mb={3}>
        <Grid item xs={6} sm={4} md={2}>
          <StatCard label="Comptes" value={d?.accountCount ?? '—'} />
        </Grid>
        <Grid item xs={6} sm={4} md={2}>
          <StatCard label="Journaux" value={d?.journalCount ?? '—'} />
        </Grid>
        <Grid item xs={6} sm={4} md={2}>
          <StatCard label="Catégories configurées" value={d?.configuredCategoryCount ?? '—'} />
        </Grid>
        <Grid item xs={6} sm={4} md={2}>
          <StatCard label="Caisses configurées" value={d?.configuredCashRegisterCount ?? '—'} />
        </Grid>
        <Grid item xs={6} sm={4} md={2}>
          <StatCard label="Batchs" value={d?.batchCount ?? '—'} />
        </Grid>
        <Grid item xs={6} sm={4} md={2}>
          <StatCard label="Écritures" value={d?.entryCount ?? '—'} />
        </Grid>
      </Grid>

      <Grid container spacing={2} mb={3}>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard label="Batchs aujourd'hui" value={d?.batchesToday ?? '—'} color="info" />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard label="Écritures aujourd'hui" value={d?.entriesToday ?? '—'} color="info" />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard label="Exports aujourd'hui" value={d?.exportsToday ?? '—'} color="success" />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard label="Erreurs" value={d?.errorsCount ?? '—'} color={d && d.errorsCount > 0 ? 'error' : 'text.primary'} />
        </Grid>
      </Grid>

      <Grid container spacing={2} mb={3}>
        <Grid item xs={12} sm={6}>
          <StatCard
            label="Dernière génération"
            value={d?.lastGenerationReference ?? '—'}
            hint={d?.lastGenerationAt ? formatDate(d.lastGenerationAt, true) : undefined}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <StatCard
            label="Dernier export"
            value={d?.lastExportFileName ?? '—'}
            hint={d?.lastExportAt ? formatDate(d.lastExportAt, true) : undefined}
          />
        </Grid>
      </Grid>

      <Typography variant="overline" color="text.secondary">Évolution</Typography>
      <Grid container spacing={2} mb={3} mt={0.5}>
        <Grid item xs={12} md={6} sx={{ minWidth: 0 }}>
          <AccountingDailyBarChart title="Évolution des écritures (14 derniers jours)" data={d?.entriesByDay ?? []} />
        </Grid>
        <Grid item xs={12} md={6} sx={{ minWidth: 0 }}>
          <AccountingDailyBarChart title="Générations par jour (14 derniers jours)" data={d?.generationsByDay ?? []} />
        </Grid>
      </Grid>

      <Typography variant="overline" color="text.secondary">Répartition</Typography>
      <Grid container spacing={2} mt={0.5}>
        <Grid item xs={12} md={6} sx={{ minWidth: 0 }}>
          <AccountingDistributionPieChart title="Répartition des journaux" data={d?.journalDistribution ?? []} />
        </Grid>
        <Grid item xs={12} md={6} sx={{ minWidth: 0 }}>
          <AccountingDistributionPieChart title="Répartition des comptes" data={d?.accountDistribution ?? []} />
        </Grid>
      </Grid>
    </PageContainer>
  );
}
