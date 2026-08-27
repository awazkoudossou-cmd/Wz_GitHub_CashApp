import { Alert, Box, Button, Card, CardContent, Divider, Stack, Switch, Typography } from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { useAppMode, useFeatureSettings, useUpdateFeatureSettings } from '@/modules/settings/hooks';
import { extractErrorMessage } from '@/api/client';
import { AppMode, FeatureCodes } from '@/types/enums';

// Modules masqués partout dans l'UI.
const HIDDEN_FEATURES = new Set<string>([FeatureCodes.ADV_ANOMALIES]);

// Mapping mode -> features V2 autorisées. Tous les CORE_* sont toujours disponibles.
const ADV_BY_MODE: Record<string, string[]> = {
  [AppMode.ESSENTIAL]: [],
  [AppMode.INTERMEDIATE]: [
    FeatureCodes.ADV_VALIDATION,
    FeatureCodes.ADV_VARIANCE_MANAGEMENT,
    FeatureCodes.ADV_AUDIT_LOG
  ],
  [AppMode.ADVANCED]: [
    FeatureCodes.ADV_VALIDATION,
    FeatureCodes.ADV_TRANSFERS,
    FeatureCodes.ADV_BANK_DEPOSITS,
    FeatureCodes.ADV_ATTACHMENTS,
    FeatureCodes.ADV_ADVANCED_REPORTS,
    FeatureCodes.ADV_IMPORTS,
    FeatureCodes.ADV_RECONCILIATION,
    FeatureCodes.ADV_VARIANCE_MANAGEMENT,
    FeatureCodes.ADV_AUDIT_LOG,
    FeatureCodes.ADV_ACCOUNTING
    // ADV_ANOMALIES intentionnellement absent (caché)
  ]
};

export function FeatureSettingsPage() {
  const query = useFeatureSettings();
  const update = useUpdateFeatureSettings();
  const appMode = useAppMode();
  const [toggles, setToggles] = useState<Record<string, boolean>>({});

  useEffect(() => {
    if (query.data) {
      setToggles(Object.fromEntries(query.data.map((f) => [f.featureCode, f.isEnabled])));
    }
  }, [query.data]);

  const allowedAdvSet = useMemo(() => {
    const mode = appMode.data?.mode ?? AppMode.ESSENTIAL;
    return new Set(ADV_BY_MODE[mode] ?? []);
  }, [appMode.data]);

  if (query.isLoading || appMode.isLoading) return <LoadingScreen />;

  const core = (query.data ?? []).filter(
    (f) => f.featureCode.startsWith('CORE_') && !HIDDEN_FEATURES.has(f.featureCode)
  );
  const adv = (query.data ?? []).filter(
    (f) => f.featureCode.startsWith('ADV_') && !HIDDEN_FEATURES.has(f.featureCode)
  );
  const advVisible = adv.filter((f) => allowedAdvSet.has(f.featureCode));
  const advUnavailable = adv.filter((f) => !allowedAdvSet.has(f.featureCode));

  const save = () => {
    // Sécurité : on force à false les features non disponibles dans le mode actuel.
    const finalToggles = { ...toggles };
    for (const f of advUnavailable) finalToggles[f.featureCode] = false;
    for (const code of HIDDEN_FEATURES) finalToggles[code] = false;

    update.mutate({
      features: Object.entries(finalToggles).map(([featureCode, isEnabled]) => ({ featureCode, isEnabled }))
    });
  };

  return (
    <PageContainer maxWidth="md">
      <PageHeader
        title="Modules"
        subtitle={`Mode applicatif courant : ${appMode.data?.mode ?? '—'}. Seuls les modules compatibles sont activables.`}
      />
      <Card>
        <CardContent>
          <Typography variant="overline" color="text.secondary">Modules V1 (Core)</Typography>
          <Stack divider={<Divider />}>
            {core.map((f) => (
              <Stack key={f.featureCode} direction="row" alignItems="center" justifyContent="space-between" py={1}>
                <div>
                  <Typography>{f.featureName}</Typography>
                  <Typography variant="caption" color="text.secondary">{f.featureCode}</Typography>
                </div>
                <Switch
                  checked={!!toggles[f.featureCode]}
                  onChange={(_, v) => setToggles((t) => ({ ...t, [f.featureCode]: v }))}
                />
              </Stack>
            ))}
          </Stack>

          <Typography variant="overline" color="text.secondary" mt={3} display="block">
            Modules V2 (Avancé)
          </Typography>

          {advVisible.length === 0 && (
            <Alert severity="info" sx={{ mt: 1 }}>
              Aucun module avancé disponible dans le mode <b>{appMode.data?.mode}</b>.
              Passe en mode <b>INTERMEDIATE</b> ou <b>ADVANCED</b> dans « Mode de l'application » pour en débloquer.
            </Alert>
          )}

          <Stack divider={<Divider />}>
            {advVisible.map((f) => (
              <Stack key={f.featureCode} direction="row" alignItems="center" justifyContent="space-between" py={1}>
                <div>
                  <Typography>{f.featureName}</Typography>
                  <Typography variant="caption" color="text.secondary">{f.featureCode}</Typography>
                </div>
                <Switch
                  checked={!!toggles[f.featureCode]}
                  onChange={(_, v) => setToggles((t) => ({ ...t, [f.featureCode]: v }))}
                />
              </Stack>
            ))}
          </Stack>

          {advUnavailable.length > 0 && (
            <Box mt={2}>
              <Typography variant="caption" color="text.secondary">
                Modules non disponibles dans le mode courant (seront forcés à OFF à l'enregistrement) :
              </Typography>
              <Typography variant="caption" color="text.secondary" display="block">
                {advUnavailable.map((f) => f.featureName).join(', ')}
              </Typography>
            </Box>
          )}

          {update.isError && <Alert severity="error" sx={{ mt: 2 }}>{extractErrorMessage(update.error)}</Alert>}
          {update.isSuccess && <Alert severity="success" sx={{ mt: 2 }}>Modules mis à jour. Rafraîchissez la page pour voir les changements de menu.</Alert>}

          <Stack direction="row" spacing={2} mt={2}>
            <Button variant="contained" onClick={save} disabled={update.isPending}>Enregistrer</Button>
          </Stack>
        </CardContent>
      </Card>
    </PageContainer>
  );
}
