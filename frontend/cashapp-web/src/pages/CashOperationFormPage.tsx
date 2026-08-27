import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Autocomplete,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Grid,
  InputAdornment,
  MenuItem,
  Stack,
  TextField
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { z } from 'zod';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { CategoryPickerDialog } from '@/components/dialogs/CategoryPickerDialog';
import {
  useCashOperation,
  useCreateCashOperation,
  useUpdateCashOperation
} from '@/modules/cash-operations/hooks';
import { useCategories } from '@/modules/categories/hooks';
import { useCashSessions } from '@/modules/cash-sessions/hooks';
import { useCashRegisters } from '@/modules/cash-registers/hooks';
import { useThirdParties } from '@/modules/third-parties/hooks';
import { useAuthStore } from '@/app/store/authStore';
import { OperationDirection, PaymentMethod } from '@/types/enums';
import { extractErrorMessage } from '@/api/client';

const schema = z.object({
  cashSessionId: z.coerce.number().int().positive(),
  operationDate: z.string().min(1),
  direction: z.enum([OperationDirection.IN, OperationDirection.OUT]),
  categoryId: z.coerce.number().int().positive(),
  amount: z.coerce.number().positive('Montant > 0'),
  paymentMethod: z.enum([
    PaymentMethod.CASH,
    PaymentMethod.MOBILE_MONEY,
    PaymentMethod.BANK_TRANSFER,
    PaymentMethod.CHECK
  ]),
  label: z.string().min(1).max(200),
  description: z.string().max(1000).optional(),
  externalReference: z.string().max(100).optional(),
  thirdPartyName: z.string().max(200).optional()
});

type FormValues = z.infer<typeof schema>;

