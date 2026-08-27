import { Alert, Button, Card, CardContent, Grid, Stack, TextField } from '@mui/material';
import SaveIcon from '@mui/icons-material/Save';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { useCompanyInfo, useUpdateCompanyInfo } from '@/modules/settings/hooks';
import { useNotificationStore } from '@/app/store/notificationStore';
import { extractErrorMessage } from '@/api/client';
import type { CompanyInfo } from '@/types';

export function CompanyInfoSettingsPage() {
  const query = useCompanyInfo();
  const update = useUpdateCompanyInfo();
  const notifySuccess = useNotificationStore((s) => s.notifySuccess);
  const notifyError = useNotificationStore((s) => s.notifyError);

  const form = useForm<CompanyInfo>({
    defaultValues: {
      name: '', legalForm: '', address: '', city: '', country: '',
      phone: '', email: '', website: '', registrationNumber: '', taxId: '', logoPath: ''
    }
  });

  useEffect(() => {
    if (query.data) {
      form.reset({
        name: query.data.name ?? '',
        legalForm: query.data.legalForm ?? '',
        address: query.data.address ?? '',
        city: query.data.city ?? '',
        country: query.data.country ?? '',
        phone: query.data.phone ?? '',
        email: query.data.email ?? '',
        website: query.data.website ?? '',
        registrationNumber: query.data.registrationNumber ?? '',
        taxId: query.data.taxId ?? '',
        logoPath: query.data.logoPath ?? ''
      });
    }
  }, [query.data, form]);

  if (query.isLoading) return <LoadingScreen />;

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      await update.mutateAsync(values);
      notifySuccess("Informations de l'entreprise enregistrées.");
    } catch (e) {
      notifyError(extractErrorMessage(e));
    }
  });

  return (
    <PageContainer maxWidth="md">
      <PageHeader
        title="Informations de l'entreprise"
        subtitle="Utilisées dans les exports PDF, états de caisse et documents officiels"
      />
      <Card>
        <CardContent>
          <form onSubmit={onSubmit}>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={8}>
                <TextField fullWidth size="small" label="Raison sociale" {...form.register('name')} />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField fullWidth size="small" label="Forme juridique (SA, SARL…)" {...form.register('legalForm')} />
              </Grid>
              <Grid item xs={12}>
                <TextField fullWidth size="small" label="Adresse" multiline rows={2} {...form.register('address')} />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label="Ville" {...form.register('city')} />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label="Pays" {...form.register('country')} />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label="Téléphone" {...form.register('phone')} />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label="Email" type="email" {...form.register('email')} />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label="Site web" {...form.register('website')} />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label="N° d'enregistrement (RCCM / SIRET)" {...form.register('registrationNumber')} />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label="Identifiant fiscal (NIF / TVA)" {...form.register('taxId')} />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label="Chemin du logo (optionnel)" {...form.register('logoPath')} />
              </Grid>

              {update.isError && <Grid item xs={12}><Alert severity="error">{extractErrorMessage(update.error)}</Alert></Grid>}

              <Grid item xs={12}>
                <Stack direction="row" spacing={2}>
                  <Button type="submit" variant="contained" startIcon={<SaveIcon />} disabled={update.isPending}>
                    Enregistrer
                  </Button>
                </Stack>
              </Grid>
            </Grid>
          </form>
        </CardContent>
      </Card>
    </PageContainer>
  );
}
