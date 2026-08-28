import {
  Box,
  Collapse,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Tooltip,
  Typography
} from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';
import PeopleIcon from '@mui/icons-material/People';
import PointOfSaleIcon from '@mui/icons-material/PointOfSale';
import CategoryIcon from '@mui/icons-material/Category';
import AssignmentIcon from '@mui/icons-material/Assignment';
import ListAltIcon from '@mui/icons-material/ListAlt';
import DownloadIcon from '@mui/icons-material/Download';
import BackupIcon from '@mui/icons-material/Backup';
import SettingsIcon from '@mui/icons-material/Settings';
import TuneIcon from '@mui/icons-material/Tune';
import ExtensionIcon from '@mui/icons-material/Extension';
import BusinessIcon from '@mui/icons-material/Business';
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import VerifiedUserIcon from '@mui/icons-material/VerifiedUser';
import SwapHorizIcon from '@mui/icons-material/SwapHoriz';
import AccountBalanceIcon from '@mui/icons-material/AccountBalance';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import CompareArrowsIcon from '@mui/icons-material/CompareArrows';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import LinkIcon from '@mui/icons-material/Link';
import BarChartIcon from '@mui/icons-material/BarChart';
import HistoryIcon from '@mui/icons-material/History';
import RuleIcon from '@mui/icons-material/Rule';
import MenuBookIcon from '@mui/icons-material/MenuBook';
import HourglassEmptyIcon from '@mui/icons-material/HourglassEmpty';
import SettingsApplicationsIcon from '@mui/icons-material/SettingsApplications';
import AccountTreeIcon from '@mui/icons-material/AccountTree';
import AutorenewIcon from '@mui/icons-material/Autorenew';
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong';
import FileDownloadIcon from '@mui/icons-material/FileDownload';
import { NavLink } from 'react-router-dom';
import { useIsFeatureEnabled, useFeatures } from '@/hooks/useFeatures';
import { useIsInAnyRole } from '@/hooks/useCurrentUser';
import { useThemeStore } from '@/app/store/themeStore';
import { useAuthStore } from '@/app/store/authStore';
import { FeatureCodes, RoleCodes } from '@/types/enums';

const DRAWER_WIDTH_EXPANDED = 240;
const DRAWER_WIDTH_COLLAPSED = 64;

interface NavEntry {
  label: string;
  to: string;
  icon: JSX.Element;
  feature?: string;
  roles?: string[];
}

// Tableau de bord — entrée seule, sans groupe.
const DASHBOARD: NavEntry = {
  label: 'Tableau de bord',
  to: '/dashboard',
  icon: <DashboardIcon />,
  feature: FeatureCodes.CORE_DASHBOARD
};

const CAISSE: NavEntry[] = [
  { label: 'Sessions de caisse', to: '/cash-sessions', icon: <PointOfSaleIcon />, feature: FeatureCodes.CORE_CASH_SESSIONS },
  { label: 'Opérations', to: '/cash-operations', icon: <ListAltIcon />, feature: FeatureCodes.CORE_OPERATIONS },
  { label: 'Caisses', to: '/cash-registers', icon: <AssignmentIcon />, feature: FeatureCodes.CORE_CASH_REGISTERS, roles: [RoleCodes.ADMIN, RoleCodes.SUPERVISOR] },
  { label: 'Catégories', to: '/categories', icon: <CategoryIcon />, feature: FeatureCodes.CORE_CATEGORIES, roles: [RoleCodes.ADMIN, RoleCodes.SUPERVISOR] }
];

const ADMINISTRATION: NavEntry[] = [
  { label: 'Utilisateurs', to: '/users', icon: <PeopleIcon />, feature: FeatureCodes.CORE_USERS, roles: [RoleCodes.ADMIN] },
  { label: 'Exports', to: '/exports', icon: <DownloadIcon />, feature: FeatureCodes.CORE_EXPORTS },
  { label: 'Sauvegardes', to: '/backups', icon: <BackupIcon />, feature: FeatureCodes.CORE_BACKUP, roles: [RoleCodes.ADMIN] }
];

