import { Box } from '@mui/material';
import { Outlet } from 'react-router-dom';
import { Sidebar, SIDEBAR_WIDTH, SIDEBAR_WIDTH_COLLAPSED } from './Sidebar';
import { Topbar } from './Topbar';
import { useThemeStore } from '@/app/store/themeStore';
import { SnackbarHost } from '@/components/feedback/SnackbarHost';

export function AppLayout() {
  const collapsed = useThemeStore((s) => s.sidebarCollapsed);
  const sidebarWidth = collapsed ? SIDEBAR_WIDTH_COLLAPSED : SIDEBAR_WIDTH;

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <Sidebar />
      <Box sx={{ flexGrow: 1, width: `calc(100% - ${sidebarWidth}px)`, transition: 'width 200ms ease' }}>
        <Topbar />
        <Box component="main">
          <Outlet />
        </Box>
      </Box>
      <SnackbarHost />
    </Box>
  );
}
