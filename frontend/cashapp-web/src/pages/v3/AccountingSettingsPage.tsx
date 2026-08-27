import { useEffect, useState } from 'react';
import {
  Alert, Autocomplete, Box, Button, Card, CardContent, Chip, Divider, Grid, MenuItem,
  Radio, RadioGroup, FormControlLabel, Step, StepContent, StepLabel, Stepper, Stack,
  Switch, TextField, Typography
} from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import EditIcon from '@mui/icons-material/Edit';
import SaveIcon from '@mui/icons-material/Save';
import CloseIcon from '@mui/icons-material/Close';
import RestartAltIcon from '@mui/icons-material/RestartAlt';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatCard } from '@/components/common/StatCard';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { ConfirmDialog } from '@/components/dialogs/ConfirmDialog';
import { useNotificationStore } from '@/app/store/notificationStore';
import { extractErrorMessage } from '@/api/client';
import {
  useAccountingAccounts,
  useAccountingChecklist,
  useAccountingJournals,
  useAccountingSettings,
  useAssignCashRegisterAccount,
  useAssignCashRegisterJournal,
  useAssignCategoryAccount,
  useUpdateAccountingSettings,
  useWizardCashRegisters,
  useWizardCategories
} from '@/modules/accounting/hooks';
import { AccountingGenerationMode, AccountingGenerationType } from '@/types/v2Enums';
import type { AccountingAccountListItem, WizardCashRegister, WizardCategory } from '@/types';

const STEPS = [
  'Journaux des caisses',
  'Comptes des caisses',
  'Comptes des catégories',
  'Type de génération',
  'Mode de génération',
  'Modèle de libellé',
  'Numérotation automatique',
  'Validation'
];

const DEFAULT_TEMPLATE = '{Category} - {CashRegister} - {OperationNumber}';

const TEMPLATE_VARIABLES = [
  'OperationNumber', 'Category', 'CashRegister', 'Amount',
  'OperationDate', 'Reference', 'Cashier', 'SessionNumber', 'Journal'
];

const SAMPLE_VALUES: Record<string, string> = {
  OperationNumber: 'OP-20260101-0001',
  Category: 'Vente',
  CashRegister: 'CAISSE-01',
  Amount: '25 000',
  OperationDate: '2026-01-01',
  Reference: 'REF-0001',
  Cashier: 'Jean Dupont',
  SessionNumber: '#1',
  Journal: 'VE'
};

function renderTemplate(template: string): string {
  return template.replace(/\{(\w+)\}/g, (match, key: string) => SAMPLE_VALUES[key] ?? match);
}

function accountLabel(a: { accountNumber: string; name: string }) {
  return `${a.accountNumber} — ${a.name}`;
}

interface PreviewRow {
  journal: string;
  account: string;
  label: string;
  debit: string;
  credit: string;
}

const PREVIEW_COLUMNS: Column<PreviewRow>[] = [
  { key: 'jrn', header: 'Journal', render: (r) => r.journal },
  { key: 'cpt', header: 'Compte', render: (r) => r.account },
  { key: 'lib', header: 'Libellé', render: (r) => r.label },
  { key: 'deb', header: 'Débit', align: 'right', render: (r) => r.debit },
  { key: 'cred', header: 'Crédit', align: 'right', render: (r) => r.credit }
];

