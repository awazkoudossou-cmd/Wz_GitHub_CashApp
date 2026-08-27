import { Alert, Button, Card, CardContent, Grid, Stack, Typography } from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useNavigate, useParams } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatusChip } from '@/components/common/StatusChip';
import { useCashRegister } from '@/modules/cash-registers/hooks';
import { formatDate } from '@/utils/format';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Grid item xs={12} sm={6}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography>{value ?? '—'}</Typography>
    </Grid>
  );
}

export function CashRegisterDetailPage() {
  const navigate = useNavigate();
  const { id } = useParams();
  const { data, isLoading } = useCashRegister(Number(id));

  if (isLoading) return <LoadingScreen />;
  if (!data) return <PageContainer><Alert severity="error">Caisse introuvable.</Alert></PageContainer>;

  return (
    <PageContainer maxWidth="md">
      <PageHeader
        title={`${data.code} — ${data.name}`}
        subtitle="Détail de la caisse"
        actions={
          <Stack direction="row" spacing={1}>
            <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/cash-registers')}>Retour</Button>
            <Button startIcon={<EditIcon />} variant="contained" onClick={() => navigate(`/cash-registers/${data.id}/edit`)}>
              Modifier
            </Button>
          </Stack>
        }
      />
      <Card>
        <CardContent>
          <Grid container spacing={2}>
            <Field label="Code" value={data.code} />
            <Field label="Nom" value={data.name} />
            <Field label="Devise" value={data.currencyCode} />
            <Field label="Statut" value={<StatusChip status={String(data.isActive)} variant="active" />} />
            <Field label="Direction par défaut des opérations" value={data.defaultDirection} />
            <Field label="Moyen de paiement par défaut" value={data.defaultPaymentMethod} />
            <Field label="Compte comptable" value={data.accountingAccountNumber ?? 'Non configuré'} />
            <Field label="Journal comptable" value={data.accountingJournalCode ?? 'Non configuré'} />
            <Grid item xs={12}>
              <Typography variant="caption" color="text.secondary">Description</Typography>
              <Typography>{data.description ?? '—'}</Typography>
            </Grid>
            <Field label="Créée le" value={formatDate(data.createdAt, true)} />
            <Field label="Modifiée le" value={formatDate(data.updatedAt, true)} />
          </Grid>
        </CardContent>
      </Card>
    </PageContainer>
  );
}
