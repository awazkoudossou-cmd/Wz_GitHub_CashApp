import { AppBar, Box, Button, IconButton, MenuItem, Stack, TextField, Toolbar, Tooltip, Typography } from '@mui/material';
import LogoutIcon from '@mui/icons-material/Logout';
import DarkModeIcon from '@mui/icons-material/DarkMode';
import LightModeIcon from '@mui/icons-material/LightMode';
import MenuIcon from '@mui/icons-material/Menu';
import { useAuthStore } from '@/app/store/authStore';
import { useThemeStore } from '@/app/store/themeStore';
import { useLogout } from '@/hooks/useAuth';

export function Topbar({ onMenuClick }: { onMenuClick: () => void }) {
  const user = useAuthStore((s) => s.user);
  const cashRegisters = useAuthStore((s) => s.cashRegisters);
  const selectedId = useAuthStore((s) => s.selectedCashRegisterId);
  const setSelected = useAuthStore((s) => s.setSelectedCashRegister);
  const logout = useLogout();
  const themeMode = useThemeStore((s) => s.mode);
  const toggleTheme = useThemeStore((s) => s.toggle);

  return (
    <AppBar position="sticky" color="default" sx={{ bgcolor: 'background.paper' }}>
      <Toolbar>
        <IconButton
          onClick={onMenuClick}
          color="inherit"
          aria-label="Ouvrir le menu"
          sx={{ display: { xs: 'inline-flex', md: 'none' }, mr: 1 }}
        >
          <MenuIcon />
        </IconButton>
        <Box sx={{ flexGrow: 1, display: { xs: 'none', sm: 'block' } }}>
          <Typography variant="body2" color="text.secondary" noWrap>
            Connecté en tant que <b>{user?.fullName}</b> ({user?.roleCode})
          </Typography>
        </Box>
        <Box sx={{ flexGrow: 1, display: { xs: 'block', sm: 'none' } }} />
        <Stack direction="row" spacing={{ xs: 1, sm: 2 }} alignItems="center">
          {cashRegisters.length > 0 && (
            <TextField
              select
              size="small"
              label="Caisse"
              value={selectedId ?? ''}
              onChange={(e) => setSelected(e.target.value ? Number(e.target.value) : null)}
              sx={{ minWidth: { xs: 110, sm: 180 } }}
            >
              {cashRegisters.map((c) => (
                <MenuItem key={c.id} value={c.id}>
                  {c.code} — {c.name}
                </MenuItem>
              ))}
            </TextField>
          )}
          <Tooltip title={themeMode === 'dark' ? 'Passer en clair' : 'Passer en sombre'}>
            <IconButton onClick={toggleTheme} color="inherit">
              {themeMode === 'dark' ? <LightModeIcon /> : <DarkModeIcon />}
            </IconButton>
          </Tooltip>
          <Button
            color="inherit"
            startIcon={<LogoutIcon />}
            onClick={logout}
            sx={{ minWidth: 0, px: { xs: 1, sm: 2 }, '& .MuiButton-startIcon': { mr: { xs: 0, sm: 1 } } }}
          >
            <Box component="span" sx={{ display: { xs: 'none', sm: 'inline' } }}>Déconnexion</Box>
          </Button>
        </Stack>
      </Toolbar>
    </AppBar>
  );
}
