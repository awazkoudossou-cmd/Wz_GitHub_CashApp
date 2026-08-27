import { Box, Stack, Typography } from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import HourglassEmptyIcon from '@mui/icons-material/HourglassEmpty';
import EditIcon from '@mui/icons-material/Edit';
import type { ApprovalAction } from '@/types';
import { formatDate } from '@/utils/format';

interface Props {
  actions: ApprovalAction[];
}

function iconFor(action: string) {
  switch (action) {
    case 'APPROVE': return <CheckCircleIcon color="success" />;
    case 'REJECT': return <CancelIcon color="error" />;
    case 'CANCEL': return <CancelIcon color="disabled" />;
    case 'UPDATE': return <EditIcon color="action" />;
    default: return <HourglassEmptyIcon color="action" />;
  }
}

export function ApprovalTimeline({ actions }: Props) {
  if (!actions.length) {
    return (
      <Typography variant="body2" color="text.secondary">
        Aucune action enregistrée.
      </Typography>
    );
  }
  return (
    <Stack spacing={2}>
      {actions.map((a) => (
        <Box key={a.id} sx={{ display: 'flex', gap: 1.5 }}>
          <Box>{iconFor(a.actionType)}</Box>
          <Box sx={{ flex: 1 }}>
            <Typography variant="body2">
              <b>{a.performedByName}</b> — {a.actionType}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {formatDate(a.performedAt, true)}
            </Typography>
            {a.comment && (
              <Typography variant="body2" sx={{ mt: 0.5 }}>
                {a.comment}
              </Typography>
            )}
          </Box>
        </Box>
      ))}
    </Stack>
  );
}
