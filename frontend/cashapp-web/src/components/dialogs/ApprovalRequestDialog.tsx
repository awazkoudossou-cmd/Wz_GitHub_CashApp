import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Grid, Stack, Typography
} from '@mui/material';
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { StatusBadge } from '@/components/common/StatusBadge';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { ApprovalTimeline } from '@/components/common/ApprovalTimeline';
import { useApprovalRequest } from '@/modules/approvals/hooks';
import { downloadApprovalRequestPdf } from '@/api/exportsApi';
import { useNotificationStore } from '@/app/store/notificationStore';
import { extractErrorMessage } from '@/api/client';
import { formatDate } from '@/utils/format';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Grid item xs={12} sm={6}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography>{value ?? '—'}</Typography>
    </Grid>
  );
}

interface Props {
  requestId: number | null;
  open: boolean;
  onClose: () => void;
}

export function ApprovalRequestDialog({ requestId, open, onClose }: Props) {
  const { data, isLoading } = useApprovalRequest(open && requestId ? requestId : undefined);
  const notifyError = useNotificationStore((s) => s.notifyError);
  const notifySuccess = useNotificationStore((s) => s.notifySuccess);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle sx={{ pr: 6 }}>
        {data ? `Demande ${data.requestRef}` : "Demande d'approbation"}
        {data && (
          <Typography variant="caption" color="text.secondary" display="block">
            {data.targetType} sur {data.targetEntityType} #{data.targetEntityId}
          </Typography>
        )}
      </DialogTitle>
      <DialogContent dividers>
        {isLoading || !data ? <LoadingScreen /> : (
          <Box>
            <Grid container spacing={2}>
              <Field label="Statut" value={<StatusBadge value={data.status} />} />
              <Field label="Règle" value={data.approvalRuleCode} />
              <Field label="Caisse" value={data.cashRegisterCode ?? '—'} />
              <Field label="Montant" value={<CurrencyDisplay value={data.amount ?? undefined} currency={data.currencyCode ?? 'XOF'} />} />
              <Field label="Demandée par" value={data.requestedByName} />
              <Field label="Demandée le" value={formatDate(data.requestedAt, true)} />
              <Field label="Décidée par" value={data.decidedByName} />
              <Field label="Décidée le" value={formatDate(data.decidedAt, true)} />
              <Grid item xs={12}>
                <Typography variant="caption" color="text.secondary">Motif de la demande</Typography>
                <Typography sx={{ whiteSpace: 'pre-line' }}>{data.reason}</Typography>
              </Grid>
              {data.decisionComment && (
                <Grid item xs={12}>
                  <Typography variant="caption" color="text.secondary">Commentaire de décision</Typography>
                  <Typography sx={{ whiteSpace: 'pre-line' }}>{data.decisionComment}</Typography>
                </Grid>
              )}
            </Grid>

            {data.actions && data.actions.length > 0 && (
              <Box mt={3}>
                <Typography variant="subtitle2" gutterBottom>Historique</Typography>
                <ApprovalTimeline actions={data.actions} />
              </Box>
            )}
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        {data && (
          <Button
            startIcon={<ReceiptLongIcon />}
            onClick={async () => {
              try {
                await downloadApprovalRequestPdf(data.id);
                notifySuccess('PDF généré.');
              } catch (e) {
                notifyError(extractErrorMessage(e));
              }
            }}
          >
            Imprimer PDF
          </Button>
        )}
        <Button onClick={onClose} variant="contained">Fermer</Button>
      </DialogActions>
    </Dialog>
  );
}
