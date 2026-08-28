import { createTheme, type Theme } from '@mui/material/styles';

const baseTokens = {
  typography: {
    fontFamily: '"Inter", system-ui, -apple-system, sans-serif',
    h6: { fontWeight: 600 },
    h5: { fontWeight: 600 },
    h4: { fontWeight: 600 },
    button: { textTransform: 'none' as const, fontWeight: 600 }
  },
  shape: { borderRadius: 8 }
};

export const lightTheme: Theme = createTheme({
  ...baseTokens,
  palette: {
    mode: 'light',
    primary: { main: '#1f4e8f' },
    secondary: { main: '#0d9488' },
    success: { main: '#16a34a' },
    warning: { main: '#f59e0b' },
    error: { main: '#dc2626' },
    background: { default: '#f5f7fa', paper: '#ffffff' }
  },
  components: {
    MuiCard: { styleOverrides: { root: { boxShadow: '0 1px 3px rgba(15,23,42,0.06)' } } },
    MuiButton: { defaultProps: { disableElevation: true } },
    MuiAppBar: { styleOverrides: { root: { boxShadow: 'none', borderBottom: '1px solid rgba(0,0,0,0.06)' } } },
    MuiCssBaseline: {
      styleOverrides: {
        '*': { scrollbarWidth: 'thin', scrollbarColor: 'rgba(15,23,42,0.08) transparent' },
        '*::-webkit-scrollbar': { width: 8, height: 8 },
        '*::-webkit-scrollbar-track': { background: 'transparent' },
        '*::-webkit-scrollbar-thumb': { backgroundColor: 'rgba(15,23,42,0.08)', borderRadius: 8 },
        '*::-webkit-scrollbar-thumb:hover': { backgroundColor: 'rgba(15,23,42,0.2)' }
      }
    }
  }
});

export const darkTheme: Theme = createTheme({
  ...baseTokens,
  palette: {
    mode: 'dark',
    primary: { main: '#60a5fa' },
    secondary: { main: '#5eead4' },
    success: { main: '#22c55e' },
    warning: { main: '#fbbf24' },
    error: { main: '#f87171' },
    background: { default: '#0f172a', paper: '#1e293b' },
    text: { primary: '#e2e8f0', secondary: '#94a3b8' }
  },
  components: {
    MuiCard: { styleOverrides: { root: { boxShadow: '0 1px 3px rgba(0,0,0,0.3)', backgroundImage: 'none' } } },
    MuiButton: { defaultProps: { disableElevation: true } },
    MuiAppBar: { styleOverrides: { root: { boxShadow: 'none', borderBottom: '1px solid rgba(255,255,255,0.08)', backgroundImage: 'none' } } },
    MuiCssBaseline: {
      styleOverrides: {
        '*': { scrollbarWidth: 'thin', scrollbarColor: 'rgba(148,163,184,0.12) transparent' },
        '*::-webkit-scrollbar': { width: 8, height: 8 },
        '*::-webkit-scrollbar-track': { background: 'transparent' },
        '*::-webkit-scrollbar-thumb': { backgroundColor: 'rgba(148,163,184,0.12)', borderRadius: 8 },
        '*::-webkit-scrollbar-thumb:hover': { backgroundColor: 'rgba(148,163,184,0.25)' }
      }
    }
  }
});

// Retro-compat
export const theme = lightTheme;
