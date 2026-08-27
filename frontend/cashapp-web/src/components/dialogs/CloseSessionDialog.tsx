import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  AlertTitle,
  Box,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Stack,
  TextField,
  Typography
} from '@mui/material';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import type { CloseCashSessionPayload, SessionPendingItems } from '@/types';
import { cashSessionsApi } from '@/api/cashSessionsApi';

const schema = z
  .object({
    physicalBalance: z.coerce.number().nonnegative('Le solde physique doit être >= 0'),
    closeComment: z.string().max(1000).optional(),
    varianceJustification: z.string().max(2000).optional()
  })
  .superRefine((v, ctx) => {
    // Validation conditionnelle gérée en dehors du resolver via theoreticalHint (cf. composant).
    // On laisse le superRefine pour permettre une extension future.
    void v; void ctx;
  });

type FormValues = z.infer<typeof schema>;

interface Props {
  open: boolean;
  onClose: () => void;
  onSubmit: (payload: CloseCashSessionPayload) => Promise<void> | void;
  theoreticalHint?: number;
  sessionId?: number;
}

export function CloseSessionDialog({ open, onClose, onSubmit, theoreticalHint, sessionId }: Props) {
  const { register, handleSubmit, reset, formState, watch, setError, clearErrors } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { physicalBalance: 0, closeComment: '', varianceJustification: '' }
  });

  const [pending, setPending] = useState<SessionPendingItems | null>(null);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const hasPending = !!pending && (pending.pendingOperationCount + pending.pendingTransferCount + pending.pendingDepositCount) > 0;

  useEffect(() => {
    if (open) {
      reset({ physicalBalance: 0, closeComment: '', varianceJustification: '' });
      setConfirmDelete(false);
      setPending(null);
      if (sessionId) {
        cashSessionsApi.pendingItems(sessionId).then(setPending).catch(() => setPending(null));
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, sessionId]);

  // Calcul écart en temps réel.
  const physicalRaw = watch('physicalBalance');
  const justificationRaw = watch('varianceJustification') ?? '';
  const physical = typeof physicalRaw === 'string' ? Number(physicalRaw) : physicalRaw;
  const variance =
    typeof theoreticalHint === 'number' && Number.isFinite(physical)
      ? physical - theoreticalHint
      : 0;
  const hasVariance = Number.isFinite(variance) && variance !== 0;
  const justificationMissing = hasVariance && justificationRaw.trim().length === 0;

  const submit = handleSubmit(async (v) => {
    if (hasVariance && (!v.varianceJustification || v.varianceJustification.trim().length === 0)) {
      setError('varianceJustification', { type: 'manual', message: 'Une justification est obligatoire en cas d\'écart.' });
      return;
    }
    clearErrors('varianceJustification');
    await onSubmit({
      physicalBalance: v.physicalBalance,
      closeComment: v.closeComment,
      varianceJustification: v.varianceJustification?.trim() || undefined,
      confirmDeletePending: hasPending ? confirmDelete : undefined
    });
  });

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Fermer la session</DialogTitle>
      <DialogContent>
        <Stack spacing={2} mt={1}>
          {hasPending && pending && (
            <Alert severity="warning">
              <AlertTitle>Éléments en attente d'approbation</AlertTitle>
              <Box component="ul" sx={{ pl: 2, my: 0.5 }}>
                {pending.pendingOperationCount > 0 && (
                  <li>
                    <b>{pending.pendingOperationCount}</b> opération(s) : {pending.pendingOperationRefs.slice(0, 5).join(', ')}
                    {pending.pendingOperationRefs.length > 5 ? '…' : ''}
                  </li>
                )}
                {pending.pendingTransferCount > 0 && (
                  <li>
                    <b>{pending.pendingTransferCount}</b> transfert(s) inter-caisses : {pending.pendingTransferRefs.slice(0, 5).join(', ')}
                    {pending.pendingTransferRefs.length > 5 ? '…' : ''}
                  </li>
                )}
                {pending.pendingDepositCount > 0 && (
                  <li>
                    <b>{pending.pendingDepositCount}</b> dépôt(s) banque : {pending.pendingDepositRefs.slice(0, 5).join(', ')}
                    {pending.pendingDepositRefs.length > 5 ? '…' : ''}
                  </li>
                )}
              </Box>
              <FormControlLabel
                control={<Checkbox checked={confirmDelete} onChange={(e) => setConfirmDelete(e.target.checked)} />}
                label={<Typography variant="body2">Je confirme la suppression de ces éléments pour clôturer.</Typography>}
              />
            </Alert>
          )}

          <TextField
            type="number"
            label="Solde physique compté"
            size="small"
            inputProps={{ step: '0.01', min: 0 }}
            helperText={theoreticalHint !== undefined ? `Théorique attendu : ${theoreticalHint}` : undefined}
            {...register('physicalBalance')}
            error={!!formState.errors.physicalBalance}
          />

          {hasVariance && (
            <Alert severity={Math.abs(variance) > 0 ? 'warning' : 'info'}>
              <Box>
                <Typography variant="body2">
                  Écart détecté : <b>{variance.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</b>
                </Typography>
                <Typography variant="caption">
                  Une <b>justification écrite</b> est obligatoire pour clôturer la session.
                </Typography>
              </Box>
            </Alert>
          )}

          <TextField
            label={hasVariance ? 'Justification de l\'écart (obligatoire)' : 'Justification de l\'écart (si applicable)'}
            multiline
            rows={2}
            size="small"
            required={hasVariance}
            {...register('varianceJustification')}
            error={!!formState.errors.varianceJustification || justificationMissing}
            helperText={
              formState.errors.varianceJustification?.message ??
              (justificationMissing ? 'Champ requis tant que le solde physique diffère du théorique.' : undefined)
            }
          />

          <TextField
            label="Commentaire de clôture (optionnel)"
            multiline
            rows={2}
            size="small"
            {...register('closeComment')}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Annuler</Button>
        <Button
          variant="contained"
          onClick={submit}
          disabled={formState.isSubmitting || justificationMissing || (hasPending && !confirmDelete)}
        >
          Fermer la session
        </Button>
      </DialogActions>
    </Dialog>
  );
}