const AVANCE: NavEntry[] = [
  { label: 'Validations', to: '/approval-requests', icon: <VerifiedUserIcon />, feature: FeatureCodes.ADV_VALIDATION },
  { label: 'Règles de validation', to: '/approval-rules', icon: <RuleIcon />, feature: FeatureCodes.ADV_VALIDATION, roles: [RoleCodes.ADMIN] },
  { label: 'Transferts', to: '/cash-transfers', icon: <SwapHorizIcon />, feature: FeatureCodes.ADV_TRANSFERS },
  { label: 'Dépôts banque', to: '/bank-deposits', icon: <AccountBalanceIcon />, feature: FeatureCodes.ADV_BANK_DEPOSITS },
  // Module Anomalies masqué dans l'UI (les routes /anomalies/* restent accessibles directement si besoin).
  // { label: 'Anomalies', to: '/anomalies', icon: <WarningAmberIcon />, feature: FeatureCodes.ADV_ANOMALIES },
  { label: 'Écarts', to: '/variances', icon: <CompareArrowsIcon />, feature: FeatureCodes.ADV_VARIANCE_MANAGEMENT },
  { label: 'Imports', to: '/imports', icon: <UploadFileIcon />, feature: FeatureCodes.ADV_IMPORTS, roles: [RoleCodes.ADMIN, RoleCodes.SUPERVISOR] },
  { label: 'Rapprochement', to: '/reconciliation', icon: <LinkIcon />, feature: FeatureCodes.ADV_RECONCILIATION, roles: [RoleCodes.ADMIN, RoleCodes.SUPERVISOR] },
  { label: 'Rapports avancés', to: '/reports', icon: <BarChartIcon />, feature: FeatureCodes.ADV_ADVANCED_REPORTS, roles: [RoleCodes.ADMIN, RoleCodes.SUPERVISOR] },
  { label: "Journal d'audit", to: '/audit-logs', icon: <HistoryIcon />, feature: FeatureCodes.ADV_AUDIT_LOG, roles: [RoleCodes.ADMIN, RoleCodes.SUPERVISOR] }
];

// Visible uniquement si mode ADVANCED + module ACCOUNTING activé + rôle SUPERVISOR
// (le feature flag ADV_ACCOUNTING ne peut être activé qu'en mode ADVANCED, cf. AppModeFeatures).
const COMPTABILITE: NavEntry[] = [
  { label: 'Tableau de bord', to: '/accounting/dashboard', icon: <DashboardIcon />, feature: FeatureCodes.ADV_ACCOUNTING, roles: [RoleCodes.SUPERVISOR] },
  { label: 'Paramètres comptables', to: '/accounting/settings', icon: <SettingsApplicationsIcon />, feature: FeatureCodes.ADV_ACCOUNTING, roles: [RoleCodes.SUPERVISOR] },
  { label: 'Plan comptable', to: '/accounting/chart-of-accounts', icon: <AccountTreeIcon />, feature: FeatureCodes.ADV_ACCOUNTING, roles: [RoleCodes.SUPERVISOR] },
  { label: 'Journaux comptables', to: '/accounting/journals', icon: <MenuBookIcon />, feature: FeatureCodes.ADV_ACCOUNTING, roles: [RoleCodes.SUPERVISOR] },
  { label: 'Générations', to: '/accounting/generations', icon: <AutorenewIcon />, feature: FeatureCodes.ADV_ACCOUNTING, roles: [RoleCodes.SUPERVISOR] },
  { label: 'Brouillard comptable', to: '/accounting/ledger', icon: <ReceiptLongIcon />, feature: FeatureCodes.ADV_ACCOUNTING, roles: [RoleCodes.SUPERVISOR] },
  { label: 'Opérations en attente', to: '/accounting/pending', icon: <HourglassEmptyIcon />, feature: FeatureCodes.ADV_ACCOUNTING, roles: [RoleCodes.SUPERVISOR] },
  { label: 'Exports comptables', to: '/accounting/exports', icon: <FileDownloadIcon />, feature: FeatureCodes.ADV_ACCOUNTING, roles: [RoleCodes.SUPERVISOR] }
];

