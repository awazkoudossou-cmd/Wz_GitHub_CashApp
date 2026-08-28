import { useState } from 'react';
import { Box } from '@mui/material';
import { Outlet } from 'react-router-dom';
import { Sidebar, SIDEBAR_WIDTH, SIDEBAR_WIDTH_COLLAPSED } from './Sidebar';
import { Topbar } from './Topbar';
import { useThemeStore } from '@/app/store/themeStore';
import { SnackbarHost } from '@/components/feedback/SnackbarHost';

export function AppLayout() {
  const collapsed = useThemeStore((s) => s.sidebarCollapsed);
  const sidebarWidth = collapsed ? SIDEBAR_WIDTH_COLLAPSED : SIDEBAR_WIDTH;
  const [mobileNavOpen, setMobileNavOpen] = useState(false);

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <Sidebar mobileOpen={mobileNavOpen} onMobileClose={() => setMobileNavOpen(false)} />
      <Box
        sx={{
          flexGrow: 1,
          minWidth: 0,
          width: { xs: '100%', md: `calc(100% - ${sidebarWidth}px)` },
          transition: 'width 200ms ease'
        }}
      >
        <Topbar onMenuClick={() => setMobileNavOpen(true)} />
        <Box component="main">
          <Outlet />
        </Box>
      </Box>
      <SnackbarHost />
    </Box>
  );
}
