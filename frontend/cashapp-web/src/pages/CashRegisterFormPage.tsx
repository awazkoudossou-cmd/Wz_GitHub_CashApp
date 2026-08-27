import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Button, Card, CardContent, Grid, MenuItem, Stack, TextField } from '@mui/material';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import { z } from 'zod';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import {
  useCashRegister,
  useCreateCashRegister,
  useUpdateCashRegister
} from '@/modules/cash-registers/hooks';
import { extractErrorMessage } from '@/api/client';
import { OperationDirection, PaymentMethod } from '@/types/enums';

const baseSchema = {
  name: z.string().min(2),
  description: z.string().max(500).optional(),
  currencyCode: z.string().min(3).max(8),
  defaultDirection: z.enum([OperationDirection.IN, OperationDirection.OUT]),
  defaultPaymentMethod: z.enum([
    PaymentMethod.CASH, PaymentMethod.MOBILE_MONEY, PaymentMethod.BANK_TRANSFER, PaymentMethod.CHECK
  ])
};

const createSchema = z.object({ ...baseSchema, code: z.string().min(2).max(40) });
const updateSchema = z.object(baseSchema);

type CreateValues = z.infer<typeof createSchema>;
type UpdateValues = z.infer<typeof updateSchema>;

export function CashRegisterFormPage() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEdit = !!id;
  const registerId = id ? Number(id) : undefined;

  const detail = useCashRegister(registerId);
  const createMut = useCreateCashRegister();
  const updateMut = useUpdateCashRegister();

  const form = useForm<CreateValues | UpdateValues>({
    resolver: zodResolver(isEdit ? updateSchema : createSchema),
    defaultValues: {
      name: '', description: '', currencyCode: 'XOF',
      defaultDirection: OperationDirection.IN,
      defaultPaymentMethod: PaymentMethod.CASH
    } as CreateValues
  });

  useEffect(() => {
    if (isEdit && detail.data) {
      form.reset({
        name: detail.data.name,
        description: detail.data.description ?? '',
        currencyCode: detail.data.currencyCode,
        defaultDirection: detail.data.defaultDirection,
        defaultPaymentMethod: detail.data.defaultPaymentMethod
      });
    }
  }, [isEdit, detail.data, form]);

  if (isEdit && detail.isLoading) return <LoadingScreen />;

  const onSubmit = form.handleSubmit(async (values) => {
    if (isEdit) {
      await updateMut.mutateAsync({
        id: registerId!,
        payload: {
          name: values.name,
          description: values.description,
          currencyCode: values.currencyCode,
          defaultDirection: values.defaultDirection,
          defaultPaymentMethod: values.defaultPaymentMethod
        }
      });
    } else {
      await createMut.mutateAsync(values as CreateValues);
    }
    navigate('/cash-registers');
  });

  return (
    <PageContainer maxWidth="sm">
      <PageHeader title={isEdit ? 'Modifier la caisse' : 'Nouvelle caisse'} />
      <Card>
        <CardContent>
          <form onSubmit={onSubmit}>
            <Grid container spacing={2}>
              {!isEdit && (
                <Grid item xs={12}>
                  <TextField fullWidth size="small" label="Code (unique, MAJUSCULES)"
                    {...form.register('code' as never)}
                    error={!!(form.formState.errors as Record<string, { message?: string }>).code}
                    helperText={(form.formState.errors as Record<string, { message?: string }>).code?.message}
                  />
                </Grid>
              )}
              <Grid item xs={12}>
                <TextField fullWidth size="small" label="Nom" {...form.register('name')}
                  error={!!form.formState.errors.name}
                  helperText={form.formState.errors.name?.message as string | undefined}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField fullWidth size="small" label="Description" multiline rows={2}
                  {...form.register('description')}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label="Devise (XOF, EUR…)"
                  {...form.register('currencyCode')}
                  error={!!form.formState.errors.currencyCode}
                  helperText={form.formState.errors.currencyCode?.message as string | undefined}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField select fullWidth size="small" label="Direction par défaut des opérations"
                  {...form.register('defaultDirection')}
                  helperText="Pré-remplie à la création d'une nouvelle opération sur cette caisse."
                >
                  <MenuItem value={OperationDirection.IN}>Entrée (IN)</MenuItem>
                  <MenuItem value={OperationDirection.OUT}>Sortie (OUT)</MenuItem>
                </TextField>
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField select fullWidth size="small" label="Moyen de paiement par défaut"
                  {...form.register('defaultPaymentMethod')}
                  helperText="Pré-rempli à la création d'une nouvelle opération sur cette caisse."
                >
                  <MenuItem value={PaymentMethod.CASH}>Espèces</MenuItem>
                  <MenuItem value={PaymentMethod.MOBILE_MONEY}>Mobile Money</MenuItem>
                  <MenuItem value={PaymentMethod.BANK_TRANSFER}>Virement bancaire</MenuItem>
                  <MenuItem value={PaymentMethod.CHECK}>Chèque</MenuItem>
                </TextField>
              </Grid>
              {(createMut.isError || updateMut.isError) && (
                <Grid item xs={12}>
                  <Alert severity="error">{extractErrorMessage((createMut.error ?? updateMut.error) as unknown)}</Alert>
                </Grid>
              )}
              <Grid item xs={12}>
                <Stack direction="row" spacing={2}>
                  <Button type="submit" variant="contained">Enregistrer</Button>
                  <Button onClick={() => navigate('/cash-registers')}>Annuler</Button>
                </Stack>
              </Grid>
            </Grid>
          </form>
        </CardContent>
      </Card>
    </PageContainer>
  );
}
