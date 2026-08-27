import { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  Stack,
  Switch,
  TextField
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { StatusBadge } from '@/components/common/StatusBadge';
import {
  useApprovalRules,
  useCreateApprovalRule,
  useUpdateApprovalRule,
  useUpdateApprovalRuleStatus
} from '@/modules/approvals/hooks';
import { ApprovalTargetType } from '@/types/v2Enums';
import { RoleCodes } from '@/types/enums';
import { extractErrorMessage } from '@/api/client';
import type { ApprovalRule } from '@/types';
import { formatCurrency } from '@/utils/format';

type Editing =
  | { mode: 'create' }
  | { mode: 'edit'; rule: ApprovalRule };

export function ApprovalRulesPage() {
  const { data, isLoading } = useApprovalRules();
  const updateStatus = useUpdateApprovalRuleStatus();
  const createMut = useCreateApprovalRule();
  const updateMut = useUpdateApprovalRule();
  const [editing, setEditing] = useState<Editing | null>(null);

  const columns: Column<ApprovalRule>[] = [
    { key: 'code', header: 'Code', render: (r) => r.code },
    { key: 'name', header: 'Nom', render: (r) => r.name },
    { key: 'target', header: 'Cible', render: (r) => r.targetType },
    { key: 'threshold', header: 'Seuil', align: 'right', render: (r) => formatCurrency(r.amountThreshold ?? undefined, r.currencyCode ?? 'XOF') },
    { key: 'role', header: 'Rôle', render: (r) => r.requiredApproverRole },
    { key: 'block', header: 'Bloquante', render: (r) => (r.isBlocking ? 'Oui' : 'Non') },
    { key: 'active', header: 'Active', render: (r) => <StatusBadge value={r.isActive ? 'APPROVED' : 'CANCELLED'} label={r.isActive ? 'Active' : 'Inactive'} /> },
    {
      key: 'toggle', header: '', align: 'right',
      render: (r) => (
        <Switch
          size="small" checked={r.isActive}
          onClick={(e) => e.stopPropagation()}
          onChange={(_, v) => updateStatus.mutate({ id: r.id, isActive: v })}
        />
      )
    }
  ];

  return (
    <PageContainer>
      <PageHeader
        title="Règles de validation"
        subtitle="Cliquer une ligne pour modifier, ou créer une nouvelle règle"
        actions={<Button variant="contained" startIcon={<AddIcon />} onClick={() => setEditing({ mode: 'create' })}>Nouvelle règle</Button>}
      />
      <AppTable
        columns={columns}
        rows={data}
        rowKey={(r) => r.id}
        isLoading={isLoading}
        onRowClick={(r) => setEditing({ mode: 'edit', rule: r })}
      />
      <RuleDialog
        editing={editing}
        loading={createMut.isPending || updateMut.isPending}
        error={
          createMut.isError ? extractErrorMessage(createMut.error) :
          updateMut.isError ? extractErrorMessage(updateMut.error) :
          null
        }
        onClose={() => setEditing(null)}
        onSubmit={async (p) => {
          if (editing?.mode === 'edit') {
            await updateMut.mutateAsync({
              id: editing.rule.id,
              payload: {
                name: p.name,
                description: p.description,
                amountThreshold: p.amountThreshold,
                currencyCode: p.currencyCode,
                requiredApproverRole: p.requiredApproverRole,
                isBlocking: p.isBlocking
              }
            });
          } else {
            await createMut.mutateAsync({
              code: p.code,
              name: p.name,
              description: p.description,
              targetType: p.targetType,
              amountThreshold: p.amountThreshold,
              currencyCode: p.currencyCode,
              requiredApproverRole: p.requiredApproverRole,
              isBlocking: p.isBlocking
            });
          }
          setEditing(null);
        }}
      />
    </PageContainer>
  );
}

interface DialogPayload {
  code: string;
  name: string;
  description?: string;
  targetType: keyof typeof ApprovalTargetType;
  amountThreshold?: number | null;
  currencyCode?: string | null;
  requiredApproverRole: string;
  isBlocking: boolean;
}

interface RuleDialogProps {
  editing: Editing | null;
  loading: boolean;
  error: string | null;
  onClose: () => void;
  onSubmit: (p: DialogPayload) => Promise<void>;
}

function RuleDialog({ editing, loading, error, onClose, onSubmit }: RuleDialogProps) {
  const isEdit = editing?.mode === 'edit';
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [targetType, setTargetType] = useState<keyof typeof ApprovalTargetType>('CASH_OPERATION');
  const [amountThreshold, setAmountThreshold] = useState<string>('100000');
  const [currencyCode, setCurrencyCode] = useState('XOF');
  const [requiredApproverRole, setRole] = useState(RoleCodes.SUPERVISOR);
  const [isBlocking, setBlocking] = useState(true);

  useEffect(() => {
    if (!editing) return;
    if (editing.mode === 'edit') {
      const r = editing.rule;
      setCode(r.code);
      setName(r.name);
      setDescription(r.description ?? '');
      setTargetType(r.targetType as keyof typeof ApprovalTargetType);
      setAmountThreshold(r.amountThreshold == null ? '' : String(r.amountThreshold));
      setCurrencyCode(r.currencyCode ?? '');
      setRole(r.requiredApproverRole);
      setBlocking(r.isBlocking);
    } else {
      setCode('');
      setName('');
      setDescription('');
      setTargetType('CASH_OPERATION');
      setAmountThreshold('100000');
      setCurrencyCode('XOF');
      setRole(RoleCodes.SUPERVISOR);
      setBlocking(true);
    }
  }, [editing]);

  return (
    <Dialog open={!!editing} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{isEdit ? `Modifier la règle « ${editing!.rule.code} »` : 'Nouvelle règle de validation'}</DialogTitle>
      <DialogContent>
        <Grid container spacing={2} mt={0.5}>
          <Grid item xs={6}>
            <TextField size="small" fullWidth label="Code (MAJ)" value={code} onChange={(e) => setCode(e.target.value)} disabled={isEdit} />
          </Grid>
          <Grid item xs={6}>
            <TextField size="small" fullWidth label="Nom" value={name} onChange={(e) => setName(e.target.value)} />
          </Grid>
          <Grid item xs={12}>
            <TextField size="small" fullWidth label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
          </Grid>
          <Grid item xs={6}>
            <TextField select size="small" fullWidth label="Cible" value={targetType}
              onChange={(e) => setTargetType(e.target.value as keyof typeof ApprovalTargetType)}
              disabled={isEdit}
              helperText={isEdit ? 'La cible est immuable' : undefined}>
              {Object.values(ApprovalTargetType).map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
            </TextField>
          </Grid>
          <Grid item xs={6}>
            <TextField select size="small" fullWidth label="Rôle approbateur" value={requiredApproverRole}
              onChange={(e) => setRole(e.target.value)}>
              <MenuItem value={RoleCodes.ADMIN}>ADMIN</MenuItem>
              <MenuItem value={RoleCodes.SUPERVISOR}>SUPERVISOR</MenuItem>
            </TextField>
          </Grid>
          <Grid item xs={6}>
            <TextField size="small" fullWidth type="number" label="Seuil (vide = toujours)" value={amountThreshold} onChange={(e) => setAmountThreshold(e.target.value)} />
          </Grid>
          <Grid item xs={6}>
            <TextField size="small" fullWidth label="Devise" value={currencyCode} onChange={(e) => setCurrencyCode(e.target.value)} />
          </Grid>
          <Grid item xs={12}>
            <Stack direction="row" alignItems="center">
              <Switch checked={isBlocking} onChange={(_, v) => setBlocking(v)} />
              <span>Bloquante (l'action est suspendue tant que non approuvée)</span>
            </Stack>
          </Grid>
          {error && <Grid item xs={12}><Alert severity="error">{error}</Alert></Grid>}
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Annuler</Button>
        <Button variant="contained" disabled={loading || !name.trim() || (!isEdit && !code.trim())}
          onClick={() => onSubmit({
            code: code.trim().toUpperCase(),
            name: name.trim(),
            description: description.trim() || undefined,
            targetType,
            amountThreshold: amountThreshold.trim() === '' ? null : Number(amountThreshold),
            currencyCode: currencyCode.trim() || null,
            requiredApproverRole,
            isBlocking
          })}>
          {isEdit ? 'Enregistrer' : 'Créer'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
