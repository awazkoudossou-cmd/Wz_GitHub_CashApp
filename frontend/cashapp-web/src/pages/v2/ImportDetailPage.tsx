import { Alert, Box, Button, Card, CardContent, Checkbox, FormControlLabel, Grid, Stack, Typography } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import PreviewIcon from '@mui/icons-material/Preview';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import { useNavigate, useParams } from 'react-router-dom';
import { useState } from 'react';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatusBadge } from '@/components/common/StatusBadge';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { useConfirmImport, useImport, usePreviewImport } from '@/modules/imports-v2/hooks';
import { ImportBatchStatus, ImportLineStatus } from '@/types/v2Enums';
import { formatDate } from '@/utils/format';
import { extractErrorMessage } from '@/api/client';
import type { ImportPreviewLine } from '@/types';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Grid item xs={12} sm={6} md={3}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography>{value ?? '—'}</Typography>
    </Grid>
  );
}

export function ImportDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const bId = Number(id);
  const { data, isLoading } = useImport(bId);
  const preview = usePreviewImport();
  const confirm = useConfirmImport();
  const [allowPartial, setAllowPartial] = useState(true);

  if (isLoading) return <LoadingScreen />;
  if (!data) return <PageContainer><Alert severity="error">Import introuvable.</Alert></PageContainer>;

  const canPreview = data.status === ImportBatchStatus.UPLOADED || data.status === ImportBatchStatus.PREVIEWED;
  const canConfirm = data.status === ImportBatchStatus.PREVIEWED || data.status === ImportBatchStatus.PARTIAL;

  const cols: Column<ImportPreviewLine>[] = [
    { key: 'n', header: 'Ligne', align: 'right', render: (r) => r.lineNumber },
    { key: 'status', header: 'Statut', render: (r) => <StatusBadge value={r.status} /> },
    { key: 'err', header: 'Erreur', render: (r) => r.errorMessage ?? '—' },
    { key: 'data', header: 'Données lues', render: (r) => <code style={{ fontSize: 11 }}>{r.rawDataJson}</code> }
  ];

  return (
    <PageContainer>
      <PageHeader
        title={`Import ${data.batchRef}`}
        subtitle={`${data.batchType} — ${data.originalFileName}`}
        actions={<Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/imports')}>Retour</Button>}
      />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Field label="Statut" value={<StatusBadge value={data.status} />} />
            <Field label="Téléversé le" value={formatDate(data.uploadedAt, true)} />
            <Field label="Par" value={data.uploadedByName} />
            <Field label="Caisse cible" value={data.cashRegisterCode ?? '—'} />
            <Field label="Total" value={data.totalLines} />
            <Field label="Valides" value={data.validLines} />
            <Field label="Invalides" value={data.invalidLines} />
            <Field label="Importées" value={data.importedLines} />
          </Grid>
          <Stack direction="row" spacing={1} mt={2}>
            <Button variant="contained" startIcon={<PreviewIcon />} disabled={!canPreview || preview.isPending}
              onClick={async () => {
                try { await preview.mutateAsync(bId); }
                catch (e) { alert(extractErrorMessage(e)); }
              }}>{preview.isPending ? 'Analyse…' : 'Analyser le fichier'}</Button>
            <FormControlLabel
              control={<Checkbox checked={allowPartial} onChange={(_, v) => setAllowPartial(v)} />}
              label="Autoriser import partiel"
            />
            <Button variant="contained" color="success" startIcon={<CheckCircleIcon />} disabled={!canConfirm || confirm.isPending}
              onClick={async () => {
                try { await confirm.mutateAsync({ id: bId, allowPartialSuccess: allowPartial }); }
                catch (e) { alert(extractErrorMessage(e)); }
              }}>Confirmer l'import</Button>
          </Stack>
        </CardContent>
      </Card>

      {data.errorSummaryJson && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          <pre style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{data.errorSummaryJson}</pre>
        </Alert>
      )}

      <Box mb={1}><b>Lignes du fichier</b></Box>
      <AppTable
        columns={cols} rows={data.lines} rowKey={(r) => r.lineNumber}
        maxHeight="calc(100vh - 460px)"
      />
    </PageContainer>
  );
}