export function AccountingSettingsPage() {
  const settingsQ = useAccountingSettings();
  const updateSettings = useUpdateAccountingSettings();
  const registersQ = useWizardCashRegisters();
  const categoriesQ = useWizardCategories();
  const journalsQ = useAccountingJournals();
  const accountsQ = useAccountingAccounts({ isActive: true, pageSize: 500 });
  const checklistQ = useAccountingChecklist();

  const assignJournal = useAssignCashRegisterJournal();
  const assignRegisterAccount = useAssignCashRegisterAccount();
  const assignCategoryAccount = useAssignCategoryAccount();

  const notifySuccess = useNotificationStore((s) => s.notifySuccess);
  const notifyError = useNotificationStore((s) => s.notifyError);

  const [editMode, setEditMode] = useState(false);
  const [activeStep, setActiveStep] = useState(0);
  const [generationType, setGenerationType] = useState<AccountingGenerationType>(AccountingGenerationType.DETAILED);
  const [generationMode, setGenerationMode] = useState<AccountingGenerationMode>(AccountingGenerationMode.MANUAL);
  const [narrationTemplate, setNarrationTemplate] = useState(DEFAULT_TEMPLATE);
  const [cashAccountRootNumber, setCashAccountRootNumber] = useState('');
  const [cashAccountNumberLength, setCashAccountNumberLength] = useState('');
  const [cashJournalRootCode, setCashJournalRootCode] = useState('');
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [onlyUnconfiguredCategories, setOnlyUnconfiguredCategories] = useState(false);

  useEffect(() => {
    if (settingsQ.data) {
      setGenerationType(settingsQ.data.generationType);
      setGenerationMode(settingsQ.data.generationMode);
      setNarrationTemplate(settingsQ.data.narrationTemplate ?? DEFAULT_TEMPLATE);
      setCashAccountRootNumber(settingsQ.data.cashAccountRootNumber ?? '');
      setCashAccountNumberLength(settingsQ.data.cashAccountNumberLength != null ? String(settingsQ.data.cashAccountNumberLength) : '');
      setCashJournalRootCode(settingsQ.data.cashJournalRootCode ?? '');
      if (!settingsQ.data.isConfigured) setEditMode(true);
    }
  }, [settingsQ.data]);

  if (settingsQ.isLoading || registersQ.isLoading || categoriesQ.isLoading || journalsQ.isLoading || accountsQ.isLoading) {
    return <LoadingScreen />;
  }

  const registers = registersQ.data ?? [];
  const activeRegisters = registers.filter((r) => r.isActive);
  const journalsConfiguredCount = activeRegisters.filter((r) => r.accountingJournalId).length;
  const accountsConfiguredCount = activeRegisters.filter((r) => r.accountingAccountId).length;

  const categories = categoriesQ.data ?? [];
  const usedCategories = categories.filter((c) => c.isUsed);
  const categoryAccountsConfiguredCount = usedCategories.filter((c) => c.accountingAccountId).length;
  const displayedCategories = onlyUnconfiguredCategories ? categories.filter((c) => !c.accountingAccountId) : categories;

  const journalOptions = (journalsQ.data ?? []).filter((j) => j.isActive);
  const accountOptions = accountsQ.data?.items ?? [];

  const step1Ok = activeRegisters.length === 0 || journalsConfiguredCount === activeRegisters.length;
  const step2Ok = activeRegisters.length === 0 || accountsConfiguredCount === activeRegisters.length;
  const step3Ok = usedCategories.length === 0 || categoryAccountsConfiguredCount === usedCategories.length;
  const step6Ok = narrationTemplate.trim().length > 0;
  const stepOk = [step1Ok, step2Ok, step3Ok, true, true, step6Ok, true, checklistQ.data?.allOk ?? false];

  const handleNext = () => setActiveStep((s) => Math.min(s + 1, STEPS.length - 1));
  const handleBack = () => setActiveStep((s) => Math.max(s - 1, 0));

  const startEdit = () => { setActiveStep(0); setEditMode(true); };

  const cancelEdit = () => {
    if (settingsQ.data) {
      setGenerationType(settingsQ.data.generationType);
      setGenerationMode(settingsQ.data.generationMode);
      setNarrationTemplate(settingsQ.data.narrationTemplate ?? DEFAULT_TEMPLATE);
      setCashAccountRootNumber(settingsQ.data.cashAccountRootNumber ?? '');
      setCashAccountNumberLength(settingsQ.data.cashAccountNumberLength != null ? String(settingsQ.data.cashAccountNumberLength) : '');
      setCashJournalRootCode(settingsQ.data.cashJournalRootCode ?? '');
    }
    setActiveStep(0);
    setEditMode(false);
  };

  const numberingPayload = {
    cashAccountRootNumber: cashAccountRootNumber.trim() || null,
    cashAccountNumberLength: cashAccountNumberLength.trim() ? Number(cashAccountNumberLength) : null,
    cashJournalRootCode: cashJournalRootCode.trim() || null
  };

  const saveDraft = async () => {
    try {
      await updateSettings.mutateAsync({
        generationType, generationMode, narrationTemplate,
        isConfigured: settingsQ.data?.isConfigured ?? false,
        ...numberingPayload
      });
      notifySuccess('Paramètres comptables enregistrés.');
    } catch (e) { notifyError(extractErrorMessage(e)); }
  };

  const validateConfiguration = async () => {
    try {
      await updateSettings.mutateAsync({ generationType, generationMode, narrationTemplate, isConfigured: true, ...numberingPayload });
      notifySuccess('Configuration comptable validée.');
      setConfirmOpen(false);
      setEditMode(false);
    } catch (e) {
      notifyError(extractErrorMessage(e));
      setConfirmOpen(false);
    }
  };

  const registerColumns = (mode: 'journal' | 'account'): Column<WizardCashRegister>[] => [
    { key: 'name', header: 'Nom', render: (r) => r.name },
    { key: 'code', header: 'Code caisse', render: (r) => r.code },
    {
      key: mode,
      header: mode === 'journal' ? 'Journal associé' : 'Compte associé',
      render: (r) =>
        mode === 'journal' ? (
          <TextField
            select size="small" fullWidth sx={{ minWidth: 220 }}
            value={r.accountingJournalId ?? ''}
            onChange={(e) => assignJournal.mutate({ cashRegisterId: r.id, accountingJournalId: Number(e.target.value) })}
          >
            {journalOptions.map((j) => <MenuItem key={j.id} value={j.id}>{j.code} — {j.name}</MenuItem>)}
          </TextField>
        ) : (
          <Autocomplete
            size="small" sx={{ minWidth: 260 }}
            options={accountOptions}
            getOptionLabel={accountLabel}
            isOptionEqualToValue={(a, b) => a.id === b.id}
            value={accountOptions.find((a) => a.id === r.accountingAccountId) ?? null}
            onChange={(_, val) => val && assignRegisterAccount.mutate({ cashRegisterId: r.id, accountingAccountId: val.id })}
            renderInput={(params) => <TextField {...params} placeholder="Rechercher un compte…" />}
          />
        )
    },
    { key: 'etat', header: 'État', render: (r) => <Chip size="small" label={r.isActive ? 'Active' : 'Inactive'} color={r.isActive ? 'success' : 'default'} /> }
  ];

  const categoryColumns: Column<WizardCategory>[] = [
    { key: 'code', header: 'Catégorie', render: (c) => `${c.code} — ${c.label}` },
    {
      key: 'account', header: 'Compte',
      render: (c) => (
        <Autocomplete
          size="small" sx={{ minWidth: 260 }}
          options={accountOptions}
          getOptionLabel={accountLabel}
          isOptionEqualToValue={(a, b) => a.id === b.id}
          value={accountOptions.find((a: AccountingAccountListItem) => a.id === c.accountingAccountId) ?? null}
          onChange={(_, val) => val && assignCategoryAccount.mutate({ categoryId: c.id, accountingAccountId: val.id })}
          renderInput={(params) => <TextField {...params} placeholder="Rechercher un compte…" />}
        />
      )
    },
    { key: 'used', header: 'Utilisée', render: (c) => <Chip size="small" label={c.isUsed ? 'Oui' : 'Non'} color={c.isUsed ? 'info' : 'default'} /> },
    { key: 'etat', header: 'État', render: (c) => <Chip size="small" label={c.isActive ? 'Active' : 'Inactive'} color={c.isActive ? 'success' : 'default'} /> }
  ];

  const renderStepContent = (index: number) => {
    switch (index) {
      case 0:
        return (
          <Box>
            <Typography variant="body2" color="text.secondary" mb={1}>
              {journalsConfiguredCount} / {activeRegisters.length} caisses configurées.
              {journalOptions.length === 0 && ' Aucun journal actif — créez-en un dans « Journaux comptables ».'}
            </Typography>
            <AppTable columns={registerColumns('journal')} rows={activeRegisters} rowKey={(r) => r.id} emptyTitle="Aucune caisse active" />
          </Box>
        );
      case 1:
        return (
          <Box>
            <Typography variant="body2" color="text.secondary" mb={1}>
              {accountsConfiguredCount} / {activeRegisters.length} caisses configurées.
            </Typography>
            <AppTable columns={registerColumns('account')} rows={activeRegisters} rowKey={(r) => r.id} emptyTitle="Aucune caisse active" />
          </Box>
        );
      case 2:
        return (
          <Box>
            <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1} mb={1}>
              <Typography variant="body2" color="text.secondary">
                {categoryAccountsConfiguredCount} / {usedCategories.length} catégories utilisées configurées.
                Seules les catégories déjà utilisées dans au moins une opération doivent avoir un compte.
              </Typography>
              <FormControlLabel
                control={<Switch size="small" checked={onlyUnconfiguredCategories} onChange={(e) => setOnlyUnconfiguredCategories(e.target.checked)} />}
                label="Sans compte uniquement"
              />
            </Stack>
            <AppTable
              columns={categoryColumns}
              rows={displayedCategories}
              rowKey={(c) => c.id}
              emptyTitle={onlyUnconfiguredCategories ? 'Toutes les catégories ont un compte' : 'Aucune catégorie'}
            />
          </Box>
        );
      case 3:
        return (
          <Grid container spacing={2}>
            <Grid item xs={12}>
              <RadioGroup
                value={generationType}
                onChange={(e) => setGenerationType(e.target.value as AccountingGenerationType)}
              >
                <Grid container spacing={2}>
                  <Grid item xs={12} md={6}>
                    <Card variant={generationType === AccountingGenerationType.DETAILED ? 'elevation' : 'outlined'} sx={{ borderColor: generationType === AccountingGenerationType.DETAILED ? 'primary.main' : undefined }}>
                      <CardContent>
                        <FormControlLabel value={AccountingGenerationType.DETAILED} control={<Radio />} label={<Typography fontWeight={600}>Génération détaillée</Typography>} />
                        <Typography variant="body2" color="text.secondary" sx={{ ml: 4 }}>
                          Chaque opération produit ses propres écritures comptables (une écriture caisse + une écriture contrepartie par opération).
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={12} md={6}>
                    <Card variant={generationType === AccountingGenerationType.CENTRALIZED ? 'elevation' : 'outlined'} sx={{ borderColor: generationType === AccountingGenerationType.CENTRALIZED ? 'primary.main' : undefined }}>
                      <CardContent>
                        <FormControlLabel value={AccountingGenerationType.CENTRALIZED} control={<Radio />} label={<Typography fontWeight={600}>Génération centralisée</Typography>} />
                        <Typography variant="body2" color="text.secondary" sx={{ ml: 4 }}>
                          Les comptes de contrepartie sont regroupés par journal et par compte caisse : une seule écriture de synthèse par groupe.
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                </Grid>
              </RadioGroup>
            </Grid>
            <Grid item xs={12}>
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="overline" color="text.secondary">Aperçu</Typography>
                  {generationType === AccountingGenerationType.DETAILED ? (
                    <AppTable
                      columns={PREVIEW_COLUMNS}
                      rows={[
                        { debit: '25 000', credit: '', journal: 'VE', account: '571000 — Caisse', label: 'Vente OP-0001' },
                        { debit: '', credit: '25 000', journal: 'VE', account: '701000 — Ventes', label: 'Vente OP-0001' },
                        { debit: '10 000', credit: '', journal: 'VE', account: '571000 — Caisse', label: 'Vente OP-0002' },
                        { debit: '', credit: '10 000', journal: 'VE', account: '701000 — Ventes', label: 'Vente OP-0002' }
                      ]}
                      rowKey={(r) => r.label + r.debit + r.credit}
                    />
                  ) : (
                    <AppTable
                      columns={PREVIEW_COLUMNS}
                      rows={[
                        { debit: '35 000', credit: '', journal: 'VE', account: '571000 — Caisse', label: 'Synthèse VE — 2 opérations' },
                        { debit: '', credit: '35 000', journal: 'VE', account: '701000 — Ventes', label: 'Synthèse VE — 2 opérations' }
                      ]}
                      rowKey={(r) => r.label + r.debit + r.credit}
                    />
                  )}
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        );
      case 4:
        return (
          <RadioGroup value={generationMode} onChange={(e) => setGenerationMode(e.target.value as AccountingGenerationMode)}>
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <Card variant={generationMode === AccountingGenerationMode.MANUAL ? 'elevation' : 'outlined'} sx={{ borderColor: generationMode === AccountingGenerationMode.MANUAL ? 'primary.main' : undefined }}>
                  <CardContent>
                    <FormControlLabel value={AccountingGenerationMode.MANUAL} control={<Radio />} label={<Typography fontWeight={600}>Manuellement</Typography>} />
                    <Typography variant="body2" color="text.secondary" sx={{ ml: 4 }}>
                      L'utilisateur choisira plus tard une date de début, une date de fin et des caisses, puis lancera la génération des écritures.
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
              <Grid item xs={12} md={6}>
                <Card variant={generationMode === AccountingGenerationMode.ON_CASH_CLOSING ? 'elevation' : 'outlined'} sx={{ borderColor: generationMode === AccountingGenerationMode.ON_CASH_CLOSING ? 'primary.main' : undefined }}>
                  <CardContent>
                    <FormControlLabel value={AccountingGenerationMode.ON_CASH_CLOSING} control={<Radio />} label={<Typography fontWeight={600}>Automatiquement à la clôture de caisse</Typography>} />
                    <Typography variant="body2" color="text.secondary" sx={{ ml: 4 }}>
                      Après chaque clôture, le moteur tente automatiquement la génération. Les opérations incomplètes sont placées dans « Opérations comptables en attente ».
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </RadioGroup>
        );
      case 5:
        return (
          <Box>
            <Stack direction="row" spacing={1} flexWrap="wrap" mb={2}>
              {TEMPLATE_VARIABLES.map((v) => (
                <Chip
                  key={v} size="small" label={`{${v}}`} clickable
                  onClick={() => setNarrationTemplate((t) => `${t}{${v}}`)}
                />
              ))}
            </Stack>
            <TextField
              fullWidth label="Modèle de libellé" value={narrationTemplate}
              onChange={(e) => setNarrationTemplate(e.target.value)}
              helperText="Utilisez les variables ci-dessus. Le modèle est libre."
            />
            <Stack direction="row" spacing={1} mt={1}>
              <Button size="small" startIcon={<RestartAltIcon />} onClick={() => setNarrationTemplate(DEFAULT_TEMPLATE)}>
                Réinitialiser
              </Button>
            </Stack>
            <Card variant="outlined" sx={{ mt: 2 }}>
              <CardContent>
                <Typography variant="overline" color="text.secondary">Aperçu en temps réel</Typography>
                <Typography variant="body1">{renderTemplate(narrationTemplate) || '—'}</Typography>
              </CardContent>
            </Card>
          </Box>
        );
      case 6: {
        const rootDigitsOnly = cashAccountRootNumber.trim();
        const suffixWidth = rootDigitsOnly
          ? Math.max(1, (cashAccountNumberLength.trim() ? Number(cashAccountNumberLength) : rootDigitsOnly.length + 2) - rootDigitsOnly.length)
          : 0;
        const previewAccount = rootDigitsOnly ? `${rootDigitsOnly}${'1'.padStart(suffixWidth, '0')}` : null;
        const previewJournal = cashJournalRootCode.trim() ? `${cashJournalRootCode.trim().toUpperCase()}001` : null;

        return (
          <Box>
            <Typography variant="body2" color="text.secondary" mb={2}>
              Lorsqu'une nouvelle caisse est créée, un compte comptable et un journal peuvent lui être attribués automatiquement.
              Laissez ces champs vides pour continuer à les assigner manuellement (étapes 1 et 2).
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={4}>
                <TextField
                  fullWidth label="Racine des comptes de caisse" placeholder="571100"
                  value={cashAccountRootNumber}
                  onChange={(e) => setCashAccountRootNumber(e.target.value.replace(/[^0-9]/g, ''))}
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField
                  fullWidth type="number" label="Longueur du numéro (optionnel)" placeholder="8"
                  value={cashAccountNumberLength}
                  onChange={(e) => setCashAccountNumberLength(e.target.value)}
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField
                  fullWidth label="Racine des codes journaux" placeholder="CAI"
                  value={cashJournalRootCode}
                  onChange={(e) => setCashJournalRootCode(e.target.value.toUpperCase())}
                />
              </Grid>
            </Grid>
            {(previewAccount || previewJournal) && (
              <Card variant="outlined" sx={{ mt: 2 }}>
                <CardContent>
                  <Typography variant="overline" color="text.secondary">Aperçu de la prochaine caisse créée</Typography>
                  <Typography>Compte comptable : {previewAccount ?? '—'}</Typography>
                  <Typography>Journal comptable : {previewJournal ?? '—'}</Typography>
                </CardContent>
              </Card>
            )}
          </Box>
        );
      }
      case 7: {
        const items = checklistQ.data?.items ?? [];
        return (
          <Box>
            <Stack spacing={1} divider={<Divider />}>
              {items.map((item) => (
                <Stack key={item.code} direction="row" alignItems="center" spacing={1.5} py={0.5}>
                  {item.ok ? <CheckCircleIcon color="success" fontSize="small" /> : <CancelIcon color="error" fontSize="small" />}
                  <Box flex={1}>
                    <Typography>{item.label}</Typography>
                    {item.detail && <Typography variant="caption" color="text.secondary">{item.detail}</Typography>}
                  </Box>
                  <Chip size="small" label={item.ok ? 'OK' : 'Erreur'} color={item.ok ? 'success' : 'error'} />
                </Stack>
              ))}
            </Stack>
            <Box mt={3}>
              <Button
                variant="contained" color="primary"
                disabled={!checklistQ.data?.allOk || updateSettings.isPending}
                onClick={() => setConfirmOpen(true)}
              >
                Valider la configuration
              </Button>
              {!checklistQ.data?.allOk && (
                <Alert severity="warning" sx={{ mt: 2 }}>
                  Corrigez les points en erreur ci-dessus avant de valider la configuration.
                </Alert>
              )}
            </Box>
          </Box>
        );
      }
      default:
        return null;
    }
  };

  return (
    <PageContainer maxWidth="lg">
      <PageHeader
        title="Paramètres comptables"
        subtitle="Assistant de configuration du moteur comptable"
        actions={
          editMode ? (
            <Stack direction="row" spacing={1}>
              <Button variant="outlined" startIcon={<CloseIcon />} onClick={cancelEdit}>Annuler</Button>
              <Button variant="contained" startIcon={<SaveIcon />} onClick={saveDraft} disabled={updateSettings.isPending}>Enregistrer</Button>
            </Stack>
          ) : (
            <Button variant="contained" startIcon={<EditIcon />} onClick={startEdit}>Modifier</Button>
          )
        }
      />

      {!editMode ? (
        <Box>
          <Grid container spacing={2} mb={3}>
            <Grid item xs={6} md={3}>
              <StatCard label="Caisses → journal" value={`${journalsConfiguredCount} / ${activeRegisters.length}`} color={step1Ok ? 'success' : 'warning'} />
            </Grid>
            <Grid item xs={6} md={3}>
              <StatCard label="Caisses → compte" value={`${accountsConfiguredCount} / ${activeRegisters.length}`} color={step2Ok ? 'success' : 'warning'} />
            </Grid>
            <Grid item xs={6} md={3}>
              <StatCard label="Catégories → compte" value={`${categoryAccountsConfiguredCount} / ${usedCategories.length}`} color={step3Ok ? 'success' : 'warning'} />
            </Grid>
            <Grid item xs={6} md={3}>
              <StatCard label="Configuration" value={settingsQ.data?.isConfigured ? 'Validée' : 'Non validée'} color={settingsQ.data?.isConfigured ? 'success' : 'error'} />
            </Grid>
          </Grid>
          <Card>
            <CardContent>
              <Typography variant="overline" color="text.secondary">Type de génération</Typography>
              <Typography mb={2}>{generationType === AccountingGenerationType.DETAILED ? 'Détaillée' : 'Centralisée'}</Typography>
              <Typography variant="overline" color="text.secondary">Mode de génération</Typography>
              <Typography mb={2}>{generationMode === AccountingGenerationMode.MANUAL ? 'Manuel' : 'Automatique à la clôture'}</Typography>
              <Typography variant="overline" color="text.secondary">Modèle de libellé</Typography>
              <Typography mb={2}>{narrationTemplate || '—'}</Typography>
              <Typography variant="overline" color="text.secondary">Numérotation automatique</Typography>
              <Typography>
                {cashAccountRootNumber || cashJournalRootCode
                  ? `Comptes : ${cashAccountRootNumber || '—'}${cashAccountNumberLength ? ` (longueur ${cashAccountNumberLength})` : ''} · Journaux : ${cashJournalRootCode || '—'}`
                  : 'Non configurée — les comptes/journaux des caisses doivent être assignés manuellement.'}
              </Typography>
            </CardContent>
          </Card>
        </Box>
      ) : (
        <Stepper activeStep={activeStep} orientation="vertical">
          {STEPS.map((label, index) => (
            <Step key={label}>
              <StepLabel error={activeStep > index && !stepOk[index]}>{label}</StepLabel>
              <StepContent>
                {renderStepContent(index)}
                <Box mt={2}>
                  <Stack direction="row" spacing={1}>
                    <Button disabled={index === 0} onClick={handleBack}>Précédent</Button>
                    {index < STEPS.length - 1 && (
                      <Button variant="contained" onClick={handleNext} disabled={!stepOk[index]}>Suivant</Button>
                    )}
                  </Stack>
                  {index < STEPS.length - 1 && !stepOk[index] && (
                    <Typography variant="caption" color="error" display="block" mt={1}>
                      Complétez cette étape avant de continuer.
                    </Typography>
                  )}
                </Box>
              </StepContent>
            </Step>
          ))}
        </Stepper>
      )}

      <ConfirmDialog
        open={confirmOpen}
        title="Valider la configuration comptable"
        description="Une fois validée, le moteur comptable pourra être utilisé. Vous pourrez toujours revenir modifier ces paramètres plus tard."
        confirmLabel="Valider"
        onConfirm={validateConfiguration}
        onClose={() => setConfirmOpen(false)}
      />
    </PageContainer>
  );
}