const CONFIGURATION: NavEntry[] = [
  // « Infos entreprise » est désormais une section interne de la page Paramètres généraux.
  { label: 'Paramètres généraux', to: '/settings/general', icon: <SettingsIcon />, feature: FeatureCodes.CORE_SETTINGS, roles: [RoleCodes.ADMIN] },
  { label: "Mode de l'app", to: '/settings/app-mode', icon: <TuneIcon />, feature: FeatureCodes.CORE_SETTINGS, roles: [RoleCodes.ADMIN] },
  { label: 'Modules', to: '/settings/features', icon: <ExtensionIcon />, feature: FeatureCodes.CORE_SETTINGS, roles: [RoleCodes.ADMIN] }
];

function NavItem({ entry, collapsed, onNavigate }: { entry: NavEntry; collapsed: boolean; onNavigate?: () => void }) {
  const featureOk = useIsFeatureEnabled(entry.feature ?? '__none__') || !entry.feature;
  const roleOk = useIsInAnyRole(entry.roles ?? []) || !entry.roles?.length;
  if (!featureOk || !roleOk) return null;

  const button = (
    <ListItemButton
      component={NavLink}
      to={entry.to}
      onClick={onNavigate}
      sx={{
        minHeight: 44,
        justifyContent: collapsed ? 'center' : 'flex-start',
        px: collapsed ? 1.5 : 2,
        '&.active': {
          bgcolor: 'action.selected',
          color: 'primary.main',
          '& .MuiListItemIcon-root': { color: 'primary.main' }
        }
      }}
    >
      <ListItemIcon sx={{ minWidth: 0, mr: collapsed ? 0 : 2, justifyContent: 'center' }}>
        {entry.icon}
      </ListItemIcon>
      {!collapsed && <ListItemText primary={entry.label} />}
    </ListItemButton>
  );

  return collapsed ? <Tooltip title={entry.label} placement="right" arrow>{button}</Tooltip> : button;
}

interface SectionProps {
  id: string;
  title: string;
  entries: NavEntry[];
  sidebarCollapsed: boolean;
  onNavigate?: () => void;
}

function Section({ id, title, entries, sidebarCollapsed, onNavigate }: SectionProps) {
  const features = useFeatures();
  const userRole = useAuthStore((s) => s.user?.roleCode);
  const collapsedSections = useThemeStore((s) => s.collapsedSections);
  const toggleSection = useThemeStore((s) => s.toggleSection);

  const enabledCodes = new Set(features.filter((f) => f.isEnabled).map((f) => f.featureCode));
  const visibleEntries = entries.filter((e) => {
    const featureOk = !e.feature || enabledCodes.has(e.feature);
    const roleOk = !e.roles?.length || (userRole ? e.roles.includes(userRole) : false);
    return featureOk && roleOk;
  });

  if (visibleEntries.length === 0) return null;

  // En mode sidebar réduite, on rend toutes les sections "plates" (chevrons cachés, pas de header).
  if (sidebarCollapsed) {
    return (
      <List dense disablePadding>
        {visibleEntries.map((e) => <NavItem key={e.to} entry={e} collapsed={true} onNavigate={onNavigate} />)}
      </List>
    );
  }

  const isCollapsed = collapsedSections.includes(id);

  return (
    <Box>
      <ListItemButton
        onClick={() => toggleSection(id)}
        sx={{ px: 2, py: 0.5, minHeight: 36 }}
      >
        <ListItemText
          primary={
            <Typography variant="overline" color="text.secondary" sx={{ lineHeight: 1.6 }}>
              {title}
            </Typography>
          }
        />
        {isCollapsed ? <ExpandMoreIcon fontSize="small" color="action" /> : <ExpandLessIcon fontSize="small" color="action" />}
      </ListItemButton>
      <Collapse in={!isCollapsed} timeout="auto" unmountOnExit>
        <List dense disablePadding>
          {visibleEntries.map((e) => <NavItem key={e.to} entry={e} collapsed={false} onNavigate={onNavigate} />)}
        </List>
      </Collapse>
    </Box>
  );
}

