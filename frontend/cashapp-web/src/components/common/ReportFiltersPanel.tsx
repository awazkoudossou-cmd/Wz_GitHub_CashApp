import { Button, Card, CardContent, Grid, Stack, TextField, MenuItem } from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import { useAuthStore } from '@/app/store/authStore';
import type { ReactNode } from 'react';

interface Props {
  from: string;
  to: string;
  cashRegisterId?: number;
  onFromChange: (v: string) => void;
  onToChange: (v: string) => void;
  onCashRegisterChange?: (v: number | undefined) => void;
  showCashRegister?: boolean;
  onApply: () => void;
  extras?: ReactNode;
}

export function ReportFiltersPanel({
  from,
  to,
  cashRegisterId,
  onFromChange,
  onToChange,
  onCashRegisterChange,
  showCashRegister = true,
  onApply,
  extras
}: Props) {
  const registers = useAuthStore((s) => s.cashRegisters);
  return (
    <Card sx={{ mb: 2 }}>
      <CardContent>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} sm={3}>
            <TextField fullWidth size="small" type="date" label="Du" InputLabelProps={{ shrink: true }}
              value={from} onChange={(e) => onFromChange(e.target.value)} />
          </Grid>
          <Grid item xs={12} sm={3}>
            <TextField fullWidth size="small" type="date" label="Au" InputLabelProps={{ shrink: true }}
              value={to} onChange={(e) => onToChange(e.target.value)} />
          </Grid>
          {showCashRegister && (
            <Grid item xs={12} sm={3}>
              <TextField select fullWidth size="small" label="Caisse"
                value={cashRegisterId ?? ''}
                onChange={(e) => onCashRegisterChange?.(e.target.value ? Number(e.target.value) : undefined)}>
                <MenuItem value="">Toutes</MenuItem>
                {registers.map((r) => (
                  <MenuItem key={r.id} value={r.id}>{r.code} — {r.name}</MenuItem>
                ))}
              </TextField>
            </Grid>
          )}
          {extras}
          <Grid item xs={12} sm="auto">
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" startIcon={<SearchIcon />} onClick={onApply}>
                Appliquer
              </Button>
            </Stack>
          </Grid>
        </Grid>
      </CardContent>
    </Card>
  );
}
