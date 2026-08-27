import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Grid, Typography
} from '@mui/material';
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatusChip } from '@/components/common/StatusChip';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { useCashOperation } from '@/modules/cash-operations/hooks';
import { downloadOperationReceipt } from '@/api/exportsApi';
import { useNotificationStore } from '@/app/store/notificationStore';
import { extractErrorMessage } from '@/api/client';
import { formatDate } from '@/utils/format';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Grid item xs={12} sm={6} md={4}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography>{value ?? '—'}</Typography>
    </Grid>
  );
}

interface Props {
  opId: number | null;
  open: boolean;
  onClose: () => void;
}

export function OperationDetailDialog({ opId, open, onClose }: Props) {
  const { data, isLoading } = useCashOperation(open && opId ? opId : undefined);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle sx={{ pr: 6 }}>
        {data ? `Opération ${data.operationRef}` : 'Opération'}
        {data && (
          <Typography variant="caption" color="text.secondary" display="block">
            Session #{data.cashSessionId} — {data.cashRegisterName}
          </Typography>
        )}
      </DialogTitle>
      <DialogContent dividers>
        {isLoading || !data ? <LoadingScreen /> : (
          <Box>
            {data.isDeleted && (
              <Alert severity="warning" sx={{ mb: 2 }}>
                Opération annulée le {formatDate(data.deletedAt, true)}{data.deleteReason ? ` — motif : ${data.deleteReason}` : ''}.
              </Alert>
            )}
            <Grid container spacing={2}>
              <Field label="Référence" value={data.operationRef} />
              <Field label="Date" value={formatDate(data.operationDate)} />
              <Field label="Statut session" value={<StatusChip status={data.cashSessionStatus} variant="session" />} />
              <Field label="Caisse" value={`${data.cashRegisterCode} — ${data.cashRegisterName}`} />
              <Field label="Direction" value={<StatusChip status={data.direction} variant="direction" />} />
              <Field label="Catégorie" value={`${data.categoryCode} — ${data.categoryLabel}`} />
              <Field label="Montant" value={<CurrencyDisplay value={data.amount} currency={data.currencyCode} />} />
              <Field label="Moyen de paiement" value={data.paymentMethod} />
              <Field label="Tiers" value={data.thirdPartyName} />
              <Field label="Référence externe" value={data.externalReference} />
              <Field label="Libellé" value={data.label} />
              <Grid item xs={12}>
                <Typography variant="caption" color="text.secondary">Description</Typography>
                <Typography>{data.description ?? '—'}</Typography>
              </Grid>
              <Field label="Créée le" value={formatDate(data.createdAt, true)} />
              <Field label="Modifiée le" value={formatDate(data.updatedAt, true)} />
            </Grid>
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        {data && (
          <Button
            startIcon={<ReceiptLongIcon />}
            onClick={async () => {
              try {
                await downloadOperationReceipt(data.id);
                useNotificationStore.getState().notifySuccess('Reçu PDF généré.');
              } catch (e) {
                useNotificationStore.getState().notifyError(extractErrorMessage(e));
              }
            }}
          >
            Reçu PDF
          </Button>
        )}
        <Button onClick={onClose} variant="contained">Fermer</Button>
      </DialogActions>
    </Dialog>
  );
}
