import { Button, Card, CardContent, Grid, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { useState } from 'react';
import DownloadIcon from '@mui/icons-material/Download';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { useExportOperations, useExportSessions } from '@/modules/exports/hooks';
import { useAuthStore } from '@/app/store/authStore';

type Fmt = 'xlsx' | 'pdf';

export function ExportsPage() {
  const registers = useAuthStore((s) => s.cashRegisters);
  const [from, setFrom] = useState(new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().slice(0, 10));
  const [to, setTo] = useState(new Date().toISOString().slice(0, 10));
  const [cashRegisterId, setCashRegisterId] = useState<number | ''>('');
  const [opFmt, setOpFmt] = useState<Fmt>('xlsx');
  const [sessionFmt, setSessionFmt] = useState<Fmt>('xlsx');

  const exportOps = useExportOperations();
  const exportSessions = useExportSessions();

  return (
    <PageContainer maxWidth="md">
      <PageHeader title="Exports" subtitle="Exportez vos opérations et sessions en Excel ou PDF" />
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={3}><TextField fullWidth size="small" type="date" label="Du" InputLabelProps={{ shrink: true }} value={from} onChange={(e) => setFrom(e.target.value)} /></Grid>
            <Grid item xs={12} sm={3}><TextField fullWidth size="small" type="date" label="Au" InputLabelProps={{ shrink: true }} value={to} onChange={(e) => setTo(e.target.value)} /></Grid>
            <Grid item xs={12} sm={6}>
              <TextField select fullWidth size="small" label="Caisse (optionnel)" value={cashRegisterId} onChange={(e) => setCashRegisterId(e.target.value ? Number(e.target.value) : '')}>
                <MenuItem value="">Toutes</MenuItem>
                {registers.map((r) => <MenuItem key={r.id} value={r.id}>{r.code} — {r.name}</MenuItem>)}
              </TextField>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Grid container spacing={2}>
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6">Opérations</Typography>
              <Typography variant="body2" color="text.secondary" mb={2}>Journal complet de la période.</Typography>
              <Stack direction="row" spacing={1}>
                <TextField select size="small" label="Format" value={opFmt} onChange={(e) => setOpFmt(e.target.value as Fmt)} sx={{ minWidth: 120 }}>
                  <MenuItem value="xlsx">Excel</MenuItem>
                  <MenuItem value="pdf">PDF</MenuItem>
                </TextField>
                <Button variant="contained" startIcon={<DownloadIcon />} onClick={() => exportOps.mutate({ from, to, cashRegisterId: cashRegisterId || undefined, format: opFmt })}>
                  Télécharger
                </Button>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6">Sessions</Typography>
              <Typography variant="body2" color="text.secondary" mb={2}>Sessions ouvertes sur la période.</Typography>
              <Stack direction="row" spacing={1}>
                <TextField select size="small" label="Format" value={sessionFmt} onChange={(e) => setSessionFmt(e.target.value as Fmt)} sx={{ minWidth: 120 }}>
                  <MenuItem value="xlsx">Excel</MenuItem>
                  <MenuItem value="pdf">PDF</MenuItem>
                </TextField>
                <Button variant="contained" startIcon={<DownloadIcon />} onClick={() => exportSessions.mutate({ from, to, cashRegisterId: cashRegisterId || undefined, format: sessionFmt })}>
                  Télécharger
                </Button>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </PageContainer>
  );
}
