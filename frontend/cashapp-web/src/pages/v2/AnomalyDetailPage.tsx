import { useState } from 'react';
import { Alert, Box, Button, Card, CardContent, Grid, Stack, TextField, Typography } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import AssignmentIndIcon from '@mui/icons-material/AssignmentInd';
import DoneAllIcon from '@mui/icons-material/DoneAll';
import SendIcon from '@mui/icons-material/Send';
import { useNavigate, useParams } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatusBadge } from '@/components/common/StatusBadge';
import { AttachmentPanel } from '@/components/common/AttachmentPanel';
import {
  useAddAnomalyComment,
  useAnomaly,
  useAssignAnomaly,
  useResolveAnomaly
} from '@/modules/anomalies-v2/hooks';
import { useUsers } from '@/modules/users/hooks';
import { AnomalyStatus } from '@/types/v2Enums';
import { formatDate } from '@/utils/format';
import { extractErrorMessage } from '@/api/client';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Grid item xs={12} sm={6} md={4}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography>{value ?? '—'}</Typography>
    </Grid>
  );
}

export function AnomalyDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const aId = Number(id);
  const { data, isLoading } = useAnomaly(aId);
  const users = useUsers();
  const assign = useAssignAnomaly();
  const resolve = useResolveAnomaly();
  const addComment = useAddAnomalyComment();
  const [comment, setComment] = useState('');
  const [resolution, setResolution] = useState('');

  if (isLoading) return <LoadingScreen />;
  if (!data) return <PageContainer><Alert severity="error">Anomalie introuvable.</Alert></PageContainer>;

  const closed = data.status === AnomalyStatus.RESOLVED || data.status === AnomalyStatus.CLOSED;

  const onAssign = async () => {
    const userIdStr = window.prompt(`Assigner à quel utilisateur ?\nIDs disponibles :\n${(users.data ?? []).map((u) => `#${u.id} ${u.fullName} (${u.roleCode})`).join('\n')}`);
    if (!userIdStr) return;
    const userId = Number(userIdStr);
    if (!userId) return;
    try { await assign.mutateAsync({ id: aId, userId }); }
    catch (e) { alert(extractErrorMessage(e)); }
  };

  return (
    <PageContainer maxWidth="md">
      <PageHeader
        title={`Anomalie ${data.reference}`}
        subtitle={data.title}
        actions={
          <Stack direction="row" spacing={1}>
            <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/anomalies')}>Retour</Button>
            <Button variant="outlined" startIcon={<AssignmentIndIcon />} disabled={closed} onClick={onAssign}>Assigner</Button>
          </Stack>
        }
      />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Field label="Statut" value={<StatusBadge value={data.status} />} />
            <Field label="Sévérité" value={<StatusBadge value={data.severity} />} />
            <Field label="Caisse" value={data.cashRegisterCode} />
            <Field label="Détectée le" value={formatDate(data.detectedAt, true)} />
            <Field label="Détectée par" value={data.detectedByName ?? 'Système'} />
            <Field label="Assignée à" value={data.assignedToName ?? '—'} />
            <Field label="Entité liée" value={data.relatedEntityType ? `${data.relatedEntityType} #${data.relatedEntityId}` : '—'} />
            <Field label="Résolue le" value={formatDate(data.resolvedAt, true)} />
            <Field label="Résolue par" value={data.resolvedByName} />
            {data.description && (
              <Grid item xs={12}>
                <Typography variant="caption" color="text.secondary">Description</Typography>
                <Typography>{data.description}</Typography>
              </Grid>
            )}
            {data.resolutionComment && (
              <Grid item xs={12}>
                <Typography variant="caption" color="text.secondary">Commentaire de résolution</Typography>
                <Typography>{data.resolutionComment}</Typography>
              </Grid>
            )}
          </Grid>
        </CardContent>
      </Card>

      {!closed && (
        <Card sx={{ mb: 2 }}>
          <CardContent>
            <Box mb={1}><b>Résoudre</b></Box>
            <Stack spacing={1.5}>
              <TextField multiline rows={2} size="small" fullWidth label="Commentaire de résolution"
                value={resolution} onChange={(e) => setResolution(e.target.value)} />
              <Stack direction="row">
                <Button variant="contained" color="success" startIcon={<DoneAllIcon />} disabled={resolve.isPending || resolution.trim().length === 0}
                  onClick={async () => {
                    try { await resolve.mutateAsync({ id: aId, comment: resolution.trim() }); setResolution(''); }
                    catch (e) { alert(extractErrorMessage(e)); }
                  }}>Marquer résolue</Button>
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      )}

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Box mb={1}><b>Commentaires</b></Box>
          {data.comments.length === 0 && <Typography variant="body2" color="text.secondary">Aucun commentaire.</Typography>}
          <Stack spacing={1.5}>
            {data.comments.map((c) => (
              <Box key={c.id}>
                <Typography variant="body2"><b>{c.authorName}</b> — {formatDate(c.createdAt, true)}</Typography>
                <Typography>{c.body}</Typography>
              </Box>
            ))}
          </Stack>
          <Stack direction="row" spacing={1} mt={2}>
            <TextField fullWidth size="small" placeholder="Ajouter un commentaire…" value={comment} onChange={(e) => setComment(e.target.value)} />
            <Button startIcon={<SendIcon />} variant="contained" disabled={addComment.isPending || !comment.trim()}
              onClick={async () => {
                try { await addComment.mutateAsync({ id: aId, body: comment.trim() }); setComment(''); }
                catch (e) { alert(extractErrorMessage(e)); }
              }}>Envoyer</Button>
          </Stack>
        </CardContent>
      </Card>

      <AttachmentPanel entityType="AnomalyCase" entityId={data.id} />
    </PageContainer>
  );
}
