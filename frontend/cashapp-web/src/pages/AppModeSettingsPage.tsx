import { Alert, Button, Card, CardContent, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { useAppMode, useUpdateAppMode } from '@/modules/settings/hooks';
import { AppMode } from '@/types/enums';
import { extractErrorMessage } from '@/api/client';

export function AppModeSettingsPage() {
  const query = useAppMode();
  const update = useUpdateAppMode();
  const [mode, setMode] = useState<AppMode>(AppMode.ESSENTIAL);

  useEffect(() => {
    if (query.data) setMode(query.data.mode);
  }, [query.data]);

  if (query.isLoading) return <LoadingScreen />;

  return (
    <PageContainer maxWidth="sm">
      <PageHeader title="Mode de l'application" subtitle="Pilote les modules disponibles globalement" />
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary">
              <b>ESSENTIAL</b> : caisse standard (V1).<br />
              <b>INTERMEDIATE</b> : ajoute les validations / écarts.<br />
              <b>ADVANCED</b> : ouvre les modules V2 (transferts, banque, imports, rapprochement…).
            </Typography>
            <TextField select size="small" label="Mode" value={mode} onChange={(e) => setMode(e.target.value as AppMode)}>
              {Object.values(AppMode).map((m) => <MenuItem key={m} value={m}>{m}</MenuItem>)}
            </TextField>
            {update.isError && <Alert severity="error">{extractErrorMessage(update.error)}</Alert>}
            {update.isSuccess && <Alert severity="success">Mode mis à jour.</Alert>}
            <Stack direction="row" spacing={2}>
              <Button variant="contained" onClick={() => update.mutate(mode)} disabled={update.isPending}>Appliquer</Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </PageContainer>
  );
}
