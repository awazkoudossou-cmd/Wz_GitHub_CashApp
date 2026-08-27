import { Card, CardContent, Divider, Stack, Typography } from '@mui/material';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import { StatusChip } from '@/components/common/StatusChip';
import type { OperationWidget } from '@/types';
import { formatDate } from '@/utils/format';

interface Props {
  title: string;
  operations: OperationWidget[];
}

export function RecentOperationsList({ title, operations }: Props) {
  return (
    <Card sx={{ height: '100%' }}>
      <CardContent>
        <Typography variant="subtitle1" fontWeight={600} gutterBottom>{title}</Typography>
        {operations.length === 0 ? (
          <Typography variant="body2" color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
            Aucune opération récente.
          </Typography>
        ) : (
          <Stack divider={<Divider />} spacing={1}>
            {operations.map((o) => (
              <Stack key={o.id} direction="row" justifyContent="space-between" alignItems="center" py={0.5}>
                <Stack>
                  <Typography variant="body2" fontWeight={500}>{o.label}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {o.operationRef} · {o.categoryLabel} · {formatDate(o.operationDate, true)}
                  </Typography>
                </Stack>
                <Stack direction="row" spacing={1} alignItems="center">
                  <StatusChip status={o.direction} variant="direction" />
                  <Typography variant="body2" fontWeight={600}>
                    <CurrencyDisplay value={o.amount} />
                  </Typography>
                </Stack>
              </Stack>
            ))}
          </Stack>
        )}
      </CardContent>
    </Card>
  );
}
