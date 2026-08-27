import { AppBar, Box, Button, IconButton, MenuItem, Stack, TextField, Toolbar, Tooltip, Typography } from '@mui/material';
import LogoutIcon from '@mui/icons-material/Logout';
import DarkModeIcon from '@mui/icons-material/DarkMode';
import LightModeIcon from '@mui/icons-material/LightMode';
import { useAuthStore } from '@/app/store/authStore';
import { useThemeStore } from '@/app/store/themeStore';
import { useLogout } from '@/hooks/useAuth';

export function Topbar() {
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
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="body2" color="text.secondary">
            Connecté en tant que <b>{user?.fullName}</b> ({user?.roleCode})
          </Typography>
        </Box>
        <Stack direction="row" spacing={2} alignItems="center">
          {cashRegisters.length > 0 && (
            <TextField
              select
              size="small"
              label="Caisse"
              value={selectedId ?? ''}
              onChange={(e) => setSelected(e.target.value ? Number(e.target.value) : null)}
              sx={{ minWidth: 180 }}
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
          <Button color="inherit" startIcon={<LogoutIcon />} onClick={logout}>
            Déconnexion
          </Button>
        </Stack>
      </Toolbar>
    </AppBar>
  );
}
