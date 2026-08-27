import { useEffect, useState } from 'react';
import {
  Alert, Box, Button, Card, CardContent, Dialog, DialogActions, DialogContent,
  DialogContentText, DialogTitle, Divider, Grid, Stack, Step, StepButton, Stepper,
  TextField, Typography
} from '@mui/material';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import SaveIcon from '@mui/icons-material/Save';
import EditIcon from '@mui/icons-material/Edit';
import CloseIcon from '@mui/icons-material/Close';
import { FormProvider, useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { AppSelect, AppSwitchField, AppTextField } from '@/components/forms/AppFormFields';
import {
  useCompanyInfo, useGeneralSettings, useUpdateCompanyInfo, useUpdateGeneralSettings
} from '@/modules/settings/hooks';
import { useAuthStore } from '@/app/store/authStore';
import { useNotificationStore } from '@/app/store/notificationStore';
import { settingsApi } from '@/api/settingsApi';
import type { CompanyInfo, GeneralSettings } from '@/types';
import { extractErrorMessage } from '@/api/client';

interface SectionDef {
  key: string;
  label: string;
  description: string;
}

const SECTIONS: SectionDef[] = [
  { key: 'company',    label: "Entreprise",       description: "Identité affichée sur les reçus et exports PDF." },
  { key: 'currency',   label: 'Devise & Réf.',    description: 'Devise par défaut et préfixe des références.' },
  { key: 'sessions',   label: 'Sessions',         description: "Règles d'ouverture et de clôture des sessions de caisse." },
  { key: 'variance',   label: 'Écarts',           description: "Seuils, justification, traçage des écarts." },
  { key: 'operations', label: 'Opérations',       description: 'Édition après clôture, ops annulées dans la liste.' },
  { key: 'receipts',   label: 'Reçus PDF',        description: 'Format et nombre de copies des reçus.' },
  { key: 'backup',     label: 'Sauvegarde',       description: 'Auto-backup planifié, dossier cible.' },
  { key: 'danger',     label: 'Zone dangereuse',  description: 'Réinitialisation complète des données.' }
];

export function GeneralSettingsPage() {
  const generalQ = useGeneralSettings();
  const updateGeneral = useUpdateGeneralSettings();
  const companyQ = useCompanyInfo();
  const updateCompany = useUpdateCompanyInfo();
  const navigate = useNavigate();
  const logout = useAuthStore((s) => s.logout);
  const notifySuccess = useNotificationStore((s) => s.notifySuccess);
  const notifyError = useNotificationStore((s) => s.notifyError);

  const [step, setStep] = useState(0);
  const [companyEdit, setCompanyEdit] = useState(false);

  const generalForm = useForm<GeneralSettings>({
    defaultValues: {
      defaultCurrency: 'XOF',
      autoBackupEnabled: false,
      autoBackupTime: '23:00',
      autoBackupOnSessionClose: false,
      allowOperationEditBeforeSessionClose: true,
      allowSupervisorCloseAnySession: true,
      operationRefPrefix: 'OP',
      backupDirectory: './backups',
      openingBalanceDefaultMode: 'ZERO',
      varianceJustificationThreshold: 0,
      varianceForceJustificationBelowThreshold: false,
      varianceTrackAllNonZero: false,
      showDeletedOperationsInList: false,
      receiptCopiesCount: 1
    }
  });

  const companyForm = useForm<CompanyInfo>({
    defaultValues: {
      name: '', legalForm: '', address: '', city: '', country: '',
      phone: '', email: '', website: '', registrationNumber: '', taxId: '', logoPath: ''
    }
  });

  const [resetOpen, setResetOpen] = useState(false);
  const [resetCode, setResetCode] = useState('');
  const [resetting, setResetting] = useState(false);
  const [resetError, setResetError] = useState<string | null>(null);

  useEffect(() => { if (generalQ.data) generalForm.reset(generalQ.data); }, [generalQ.data, generalForm]);
  useEffect(() => {
    if (companyQ.data) {
      companyForm.reset({
        name: companyQ.data.name ?? '',
        legalForm: companyQ.data.legalForm ?? '',
        address: companyQ.data.address ?? '',
        city: companyQ.data.city ?? '',
        country: companyQ.data.country ?? '',
        phone: companyQ.data.phone ?? '',
        email: companyQ.data.email ?? '',
        website: companyQ.data.website ?? '',
        registrationNumber: companyQ.data.registrationNumber ?? '',
        taxId: companyQ.data.taxId ?? '',
        logoPath: companyQ.data.logoPath ?? ''
      });
    }
  }, [companyQ.data, companyForm]);

  if (generalQ.isLoading || companyQ.isLoading) return <LoadingScreen />;

  const submitGeneral = generalForm.handleSubmit(async (v) => {
    try { await updateGeneral.mutateAsync(v); notifySuccess('Paramètres enregistrés.'); }
    catch (e) { notifyError(extractErrorMessage(e)); }
  });
  const submitCompany = companyForm.handleSubmit(async (v) => {
    try {
      await updateCompany.mutateAsync(v);
      notifySuccess("Informations de l'entreprise enregistrées.");
      setCompanyEdit(false);
    } catch (e) { notifyError(extractErrorMessage(e)); }
  });

  const cancelCompanyEdit = () => {
    if (companyQ.data) {
      companyForm.reset({
        name: companyQ.data.name ?? '',
        legalForm: companyQ.data.legalForm ?? '',
        address: companyQ.data.address ?? '',
        city: companyQ.data.city ?? '',
        country: companyQ.data.country ?? '',
        phone: companyQ.data.phone ?? '',
        email: companyQ.data.email ?? '',
        website: companyQ.data.website ?? '',
        registrationNumber: companyQ.data.registrationNumber ?? '',
        taxId: companyQ.data.taxId ?? '',
        logoPath: companyQ.data.logoPath ?? ''
      });
    }
    setCompanyEdit(false);
  };

  const performReset = async () => {
    setResetError(null); setResetting(true);
    try {
      await settingsApi.resetDatabase(resetCode);
      logout();
      navigate('/login', { replace: true });
    } catch (e) {
      setResetError(extractErrorMessage(e));
    } finally { setResetting(false); }
  };

  const SectionTitle = ({ title, hint }: { title: string; hint?: string }) => (
    <Box mb={2}>
      <Typography variant="h6">{title}</Typography>
      {hint && <Typography variant="body2" color="text.secondary">{hint}</Typography>}
      <Divider sx={{ mt: 1 }} />
    </Box>
  );

  const GeneralSaveBar = () => (
    <Box mt={3}>
      <Button type="submit" variant="contained" startIcon={<SaveIcon />} disabled={updateGeneral.isPending}>Enregistrer</Button>
      {updateGeneral.isError && <Alert severity="error" sx={{ mt: 2 }}>{extractErrorMessage(updateGeneral.error)}</Alert>}
    </Box>
  );

  const renderSection = () => {
    switch (SECTIONS[step].key) {
      case 'company': {
        const c = companyQ.data;
        const ReadField = ({ label, value }: { label: string; value?: string | null }) => (
          <Grid item xs={12} sm={6}>
            <Typography variant="caption" color="text.secondary">{label}</Typography>
            <Typography>{value && value.trim() ? value : '—'}</Typography>
          </Grid>
        );
        return (
          <Box>
            <Stack direction="row" justifyContent="space-between" alignItems="flex-start" mb={2}>
              <Box>
                <Typography variant="h6">Informations de l'entreprise</Typography>
                <Typography variant="body2" color="text.secondary">Utilisées dans l'en-tête des reçus PDF, états de caisse et documents officiels.</Typography>
              </Box>
              {!companyEdit && (
                <Button variant="contained" startIcon={<EditIcon />} onClick={() => setCompanyEdit(true)}>Modifier</Button>
              )}
            </Stack>
            <Divider sx={{ mb: 2 }} />

            {!companyEdit ? (
              <Grid container spacing={2}>
                <ReadField label="Raison sociale" value={c?.name} />
                <ReadField label="Forme juridique" value={c?.legalForm} />
                <Grid item xs={12}>
                  <Typography variant="caption" color="text.secondary">Adresse</Typography>
                  <Typography sx={{ whiteSpace: 'pre-line' }}>{c?.address && c.address.trim() ? c.address : '—'}</Typography>
                </Grid>
                <ReadField label="Ville" value={c?.city} />
                <ReadField label="Pays" value={c?.country} />
                <ReadField label="Téléphone" value={c?.phone} />
                <ReadField label="Email" value={c?.email} />
                <ReadField label="Site web" value={c?.website} />
                <ReadField label="N° d'enregistrement (RCCM / SIRET)" value={c?.registrationNumber} />
                <ReadField label="Identifiant fiscal (NIF / TVA)" value={c?.taxId} />
                <ReadField label="Logo (chemin)" value={c?.logoPath} />
              </Grid>
            ) : (
              <form onSubmit={submitCompany}>
                <Grid container spacing={2}>
                  <Grid item xs={12} sm={8}><TextField fullWidth size="small" label="Raison sociale" {...companyForm.register('name')} /></Grid>
                  <Grid item xs={12} sm={4}><TextField fullWidth size="small" label="Forme juridique (SA, SARL…)" {...companyForm.register('legalForm')} /></Grid>
                  <Grid item xs={12}><TextField fullWidth size="small" label="Adresse" multiline rows={2} {...companyForm.register('address')} /></Grid>
                  <Grid item xs={12} sm={6}><TextField fullWidth size="small" label="Ville" {...companyForm.register('city')} /></Grid>
                  <Grid item xs={12} sm={6}><TextField fullWidth size="small" label="Pays" {...companyForm.register('country')} /></Grid>
                  <Grid item xs={12} sm={6}><TextField fullWidth size="small" label="Téléphone" {...companyForm.register('phone')} /></Grid>
                  <Grid item xs={12} sm={6}><TextField fullWidth size="small" label="Email" type="email" {...companyForm.register('email')} /></Grid>
                  <Grid item xs={12} sm={6}><TextField fullWidth size="small" label="Site web" {...companyForm.register('website')} /></Grid>
                  <Grid item xs={12} sm={6}><TextField fullWidth size="small" label="N° d'enregistrement (RCCM / SIRET)" {...companyForm.register('registrationNumber')} /></Grid>
                  <Grid item xs={12} sm={6}><TextField fullWidth size="small" label="Identifiant fiscal (NIF / TVA)" {...companyForm.register('taxId')} /></Grid>
                  <Grid item xs={12} sm={6}><TextField fullWidth size="small" label="Chemin du logo (optionnel)" {...companyForm.register('logoPath')} /></Grid>
                </Grid>
                <Box mt={3}>
                  <Stack direction="row" spacing={1}>
                    <Button type="submit" variant="contained" startIcon={<SaveIcon />} disabled={updateCompany.isPending}>Enregistrer</Button>
                    <Button variant="outlined" startIcon={<CloseIcon />} onClick={cancelCompanyEdit} disabled={updateCompany.isPending}>Annuler</Button>
                  </Stack>
                  {updateCompany.isError && <Alert severity="error" sx={{ mt: 2 }}>{extractErrorMessage(updateCompany.error)}</Alert>}
                </Box>
              </form>
            )}
          </Box>
        );
      }

      case 'currency':
        return (
          <FormProvider {...generalForm}>
            <form onSubmit={submitGeneral}>
              <SectionTitle title="Devise & Numérotation" hint="La devise s'applique aux nouvelles caisses et aux exports. Le préfixe est utilisé pour générer les références d'opération (ex. OP-AAAAMMJJ-NNNN)." />
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6}>
                  <AppTextField name="defaultCurrency" label="Devise par défaut" helperText="Code ISO 4217 (XOF, EUR, USD…). Modifié n'affecte que les futures créations." />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <AppTextField name="operationRefPrefix" label="Préfixe des références" helperText="Préfixe ajouté devant chaque référence d'opération générée." />
                </Grid>
              </Grid>
              <GeneralSaveBar />
            </form>
          </FormProvider>
        );

      case 'sessions':
        return (
          <FormProvider {...generalForm}>
            <form onSubmit={submitGeneral}>
              <SectionTitle title="Sessions de caisse" hint="Règles appliquées à l'ouverture, à l'édition et à la clôture des sessions." />
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6}>
                  <AppSelect
                    name="openingBalanceDefaultMode"
                    label="Solde d'ouverture par défaut"
                    options={[
                      { value: 'ZERO', label: '0 (toujours)' },
                      { value: 'LAST_CLOSING_PHYSICAL', label: 'Dernier solde physique de clôture' }
                    ]}
                  />
                  <Typography variant="caption" color="text.secondary" display="block" mt={0.5}>
                    Choisir « dernier solde » pré-remplit l'ouverture avec le solde physique compté à la clôture précédente — utile pour les caisses qui restent approvisionnées d'un jour sur l'autre.
                  </Typography>
                </Grid>
                <Grid item xs={12}>
                  <AppSwitchField
                    name="allowOperationEditBeforeSessionClose"
                    label="Autoriser l'édition d'opérations tant que la session est ouverte"
                  />
                  <Typography variant="caption" color="text.secondary" display="block" sx={{ ml: 4 }}>
                    Désactivé : une opération est figée dès sa création. Activé : modifiable jusqu'à la clôture.
                  </Typography>
                </Grid>
                <Grid item xs={12}>
                  <AppSwitchField
                    name="allowSupervisorCloseAnySession"
                    label="Le superviseur peut clôturer toute session"
                  />
                  <Typography variant="caption" color="text.secondary" display="block" sx={{ ml: 4 }}>
                    Désactivé : seul l'ouvreur de session (ou un admin) peut clôturer.
                  </Typography>
                </Grid>
              </Grid>
              <GeneralSaveBar />
            </form>
          </FormProvider>
        );

      case 'variance':
        return (
          <FormProvider {...generalForm}>
            <form onSubmit={submitGeneral}>
              <SectionTitle title="Écarts de caisse" hint="Tolérance, justification obligatoire et traçage des écarts détectés à la clôture." />
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6}>
                  <AppTextField
                    name="varianceJustificationThreshold"
                    label="Tolérance d'écart sans justification"
                    type="number"
                    helperText="En valeur absolue. En dessous, la justification devient optionnelle. 0 = toujours obligatoire dès qu'il y a un écart."
                  />
                </Grid>
                <Grid item xs={12}>
                  <AppSwitchField
                    name="varianceForceJustificationBelowThreshold"
                    label="Forcer la justification même sous le seuil de tolérance"
                  />
                  <Typography variant="caption" color="text.secondary" display="block" sx={{ ml: 4 }}>
                    Activé : la justification reste obligatoire pour n'importe quel écart non nul, indépendamment du seuil ci-dessus.
                  </Typography>
                </Grid>
                <Grid item xs={12}>
                  <AppSwitchField
                    name="varianceTrackAllNonZero"
                    label="Tracer dans le menu « Écarts de caisse » tout écart non nul"
                  />
                  <Typography variant="caption" color="text.secondary" display="block" sx={{ ml: 4 }}>
                    Activé : chaque clôture avec un écart, même minime, crée un dossier consultable. Désactivé : seuls les écarts ≥ seuil de justification créent un dossier (et nécessitent la feature « Gestion avancée des écarts »).
                  </Typography>
                </Grid>
              </Grid>
              <GeneralSaveBar />
            </form>
          </FormProvider>
        );

      case 'operations':
        return (
          <FormProvider {...generalForm}>
            <form onSubmit={submitGeneral}>
              <SectionTitle title="Opérations" hint="Comportement du menu Opérations et règles d'édition." />
              <Grid container spacing={2}>
                <Grid item xs={12}>
                  <AppSwitchField
                    name="showDeletedOperationsInList"
                    label="Inclure les opérations rejetées / annulées dans la liste"
                  />
                  <Typography variant="caption" color="text.secondary" display="block" sx={{ ml: 4 }}>
                    Activé : les opérations soft-deleted apparaissent dans le menu Opérations avec un statut « ANNULÉE ». Elles n'impactent jamais les calculs de solde — usage : audit et traçabilité.
                  </Typography>
                </Grid>
              </Grid>
              <GeneralSaveBar />
            </form>
          </FormProvider>
        );

      case 'receipts':
        return (
          <FormProvider {...generalForm}>
            <form onSubmit={submitGeneral}>
              <SectionTitle title="Reçus PDF" hint="Format d'impression des reçus d'opération." />
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6}>
                  <AppSelect
                    name="receiptCopiesCount"
                    label="Nombre de copies par reçu"
                    options={[
                      { value: 1, label: '1 copie (pleine page A5)' },
                      { value: 2, label: '2 copies (client + souche)' }
                    ]}
                  />
                  <Typography variant="caption" color="text.secondary" display="block" mt={0.5}>
                    Mode 2 copies : la page A5 est partagée entre une copie originale (client) et une copie souche (caisse). La police est réduite automatiquement.
                  </Typography>
                </Grid>
              </Grid>
              <GeneralSaveBar />
            </form>
          </FormProvider>
        );

      case 'backup':
        return (
          <FormProvider {...generalForm}>
            <form onSubmit={submitGeneral}>
              <SectionTitle title="Sauvegarde" hint="Sauvegarde du fichier SQLite. Manuelle via le menu Sauvegardes, ou automatique selon ces réglages." />
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6}><AppTextField name="backupDirectory" label="Dossier des sauvegardes" helperText="Chemin local côté serveur. Relatif au répertoire de l'API si non absolu." /></Grid>
                <Grid item xs={12} sm={6}><AppTextField name="autoBackupTime" label="Heure d'auto-backup (HH:MM)" helperText="Exécuté quotidiennement à cette heure si l'auto-backup est activé." /></Grid>
                <Grid item xs={12}>
                  <AppSwitchField name="autoBackupEnabled" label="Sauvegarde automatique planifiée" />
                  <Typography variant="caption" color="text.secondary" display="block" sx={{ ml: 4 }}>
                    Quotidienne à l'heure configurée ci-dessus.
                  </Typography>
                </Grid>
                <Grid item xs={12}>
                  <AppSwitchField name="autoBackupOnSessionClose" label="Sauvegarder après chaque clôture de session" />
                  <Typography variant="caption" color="text.secondary" display="block" sx={{ ml: 4 }}>
                    Activé : un backup est déclenché immédiatement après chaque clôture validée. Utile pour les caisses à forte rotation.
                  </Typography>
                </Grid>
              </Grid>
              <GeneralSaveBar />
            </form>
          </FormProvider>
        );

      case 'danger':
        return (
          <Box>
            <SectionTitle title="Zone dangereuse" hint="Opérations irréversibles." />
            <Card sx={{ borderColor: 'error.main', borderStyle: 'solid', borderWidth: 1 }}>
              <CardContent>
                <Stack direction="row" alignItems="center" spacing={1} mb={1}>
                  <WarningAmberIcon color="error" />
                  <Typography variant="h6" color="error">Réinitialiser la base de données</Typography>
                </Stack>
                <Typography variant="body2" color="text.secondary">
                  Supprime <b>toutes</b> les données métier (utilisateurs, caisses, sessions, opérations, transferts, dépôts, anomalies, écarts, validations, sauvegardes, pièces jointes, imports…) et restaure uniquement les valeurs par défaut (admin <code>admin/Admin@123</code>, settings, features V1 actives, catégories par défaut).
                </Typography>
                <Typography variant="body2" color="error" sx={{ mt: 1 }}>
                  <b>Cette action est irréversible.</b>
                </Typography>
                <Box mt={2}>
                  <Button color="error" variant="outlined" startIcon={<WarningAmberIcon />} onClick={() => { setResetCode(''); setResetError(null); setResetOpen(true); }}>
                    Réinitialiser la base de données
                  </Button>
                </Box>
              </CardContent>
            </Card>
          </Box>
        );
    }
  };

  return (
    <PageContainer maxWidth="lg">
      <PageHeader title="Paramètres généraux" />
      <Grid container spacing={3}>
        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Stepper nonLinear orientation="vertical" activeStep={step}>
                {SECTIONS.map((s, i) => (
                  <Step key={s.key} completed={false}>
                    <StepButton onClick={() => setStep(i)}>
                      <Box textAlign="left">
                        <Typography variant="body2" fontWeight={step === i ? 700 : 400}>{s.label}</Typography>
                        <Typography variant="caption" color="text.secondary">{s.description}</Typography>
                      </Box>
                    </StepButton>
                  </Step>
                ))}
              </Stepper>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={9}>
          <Card>
            <CardContent>{renderSection()}</CardContent>
          </Card>
        </Grid>
      </Grid>

      <Dialog open={resetOpen} onClose={() => !resetting && setResetOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ color: 'error.main' }}>Confirmer la réinitialisation</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ mb: 2 }}>
            Toutes les données seront <b>définitivement supprimées</b>. Pour confirmer, tape <b>RESET</b> en majuscules dans le champ ci-dessous.
          </DialogContentText>
          <TextField fullWidth autoFocus size="small" value={resetCode} onChange={(e) => setResetCode(e.target.value)} placeholder="Tape RESET pour confirmer" disabled={resetting} />
          {resetError && <Alert severity="error" sx={{ mt: 2 }}>{resetError}</Alert>}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setResetOpen(false)} disabled={resetting}>Annuler</Button>
          <Button color="error" variant="contained" disabled={resetCode !== 'RESET' || resetting} onClick={performReset}>
            {resetting ? 'Réinitialisation…' : 'Réinitialiser définitivement'}
          </Button>
        </DialogActions>
      </Dialog>
    </PageContainer>
  );
}
