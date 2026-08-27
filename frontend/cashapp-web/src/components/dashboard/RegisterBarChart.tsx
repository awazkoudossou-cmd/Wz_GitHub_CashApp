import { Card, CardContent, Typography, useTheme } from '@mui/material';
import { Bar, BarChart, CartesianGrid, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import type { RegisterBreakdown } from '@/types';
import { formatNumber } from '@/utils/format';

interface Props {
  title: string;
  data: RegisterBreakdown[];
  height?: number;
}

export function RegisterBarChart({ title, data, height = 260 }: Props) {
  const theme = useTheme();
  const chartData = data.map((d) => ({ name: d.cashRegisterCode, net: d.net, count: d.operationCount }));

  return (
    <Card sx={{ height: '100%', overflow: 'hidden' }}>
      <CardContent sx={{ minWidth: 0 }}>
        <Typography variant="subtitle1" fontWeight={600} gutterBottom>{title}</Typography>
        {chartData.length === 0 ? (
          <Typography variant="body2" color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
            Aucune activité aujourd'hui.
          </Typography>
        ) : (
          <ResponsiveContainer width="100%" height={height}>
            <BarChart data={chartData} margin={{ top: 4, right: 8, left: -12, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke={theme.palette.divider} />
              <XAxis dataKey="name" tick={{ fontSize: 11 }} stroke={theme.palette.text.secondary} />
              <YAxis tick={{ fontSize: 11 }} stroke={theme.palette.text.secondary} tickFormatter={(v) => formatNumber(v as number)} />
              <Tooltip formatter={(value) => formatNumber(Number(value))} contentStyle={{ borderRadius: 8, fontSize: 12 }} />
              <Bar dataKey="net" radius={[4, 4, 0, 0]}>
                {chartData.map((d, i) => (
                  <Cell key={i} fill={d.net >= 0 ? theme.palette.success.main : theme.palette.error.main} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        )}
      </CardContent>
    </Card>
  );
}
