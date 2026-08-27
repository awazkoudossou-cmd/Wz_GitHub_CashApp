import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  FormControlLabel,
  Grid,
  MenuItem,
  Stack,
  TextField
} from '@mui/material';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import { z } from 'zod';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { useCashRegisters } from '@/modules/cash-registers/hooks';
import { useCreateUser, useUpdateUser, useUser } from '@/modules/users/hooks';
import { RoleCodes } from '@/types/enums';
import { extractErrorMessage } from '@/api/client';

const baseSchema = {
  username: z.string().min(2),
  fullName: z.string().min(2).max(150),
  roleCode: z.enum([RoleCodes.ADMIN, RoleCodes.SUPERVISOR, RoleCodes.CASHIER]),
  cashRegisterIds: z.array(z.number()).optional()
};

const createSchema = z.object({ ...baseSchema, password: z.string().min(8) });
const updateSchema = z.object(baseSchema);

type CreateValues = z.infer<typeof createSchema>;
type UpdateValues = z.infer<typeof updateSchema>;

export function UserFormPage() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEdit = !!id;
  const userId = id ? Number(id) : undefined;

  const userQuery = useUser(userId);
  const registers = useCashRegisters();
  const createMut = useCreateUser();
  const updateMut = useUpdateUser();

  const form = useForm<CreateValues | UpdateValues>({
    resolver: zodResolver(isEdit ? updateSchema : createSchema),
    defaultValues: { username: '', fullName: '', roleCode: RoleCodes.CASHIER, cashRegisterIds: [] }
  });

  useEffect(() => {
    if (isEdit && userQuery.data) {
      form.reset({
        username: userQuery.data.username,
        fullName: userQuery.data.fullName,
        roleCode: userQuery.data.roleCode,
        cashRegisterIds: userQuery.data.cashRegisterIds
      });
    }
  }, [isEdit, userQuery.data, form]);

  if (isEdit && userQuery.isLoading) return <LoadingScreen />;

  const onSubmit = form.handleSubmit(async (values) => {
    if (isEdit) {
      await updateMut.mutateAsync({
        id: userId!,
        payload: {
          fullName: values.fullName,
          roleCode: values.roleCode,
          cashRegisterIds: values.cashRegisterIds
        }
      });
    } else {
      await createMut.mutateAsync(values as CreateValues);
    }
    navigate('/users');
  });

  const selected = form.watch('cashRegisterIds') ?? [];
  const toggle = (rid: number) => {
    const set = new Set(selected);
    set.has(rid) ? set.delete(rid) : set.add(rid);
    form.setValue('cashRegisterIds', Array.from(set));
  };

  return (
    <PageContainer maxWidth="md">
      <PageHeader title={isEdit ? 'Modifier un utilisateur' : 'Nouvel utilisateur'} />
      <Card>
        <CardContent>
          <form onSubmit={onSubmit}>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth size="small" label="Username" disabled={isEdit}
                  {...form.register('username')}
                  error={!!form.formState.errors.username}
                  helperText={form.formState.errors.username?.message as string | undefined}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label="Nom complet"
                  {...form.register('fullName')}
                  error={!!form.formState.errors.fullName}
                  helperText={form.formState.errors.fullName?.message as string | undefined}
                />
              </Grid>
              {!isEdit && (
                <Grid item xs={12} sm={6}>
                  <TextField fullWidth size="small" type="password" label="Mot de passe"
                    {...form.register('password' as never)}
                    error={!!(form.formState.errors as Record<string, { message?: string }>).password}
                    helperText={(form.formState.errors as Record<string, { message?: string }>).password?.message}
                  />
                </Grid>
              )}
              <Grid item xs={12} sm={6}>
                <TextField select fullWidth size="small" label="Rôle"
                  value={form.watch('roleCode')}
                  onChange={(e) => form.setValue('roleCode', e.target.value as never)}
                >
                  {Object.values(RoleCodes).map((r) => (
                    <MenuItem key={r} value={r}>{r}</MenuItem>
                  ))}
                </TextField>
              </Grid>
              <Grid item xs={12}>
                <Box mb={1}><b>Caisses affectées</b></Box>
                <Stack direction="row" flexWrap="wrap" gap={1}>
                  {registers.data?.map((r) => (
                    <FormControlLabel
                      key={r.id}
                      control={<Checkbox checked={selected.includes(r.id)} onChange={() => toggle(r.id)} />}
                      label={`${r.code} — ${r.name}`}
                    />
                  ))}
                </Stack>
              </Grid>
              {(createMut.isError || updateMut.isError) && (
                <Grid item xs={12}>
                  <Alert severity="error">
                    {extractErrorMessage((createMut.error ?? updateMut.error) as unknown)}
                  </Alert>
                </Grid>
              )}
              <Grid item xs={12}>
                <Stack direction="row" spacing={2}>
                  <Button type="submit" variant="contained">Enregistrer</Button>
                  <Button onClick={() => navigate('/users')}>Annuler</Button>
                </Stack>
              </Grid>
            </Grid>
          </form>
        </CardContent>
      </Card>
    </PageContainer>
  );
}