export function CashOperationFormPage() {
  const navigate = useNavigate();
  const { id } = useParams();
  const [search] = useSearchParams();
  const isEdit = !!id;
  const opId = id ? Number(id) : undefined;
  // Pas de filtre par caisse ici : on liste TOUTES les sessions accessibles
  // pour que les sessions ouvertes apparaissent même si la caisse courante du topbar
  // n'a pas de session en cours.
  const detail = useCashOperation(opId);
  const sessions = useCashSessions(undefined);
  const registers = useCashRegisters();
  const categories = useCategories();
  const thirdParties = useThirdParties();
  const createMut = useCreateCashOperation();
  const updateMut = useUpdateCashOperation();
  const [categoryPickerOpen, setCategoryPickerOpen] = useState(false);

  const openSessions = useMemo(() => (sessions.data ?? []).filter((s) => s.status === 'OPEN'), [sessions.data]);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      cashSessionId: Number(search.get('sessionId')) || openSessions[0]?.id || 0,
      operationDate: new Date().toISOString().slice(0, 10),
      direction: OperationDirection.OUT,
      categoryId: 0,
      amount: 0,
      paymentMethod: PaymentMethod.CASH,
      label: ''
    }
  });

  // Quand les sessions arrivent (ou changent), si aucun sessionId n'est encore choisi,
  // pré-remplir avec la première session ouverte.
  useEffect(() => {
    if (isEdit) return;
    if (form.getValues('cashSessionId') > 0) return;
    if (openSessions.length === 0) return;
    const preferred = Number(search.get('sessionId')) || openSessions[0].id;
    form.setValue('cashSessionId', preferred);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [openSessions, isEdit]);

  const selectedSessionId = form.watch('cashSessionId');
  // Pré-remplit Direction et Moyen de paiement avec les valeurs par défaut de la caisse,
  // à chaque changement de session (uniquement en création — pas en édition).
  useEffect(() => {
    if (isEdit) return;
    if (!selectedSessionId) return;
    const session = openSessions.find((s) => s.id === selectedSessionId);
    if (!session) return;
    const register = (registers.data ?? []).find((r) => r.id === session.cashRegisterId);
    if (!register) return;
    form.setValue('direction', register.defaultDirection);
    form.setValue('paymentMethod', register.defaultPaymentMethod);
    form.setValue('categoryId', 0);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedSessionId, registers.data, isEdit]);

  useEffect(() => {
    if (isEdit && detail.data) {
      form.reset({
        cashSessionId: detail.data.cashSessionId,
        operationDate: detail.data.operationDate.slice(0, 10),
        direction: detail.data.direction,
        categoryId: detail.data.categoryId,
        amount: detail.data.amount,
        paymentMethod: detail.data.paymentMethod,
        label: detail.data.label,
        description: detail.data.description ?? '',
        externalReference: detail.data.externalReference ?? '',
        thirdPartyName: detail.data.thirdPartyName ?? ''
      });
    }
  }, [isEdit, detail.data, form]);

  if (isEdit && detail.isLoading) return <LoadingScreen />;
  if (!isEdit && sessions.isLoading) return <LoadingScreen />;

  // Aucune session ouverte → popup et impossibilité de saisir.
  if (!isEdit && sessions.isFetched && openSessions.length === 0) {
    return (
      <PageContainer maxWidth="sm">
        <PageHeader title="Nouvelle opération" />
        <Dialog open onClose={() => navigate('/cash-sessions')} maxWidth="xs" fullWidth>
          <DialogTitle>Aucune session ouverte</DialogTitle>
          <DialogContent>
            <DialogContentText>
              Aucune session n'est actuellement ouverte sur les caisses auxquelles tu es affecté.
              Tu dois <b>ouvrir une session</b> avant de pouvoir saisir une opération.
            </DialogContentText>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => navigate(-1)}>Retour</Button>
            <Button variant="contained" onClick={() => navigate('/cash-sessions')}>
              Ouvrir une session
            </Button>
          </DialogActions>
        </Dialog>
      </PageContainer>
    );
  }

  const direction = form.watch('direction');
  const filteredCategories = (categories.data ?? []).filter((c) => c.isActive && c.direction === direction);

  const onSubmit = form.handleSubmit(async (values) => {
    if (isEdit) {
      await updateMut.mutateAsync({
        id: opId!,
        payload: {
          operationDate: values.operationDate,
          categoryId: values.categoryId,
          amount: values.amount,
          paymentMethod: values.paymentMethod,
          label: values.label,
          description: values.description,
          externalReference: values.externalReference,
          thirdPartyName: values.thirdPartyName
        }
      });
    } else {
      await createMut.mutateAsync({
        cashSessionId: values.cashSessionId,
        operationDate: values.operationDate,
        direction: values.direction,
        categoryId: values.categoryId,
        amount: values.amount,
        paymentMethod: values.paymentMethod,
        label: values.label,
        description: values.description,
        externalReference: values.externalReference,
        thirdPartyName: values.thirdPartyName
      });
    }
    navigate('/cash-operations');
  });

  return (
    <PageContainer maxWidth="md">
      <PageHeader title={isEdit ? "Modifier l'opération" : 'Nouvelle opération'} />
      <Card>
        <CardContent>
          <form onSubmit={onSubmit}>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <TextField select fullWidth size="small" label="Session"
                  value={form.watch('cashSessionId')}
                  onChange={(e) => form.setValue('cashSessionId', Number(e.target.value))}
                  disabled={isEdit}
                >
                  {openSessions.map((s) => (
                    <MenuItem key={s.id} value={s.id}>
                      #{s.id} — {s.cashRegisterCode}
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>
              <Grid item xs={12} sm={3}>
                <TextField type="date" fullWidth size="small" label="Date"
                  InputLabelProps={{ shrink: true }}
                  {...form.register('operationDate')}
                />
              </Grid>
              <Grid item xs={12} sm={3}>
                <TextField select fullWidth size="small" label="Direction"
                  value={direction}
                  onChange={(e) => { form.setValue('direction', e.target.value as never); form.setValue('categoryId', 0); }}
                  disabled={isEdit}
                >
                  <MenuItem value={OperationDirection.IN}>IN (entrée)</MenuItem>
                  <MenuItem value={OperationDirection.OUT}>OUT (sortie)</MenuItem>
                </TextField>
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth size="small" label="Catégorie"
                  value={filteredCategories.find((c) => c.id === form.watch('categoryId'))?.label ?? ''}
                  placeholder="Cliquer pour choisir…"
                  onClick={() => setCategoryPickerOpen(true)}
                  InputProps={{
                    readOnly: true,
                    endAdornment: <InputAdornment position="end"><SearchIcon fontSize="small" color="action" /></InputAdornment>
                  }}
                  sx={{ cursor: 'pointer', '& .MuiInputBase-input': { cursor: 'pointer' } }}
                />
              </Grid>
              <Grid item xs={12} sm={3}>
                <TextField type="number" fullWidth size="small" label="Montant"
                  inputProps={{ step: '0.01', min: 0 }}
                  {...form.register('amount')}
                  error={!!form.formState.errors.amount}
                  helperText={form.formState.errors.amount?.message}
                />
              </Grid>
              <Grid item xs={12} sm={3}>
                <TextField select fullWidth size="small" label="Moyen de paiement"
                  value={form.watch('paymentMethod')}
                  onChange={(e) => form.setValue('paymentMethod', e.target.value as never)}
                >
                  {Object.values(PaymentMethod).map((p) => (
                    <MenuItem key={p} value={p}>{p}</MenuItem>
                  ))}
                </TextField>
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label="Libellé" {...form.register('label')}
                  error={!!form.formState.errors.label} helperText={form.formState.errors.label?.message}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <Autocomplete
                  freeSolo
                  fullWidth
                  size="small"
                  options={(thirdParties.data ?? []).map((t) => t.name)}
                  value={form.watch('thirdPartyName') ?? ''}
                  onInputChange={(_, value) => form.setValue('thirdPartyName', value)}
                  renderInput={(params) => (
                    <TextField {...params} label="Tiers (optionnel)" helperText="Sélectionne un tiers existant ou saisis-en un nouveau." />
                  )}
                />
              </Grid>
              <Grid item xs={12} sm={6}><TextField fullWidth size="small" label="Référence externe" {...form.register('externalReference')} /></Grid>
              <Grid item xs={12}><TextField fullWidth size="small" label="Description" multiline rows={2} {...form.register('description')} /></Grid>
              {(createMut.isError || updateMut.isError) && (
                <Grid item xs={12}><Alert severity="error">{extractErrorMessage((createMut.error ?? updateMut.error) as unknown)}</Alert></Grid>
              )}
              <Grid item xs={12}>
                <Stack direction="row" spacing={2}>
                  <Button type="submit" variant="contained">Enregistrer</Button>
                  <Button onClick={() => navigate(-1)}>Annuler</Button>
                </Stack>
              </Grid>
            </Grid>
          </form>
        </CardContent>
      </Card>

      <CategoryPickerDialog
        open={categoryPickerOpen}
        onClose={() => setCategoryPickerOpen(false)}
        categories={filteredCategories}
        onSelect={(c) => form.setValue('categoryId', c.id)}
      />
    </PageContainer>
  );
}
