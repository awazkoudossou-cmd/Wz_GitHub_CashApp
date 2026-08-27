import { Card, CardContent, Typography, useTheme } from '@mui/material';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import type { AccountingDailyCount } from '@/types';
import { formatDate, formatNumber } from '@/utils/format';

interface Props {
  title: string;
  data: AccountingDailyCount[];
  height?: number;
}

// Graphique en barres générique jour par jour (écritures, générations…).
export function AccountingDailyBarChart({ title, data, height = 240 }: Props) {
  const theme = useTheme();
  const chartData = data.map((d) => ({ date: formatDate(d.date), count: d.count }));
  const hasActivity = data.some((d) => d.count > 0);

  return (
    <Card sx={{ height: '100%', overflow: 'hidden' }}>
      <CardContent sx={{ minWidth: 0 }}>
        <Typography variant="subtitle1" fontWeight={600} gutterBottom>{title}</Typography>
        {!hasActivity ? (
          <Typography variant="body2" color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
            Aucune activité sur cette période.
          </Typography>
        ) : (
          <ResponsiveContainer width="100%" height={height}>
            <BarChart data={chartData} margin={{ top: 4, right: 8, left: -12, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke={theme.palette.divider} />
              <XAxis dataKey="date" tick={{ fontSize: 11 }} stroke={theme.palette.text.secondary} />
              <YAxis tick={{ fontSize: 11 }} stroke={theme.palette.text.secondary} allowDecimals={false} tickFormatter={(v) => formatNumber(v as number)} />
              <Tooltip formatter={(value) => formatNumber(Number(value))} contentStyle={{ borderRadius: 8, fontSize: 12 }} />
              <Bar dataKey="count" name="Nombre" fill={theme.palette.primary.main} radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        )}
      </CardContent>
    </Card>
  );
}
