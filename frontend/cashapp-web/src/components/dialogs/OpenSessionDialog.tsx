import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { useAuthStore } from '@/app/store/authStore';
import { cashSessionsApi } from '@/api/cashSessionsApi';
import type { OpenCashSessionPayload, OpeningDefault } from '@/types';

const schema = z.object({
  cashRegisterId: z.coerce.number().int().positive(),
  openingBalance: z.coerce.number().nonnegative('Le solde doit être >= 0'),
  openComment: z.string().max(1000).optional()
});

type FormValues = z.infer<typeof schema>;

interface Props {
  open: boolean;
  onClose: () => void;
  onSubmit: (payload: OpenCashSessionPayload) => Promise<void> | void;
  defaultCashRegisterId?: number;
}

export function OpenSessionDialog({ open, onClose, onSubmit, defaultCashRegisterId }: Props) {
  // Référence stable : on lit la liste brute puis on dérive via useMemo, sinon le filter
  // génère une nouvelle référence à chaque rendu et le useEffect ci-dessous reset le form en boucle.
  const allRegisters = useAuthStore((s) => s.cashRegisters);
  const cashRegisters = useMemo(() => allRegisters.filter((c) => c.isActive), [allRegisters]);

  const { register, handleSubmit, reset, formState, setValue, watch } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { cashRegisterId: defaultCashRegisterId ?? cashRegisters[0]?.id, openingBalance: 0 }
  });

  const [openingDefault, setOpeningDefault] = useState<OpeningDefault | null>(null);
  const selectedRegisterId = watch('cashRegisterId');

  // Reset à l'ouverture du dialog, pas à chaque rendu.
  useEffect(() => {
    if (open) {
      const initial = defaultCashRegisterId ?? cashRegisters[0]?.id;
      reset({ cashRegisterId: initial, openingBalance: 0 });
      setOpeningDefault(null);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, defaultCashRegisterId]);

  // Fetch le solde par défaut pour la caisse sélectionnée et pré-remplir si l'utilisateur
  // n'a pas encore saisi quelque chose de différent (i.e. valeur courante == 0 ou == ancien default).
  useEffect(() => {
    if (!open || !selectedRegisterId) return;
    let cancelled = false;
    cashSessionsApi.openingDefault(selectedRegisterId).then((res) => {
      if (cancelled) return;
      setOpeningDefault(res);
      setValue('openingBalance', res.defaultOpeningBalance, { shouldDirty: false });
    }).catch(() => { /* silencieux : on garde 0 */ });
    return () => { cancelled = true; };
  }, [open, selectedRegisterId, setValue]);

  const submit = handleSubmit(async (values) => {
    await onSubmit({
      cashRegisterId: values.cashRegisterId,
      openingBalance: values.openingBalance,
      openComment: values.openComment
    });
  });

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Ouvrir une session</DialogTitle>
      <DialogContent>
        <Stack spacing={2} mt={1}>
          <TextField
            select
            label="Caisse"
            size="small"
            value={selectedRegisterId ?? ''}
            onChange={(e) => setValue('cashRegisterId', Number(e.target.value))}
            error={!!formState.errors.cashRegisterId}
            helperText={formState.errors.cashRegisterId?.message}
          >
            {cashRegisters.map((c) => (
              <MenuItem key={c.id} value={c.id}>
                {c.code} — {c.name}
              </MenuItem>
            ))}
          </TextField>

          {openingDefault?.source === 'LAST_CLOSING_PHYSICAL' && (
            <Alert severity="info">
              Solde proposé : <b>{openingDefault.defaultOpeningBalance.toLocaleString('fr-FR')}</b> — dernier solde
              physique de clôture sur cette caisse. Tu peux le modifier.
            </Alert>
          )}
          {openingDefault?.source === 'NO_PREVIOUS_SESSION' && (
            <Alert severity="info">Aucune session précédente — solde par défaut à 0.</Alert>
          )}

          <TextField
            type="number"
            label="Solde d'ouverture"
            size="small"
            inputProps={{ step: '0.01', min: 0 }}
            {...register('openingBalance')}
            error={!!formState.errors.openingBalance}
            helperText={formState.errors.openingBalance?.message}
          />
          <TextField
            label="Commentaire (optionnel)"
            size="small"
            multiline
            rows={2}
            {...register('openComment')}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Annuler</Button>
        <Button variant="contained" onClick={submit} disabled={formState.isSubmitting}>
          Ouvrir
        </Button>
      </DialogActions>
    </Dialog>
  );
}
