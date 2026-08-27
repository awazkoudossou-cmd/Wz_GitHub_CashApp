import { Alert, Button, Card, CardContent, Chip, Grid, Stack, Typography } from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import KeyIcon from '@mui/icons-material/Key';
import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatusChip } from '@/components/common/StatusChip';
import { useResetUserPassword, useUser } from '@/modules/users/hooks';
import { useCashRegisters } from '@/modules/cash-registers/hooks';
import { formatDate } from '@/utils/format';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Grid item xs={12} sm={6}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography>{value ?? '—'}</Typography>
    </Grid>
  );
}

export function UserDetailPage() {
  const navigate = useNavigate();
  const { id } = useParams();
  const userId = Number(id);
  const { data, isLoading } = useUser(userId);
  const registers = useCashRegisters();
  const resetPwd = useResetUserPassword();
  const [resetting, setResetting] = useState(false);

  if (isLoading) return <LoadingScreen />;
  if (!data) return <PageContainer><Alert severity="error">Utilisateur introuvable.</Alert></PageContainer>;

  const assignedRegisters = (registers.data ?? []).filter((r) => data.cashRegisterIds.includes(r.id));

  const onReset = async () => {
    const pwd = window.prompt('Nouveau mot de passe (8 caractères min) :');
    if (!pwd || pwd.length < 8) {
      if (pwd) alert('Mot de passe trop court.');
      return;
    }
    setResetting(true);
    try {
      await resetPwd.mutateAsync({ id: data.id, newPassword: pwd });
      alert('Mot de passe réinitialisé.');
    } finally {
      setResetting(false);
    }
  };

  return (
    <PageContainer maxWidth="md">
      <PageHeader
        title={data.fullName}
        subtitle={`@${data.username}`}
        actions={
          <Stack direction="row" spacing={1}>
            <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/users')}>Retour</Button>
            <Button startIcon={<KeyIcon />} variant="outlined" onClick={onReset} disabled={resetting}>
              Réinitialiser le mot de passe
            </Button>
            <Button startIcon={<EditIcon />} variant="contained" onClick={() => navigate(`/users/${data.id}/edit`)}>
              Modifier
            </Button>
          </Stack>
        }
      />
      <Card>
        <CardContent>
          <Grid container spacing={2}>
            <Field label="Username" value={data.username} />
            <Field label="Nom complet" value={data.fullName} />
            <Field label="Rôle" value={data.roleCode} />
            <Field label="Statut" value={<StatusChip status={String(data.isActive)} variant="active" />} />
            <Grid item xs={12}>
              <Typography variant="caption" color="text.secondary">Caisses affectées</Typography>
              <Stack direction="row" spacing={1} flexWrap="wrap" mt={0.5}>
                {assignedRegisters.length === 0 && <Typography color="text.secondary">—</Typography>}
                {assignedRegisters.map((r) => (
                  <Chip key={r.id} label={`${r.code} — ${r.name}`} size="small" />
                ))}
              </Stack>
            </Grid>
            <Field label="Créé le" value={formatDate(data.createdAt, true)} />
            <Field label="Modifié le" value={formatDate(data.updatedAt, true)} />
          </Grid>
        </CardContent>
      </Card>
    </PageContainer>
  );
}
