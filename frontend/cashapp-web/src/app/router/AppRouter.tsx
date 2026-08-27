import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { AppLayout } from '@/components/layout/AppLayout';
import { LoginPage } from '@/pages/LoginPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { UsersListPage } from '@/pages/UsersListPage';
import { UserFormPage } from '@/pages/UserFormPage';
import { UserDetailPage } from '@/pages/UserDetailPage';
import { CashRegistersListPage } from '@/pages/CashRegistersListPage';
import { CashRegisterFormPage } from '@/pages/CashRegisterFormPage';
import { CashRegisterDetailPage } from '@/pages/CashRegisterDetailPage';
import { CategoriesPage } from '@/pages/CategoriesPage';
import { CashSessionsListPage } from '@/pages/CashSessionsListPage';
import { CashSessionDetailPage } from '@/pages/CashSessionDetailPage';
import { CashOperationsListPage } from '@/pages/CashOperationsListPage';
import { CashOperationFormPage } from '@/pages/CashOperationFormPage';
import { CashOperationDetailPage } from '@/pages/CashOperationDetailPage';
import { ExportsPage } from '@/pages/ExportsPage';
import { BackupsPage } from '@/pages/BackupsPage';
import { GeneralSettingsPage } from '@/pages/GeneralSettingsPage';
import { AppModeSettingsPage } from '@/pages/AppModeSettingsPage';
import { FeatureSettingsPage } from '@/pages/FeatureSettingsPage';
import { CompanyInfoSettingsPage } from '@/pages/CompanyInfoSettingsPage';
import { NotFoundPage } from '@/pages/NotFoundPage';
import { ForbiddenPage } from '@/pages/ForbiddenPage';
// V2
import { ApprovalRulesPage } from '@/pages/v2/ApprovalRulesPage';
import { ApprovalRequestsPage } from '@/pages/v2/ApprovalRequestsPage';
import { ApprovalRequestDetailPage } from '@/pages/v2/ApprovalRequestDetailPage';
import { CashTransfersListPage } from '@/pages/v2/CashTransfersListPage';
import { CashTransferFormPage } from '@/pages/v2/CashTransferFormPage';
import { CashTransferDetailPage } from '@/pages/v2/CashTransferDetailPage';
import { BankDepositsListPage } from '@/pages/v2/BankDepositsListPage';
import { BankDepositFormPage } from '@/pages/v2/BankDepositFormPage';
import { BankDepositDetailPage } from '@/pages/v2/BankDepositDetailPage';
import { AnomaliesListPage } from '@/pages/v2/AnomaliesListPage';
import { AnomalyDetailPage } from '@/pages/v2/AnomalyDetailPage';
import { VariancesListPage } from '@/pages/v2/VariancesListPage';
import { VarianceDetailPage } from '@/pages/v2/VarianceDetailPage';
import { ImportsListPage } from '@/pages/v2/ImportsListPage';
import { ImportDetailPage } from '@/pages/v2/ImportDetailPage';
import { ReconciliationListPage } from '@/pages/v2/ReconciliationListPage';
import { ReconciliationDetailPage } from '@/pages/v2/ReconciliationDetailPage';
import { ReportsHubPage } from '@/pages/v2/ReportsHubPage';
import { AuditLogsPage } from '@/pages/v2/AuditLogsPage';
// V3 — Comptabilité
import { AccountingDashboardPage } from '@/pages/v3/AccountingDashboardPage';
import { AccountingSettingsPage } from '@/pages/v3/AccountingSettingsPage';
import { AccountingChartOfAccountsPage } from '@/pages/v3/AccountingChartOfAccountsPage';
import { AccountingJournalsPage } from '@/pages/v3/AccountingJournalsPage';
import { AccountingLedgerPage } from '@/pages/v3/AccountingLedgerPage';
import { AccountingGenerationsPage } from '@/pages/v3/AccountingGenerationsPage';
import { AccountingPendingPage } from '@/pages/v3/AccountingPendingPage';
import { AccountingExportsPage } from '@/pages/v3/AccountingExportsPage';
import { FeatureCodes, RoleCodes } from '@/types/enums';

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/403" element={<ForbiddenPage />} />

        <Route element={<ProtectedRoute />}>
          <Route element={<AppLayout />}>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />

            {/* === V1 === */}
            <Route element={<ProtectedRoute feature={FeatureCodes.CORE_DASHBOARD} />}>
              <Route path="/dashboard" element={<DashboardPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.CORE_USERS} roles={[RoleCodes.ADMIN]} />}>
              <Route path="/users" element={<UsersListPage />} />
              <Route path="/users/new" element={<UserFormPage />} />
              <Route path="/users/:id" element={<UserDetailPage />} />
              <Route path="/users/:id/edit" element={<UserFormPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.CORE_CASH_REGISTERS} />}>
              <Route path="/cash-registers" element={<CashRegistersListPage />} />
              <Route path="/cash-registers/new" element={<CashRegisterFormPage />} />
              <Route path="/cash-registers/:id" element={<CashRegisterDetailPage />} />
              <Route path="/cash-registers/:id/edit" element={<CashRegisterFormPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.CORE_CATEGORIES} />}>
              <Route path="/categories" element={<CategoriesPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.CORE_CASH_SESSIONS} />}>
              <Route path="/cash-sessions" element={<CashSessionsListPage />} />
              <Route path="/cash-sessions/:id" element={<CashSessionDetailPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.CORE_OPERATIONS} />}>
              <Route path="/cash-operations" element={<CashOperationsListPage />} />
              <Route path="/cash-operations/new" element={<CashOperationFormPage />} />
              <Route path="/cash-operations/:id" element={<CashOperationDetailPage />} />
              <Route path="/cash-operations/:id/edit" element={<CashOperationFormPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.CORE_EXPORTS} />}>
              <Route path="/exports" element={<ExportsPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.CORE_BACKUP} roles={[RoleCodes.ADMIN]} />}>
              <Route path="/backups" element={<BackupsPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.CORE_SETTINGS} roles={[RoleCodes.ADMIN]} />}>
              <Route path="/settings/general" element={<GeneralSettingsPage />} />
              <Route path="/settings/company" element={<CompanyInfoSettingsPage />} />
              <Route path="/settings/app-mode" element={<AppModeSettingsPage />} />
              <Route path="/settings/features" element={<FeatureSettingsPage />} />
            </Route>

            {/* === V2 === */}
            <Route element={<ProtectedRoute feature={FeatureCodes.ADV_VALIDATION} />}>
              <Route path="/approval-requests" element={<ApprovalRequestsPage />} />
              <Route path="/approval-requests/:id" element={<ApprovalRequestDetailPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.ADV_VALIDATION} roles={[RoleCodes.ADMIN]} />}>
              <Route path="/approval-rules" element={<ApprovalRulesPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.ADV_TRANSFERS} />}>
              <Route path="/cash-transfers" element={<CashTransfersListPage />} />
              <Route path="/cash-transfers/new" element={<CashTransferFormPage />} />
              <Route path="/cash-transfers/:id" element={<CashTransferDetailPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.ADV_BANK_DEPOSITS} />}>
              <Route path="/bank-deposits" element={<BankDepositsListPage />} />
              <Route path="/bank-deposits/new" element={<BankDepositFormPage />} />
              <Route path="/bank-deposits/:id" element={<BankDepositDetailPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.ADV_ANOMALIES} />}>
              <Route path="/anomalies" element={<AnomaliesListPage />} />
              <Route path="/anomalies/:id" element={<AnomalyDetailPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.ADV_VARIANCE_MANAGEMENT} />}>
              <Route path="/variances" element={<VariancesListPage />} />
              <Route path="/variances/:id" element={<VarianceDetailPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.ADV_IMPORTS} roles={[RoleCodes.ADMIN, RoleCodes.SUPERVISOR]} />}>
              <Route path="/imports" element={<ImportsListPage />} />
              <Route path="/imports/:id" element={<ImportDetailPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.ADV_RECONCILIATION} roles={[RoleCodes.ADMIN, RoleCodes.SUPERVISOR]} />}>
              <Route path="/reconciliation" element={<ReconciliationListPage />} />
              <Route path="/reconciliation/:id" element={<ReconciliationDetailPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.ADV_ADVANCED_REPORTS} roles={[RoleCodes.ADMIN, RoleCodes.SUPERVISOR]} />}>
              <Route path="/reports" element={<ReportsHubPage />} />
            </Route>
            <Route element={<ProtectedRoute feature={FeatureCodes.ADV_AUDIT_LOG} roles={[RoleCodes.ADMIN, RoleCodes.SUPERVISOR]} />}>
              <Route path="/audit-logs" element={<AuditLogsPage />} />
            </Route>

            {/* === V3 — Comptabilité (visible uniquement mode ADVANCED + module ACCOUNTING + rôle SUPERVISOR) === */}
            <Route element={<ProtectedRoute feature={FeatureCodes.ADV_ACCOUNTING} roles={[RoleCodes.SUPERVISOR]} />}>
              <Route path="/accounting/dashboard" element={<AccountingDashboardPage />} />
              <Route path="/accounting/settings" element={<AccountingSettingsPage />} />
              <Route path="/accounting/chart-of-accounts" element={<AccountingChartOfAccountsPage />} />
              <Route path="/accounting/journals" element={<AccountingJournalsPage />} />
              <Route path="/accounting/ledger" element={<AccountingLedgerPage />} />
              <Route path="/accounting/generations" element={<AccountingGenerationsPage />} />
              <Route path="/accounting/pending" element={<AccountingPendingPage />} />
              <Route path="/accounting/exports" element={<AccountingExportsPage />} />
            </Route>
          </Route>
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  );
}