interface SidebarProps {
  mobileOpen: boolean;
  onMobileClose: () => void;
}

function DrawerContent({ collapsed, onNavigate }: { collapsed: boolean; onNavigate?: () => void }) {
  return (
    <>
      {/* Tableau de bord — entrée seule sans groupe */}
      <List disablePadding>
        <NavItem entry={DASHBOARD} collapsed={collapsed} onNavigate={onNavigate} />
      </List>
      <Divider />

      <Section id="caisse"         title="Caisse"         entries={CAISSE}         sidebarCollapsed={collapsed} onNavigate={onNavigate} />
      <Section id="administration" title="Administration" entries={ADMINISTRATION} sidebarCollapsed={collapsed} onNavigate={onNavigate} />
      <Section id="avance"         title="Avancé"         entries={AVANCE}         sidebarCollapsed={collapsed} onNavigate={onNavigate} />
      <Section id="comptabilite"   title="Comptabilité"   entries={COMPTABILITE}   sidebarCollapsed={collapsed} onNavigate={onNavigate} />
      <Section id="configuration"  title="Configuration"  entries={CONFIGURATION}  sidebarCollapsed={collapsed} onNavigate={onNavigate} />
    </>
  );
}

export function Sidebar({ mobileOpen, onMobileClose }: SidebarProps) {
  const collapsed = useThemeStore((s) => s.sidebarCollapsed);
  const toggleSidebar = useThemeStore((s) => s.toggleSidebar);
  const width = collapsed ? DRAWER_WIDTH_COLLAPSED : DRAWER_WIDTH_EXPANDED;

  return (
    <>
      {/* Mobile : tiroir en superposition, toujours pleine largeur, se ferme après un clic */}
      <Drawer
        variant="temporary"
        open={mobileOpen}
        onClose={onMobileClose}
        ModalProps={{ keepMounted: true }}
        sx={{
          display: { xs: 'block', md: 'none' },
          '& .MuiDrawer-paper': { width: DRAWER_WIDTH_EXPANDED, boxSizing: 'border-box' }
        }}
      >
        <Toolbar sx={{ px: 2, justifyContent: 'space-between', minHeight: 64 }}>
          <Typography variant="h6" color="primary">CashApp</Typography>
        </Toolbar>
        <Divider />
        <DrawerContent collapsed={false} onNavigate={onMobileClose} />
      </Drawer>

      {/* Desktop : tiroir permanent, largeur repliable */}
      <Drawer
        variant="permanent"
        sx={{
          display: { xs: 'none', md: 'block' },
          width,
          flexShrink: 0,
          whiteSpace: 'nowrap',
          '& .MuiDrawer-paper': {
            width,
            boxSizing: 'border-box',
            borderRight: '1px solid',
            borderColor: 'divider',
            overflowX: 'hidden',
            transition: 'width 200ms ease'
          }
        }}
      >
        <Toolbar sx={{ px: collapsed ? 1 : 2, justifyContent: 'space-between', minHeight: 64 }}>
          {!collapsed && <Typography variant="h6" color="primary">CashApp</Typography>}
          <IconButton size="small" onClick={toggleSidebar} aria-label={collapsed ? 'Déplier' : 'Réduire'}>
            {collapsed ? <ChevronRightIcon /> : <ChevronLeftIcon />}
          </IconButton>
        </Toolbar>
        <Divider />
        <DrawerContent collapsed={collapsed} />
      </Drawer>
    </>
  );
}

export const SIDEBAR_WIDTH = DRAWER_WIDTH_EXPANDED;
export const SIDEBAR_WIDTH_COLLAPSED = DRAWER_WIDTH_COLLAPSED;
