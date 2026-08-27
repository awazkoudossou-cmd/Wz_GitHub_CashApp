import { Card, CardContent, Divider, Stack, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { CurrencyDisplay } from '@/components/common/CurrencyDisplay';
import type { CashSessionWidget } from '@/types';
import { formatDate } from '@/utils/format';

interface Props {
  sessions: CashSessionWidget[];
}

export function OpenSessionsList({ sessions }: Props) {
  const navigate = useNavigate();
  return (
    <Card sx={{ height: '100%' }}>
      <CardContent>
        <Typography variant="subtitle1" fontWeight={600} gutterBottom>
          Sessions ouvertes ({sessions.length})
        </Typography>
        {sessions.length === 0 ? (
          <Typography variant="body2" color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
            Aucune session ouverte actuellement.
          </Typography>
        ) : (
          <Stack divider={<Divider />} spacing={1}>
            {sessions.map((s) => (
              <Stack
                key={s.id}
                direction="row"
                justifyContent="space-between"
                alignItems="center"
                py={0.5}
                sx={{ cursor: 'pointer' }}
                onClick={() => navigate(`/cash-sessions/${s.id}`)}
              >
                <Stack>
                  <Typography variant="body2" fontWeight={500}>{s.cashRegisterName}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    Ouverte le {formatDate(s.openedAt, true)} · {s.operationCount} opération(s)
                  </Typography>
                </Stack>
                <Typography variant="body2" fontWeight={600}>
                  <CurrencyDisplay value={s.currentTheoreticalBalance} />
                </Typography>
              </Stack>
            ))}
          </Stack>
        )}
      </CardContent>
    </Card>
  );
}
