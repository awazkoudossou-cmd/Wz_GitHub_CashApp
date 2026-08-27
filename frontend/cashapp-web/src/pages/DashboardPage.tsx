import { Alert, Grid, Stack, Typography } from '@mui/material';
import AssignmentLateIcon from '@mui/icons-material/AssignmentLate';
import BalanceIcon from '@mui/icons-material/Balance';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { StatCard } from '@/components/common/StatCard';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { TrendAreaChart } from '@/components/dashboard/TrendAreaChart';
import { CategoryPieChart } from '@/components/dashboard/CategoryPieChart';
import { RegisterBarChart } from '@/components/dashboard/RegisterBarChart';
import { RecentOperationsList } from '@/components/dashboard/RecentOperationsList';
import { OpenSessionsList } from '@/components/dashboard/OpenSessionsList';
import { ActionWidget } from '@/components/dashboard/ActionWidget';
import { useAuthStore } from '@/app/store/authStore';
import { useCashierDashboard, useSupervisorDashboard } from '@/modules/dashboard/hooks';
import { useIsInAnyRole } from '@/hooks/useCurrentUser';
import { RoleCodes } from '@/types/enums';
import { formatDate } from '@/utils/format';

export function DashboardPage() {
  const cashRegisterId = useAuthStore((s) => s.selectedCashRegisterId);
  const isSupervisor = useIsInAnyRole([RoleCodes.ADMIN, RoleCodes.SUPERVISOR]);

  const cashier = useCashierDashboard(cashRegisterId);
  const supervisor = useSupervisorDashboard();

  return (
    <PageContainer>
      <PageHeader title="Tableau de bord" subtitle="Vue d'ensemble de l'activité de caisse" />
      {isSupervisor && (
        <>
          <Typography variant="overline" color="text.secondary">Vue superviseur</Typography>
          {supervisor.isLoading && <LoadingScreen />}
          {supervisor.data && (
            <Stack spacing={2} mb={4} mt={1}>
              <Grid container spacing={2} alignItems="stretch">
                <Grid item xs={12} sm={6} md={2.4}>
                  <StatCard label="Sessions ouvertes" value={supervisor.data.openSessionsCount} />
                </Grid>
                <Grid item xs={12} sm={6} md={2.4}>
                  <StatCard label="Sessions clôturées aujourd'hui" value={supervisor.data.todayClosedSessionsCount} />
                </Grid>
                <Grid item xs={12} sm={6} md={2.4}>
                  <StatCard label="Entrées du jour" value={<CurrencyDisplay value={supervisor.data.todayTotalIn} />} color="success" />
                </Grid>
                <Grid item xs={12} sm={6} md={2.4}>
                  <StatCard label="Sorties du jour" value={<CurrencyDisplay value={supervisor.data.todayTotalOut} />} color="warning" />
                </Grid>
                <Grid item xs={12} sm={6} md={2.4}>
                  <StatCard label="Net du jour" value={<CurrencyDisplay value={supervisor.data.todayNetMovement} />} />
                </Grid>
              </Grid>

              {(supervisor.data.approvalsFeatureEnabled || supervisor.data.varianceFeatureEnabled) && (
                <Grid container spacing={2} alignItems="stretch">
                  {supervisor.data.approvalsFeatureEnabled && (
                    <Grid item xs={12} sm={6} md={3}>
                      <ActionWidget
                        icon={<AssignmentLateIcon />}
                        label="Demandes d'approbation en attente"
                        count={supervisor.data.pendingApprovalsCount}
                        hint="Cliquer pour traiter la file d'attente"
                        to="/approval-requests"
                        color={supervisor.data.pendingApprovalsCount > 0 ? 'warning' : 'primary'}
                      />
                    </Grid>
                  )}
                  {supervisor.data.varianceFeatureEnabled && (
                    <Grid item xs={12} sm={6} md={3}>
                      <ActionWidget
                        icon={<BalanceIcon />}
                        label="Écarts de caisse ouverts"
                        count={supervisor.data.openVarianceCasesCount}
                        hint="Cliquer pour justifier les écarts"
                        to="/variances"
                        color={supervisor.data.openVarianceCasesCount > 0 ? 'error' : 'primary'}
                      />
                    </Grid>
                  )}
                </Grid>
              )}

              <Grid container spacing={2} alignItems="stretch">
                <Grid item xs={12} md={8} sx={{ minWidth: 0 }}>
                  <TrendAreaChart title="Évolution entrées / sorties (14 derniers jours)" data={supervisor.data.trend14Days} />
                </Grid>
                <Grid item xs={12} md={4} sx={{ minWidth: 0 }}>
                  <CategoryPieChart title="Top catégories (30 derniers jours)" data={supervisor.data.topCategories30Days} />
                </Grid>
              </Grid>

              <Grid container spacing={2} alignItems="stretch">
                <Grid item xs={12} md={6} sx={{ minWidth: 0 }}>
                  <RegisterBarChart title="Solde net du jour par caisse" data={supervisor.data.registerBreakdownToday} />
                </Grid>
                <Grid item xs={12} md={6} sx={{ minWidth: 0 }}>
                  <OpenSessionsList sessions={supervisor.data.openSessions} />
                </Grid>
              </Grid>
            </Stack>
          )}
        </>
      )}

      <Typography variant="overline" color="text.secondary">Vue caissier</Typography>
      {!cashRegisterId && (
        <Alert severity="info" sx={{ mt: 1 }}>
          Sélectionne une caisse dans la barre supérieure pour afficher la vue caissier.
        </Alert>
      )}
      {cashier.isLoading && <LoadingScreen />}
      {cashier.data && (
        <Stack spacing={2} mt={1}>
          {cashier.data.activeSession ? (
            <Grid container spacing={2} alignItems="stretch">
              <Grid item xs={12} sm={6} md={3}>
                <StatCard label="Session active" value={`#${cashier.data.activeSession.id}`} hint={formatDate(cashier.data.activeSession.openedAt, true)} />
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <StatCard label="Solde d'ouverture" value={<CurrencyDisplay value={cashier.data.activeSession.openingBalance} />} />
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <StatCard label="Solde théorique" value={<CurrencyDisplay value={cashier.data.activeSession.currentTheoreticalBalance} />} color="primary" />
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <StatCard label="Opérations" value={cashier.data.activeSession.operationCount} />
              </Grid>
            </Grid>
          ) : (
            <Alert severity="warning">Aucune session active sur cette caisse.</Alert>
          )}
          <Grid container spacing={2} alignItems="stretch">
            <Grid item xs={12} sm={6} md={4}>
              <StatCard label="Entrées du jour" value={<CurrencyDisplay value={cashier.data.todayTotalIn} />} color="success" />
            </Grid>
            <Grid item xs={12} sm={6} md={4}>
              <StatCard label="Sorties du jour" value={<CurrencyDisplay value={cashier.data.todayTotalOut} />} color="warning" />
            </Grid>
            <Grid item xs={12} sm={6} md={4}>
              <StatCard label="Opérations du jour" value={cashier.data.todayOperationCount} />
            </Grid>
          </Grid>

          <Grid container spacing={2} alignItems="stretch">
            <Grid item xs={12} md={7} sx={{ minWidth: 0 }}>
              <TrendAreaChart title="Évolution entrées / sorties (7 derniers jours)" data={cashier.data.trend7Days} height={220} />
            </Grid>
            <Grid item xs={12} md={5} sx={{ minWidth: 0 }}>
              <CategoryPieChart title="Répartition des catégories (aujourd'hui)" data={cashier.data.todayCategoryBreakdown} height={220} />
            </Grid>
          </Grid>

          <RecentOperationsList title="Dernières opérations" operations={cashier.data.recentOperations} />
        </Stack>
      )}
    </PageContainer>
  );
}
