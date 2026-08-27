import { Card, CardContent, Typography, useTheme } from '@mui/material';
import {
  Area, CartesianGrid, ComposedChart, Legend, Line, ResponsiveContainer, Tooltip, XAxis, YAxis
} from 'recharts';
import type { DailyTrendPoint } from '@/types';
import { formatDate, formatNumber } from '@/utils/format';

interface Props {
  title: string;
  data: DailyTrendPoint[];
  height?: number;
}

// Graphique en aires miroir (entrées au-dessus de zéro, sorties en dessous) avec une courbe
// de solde net superposée — donne une vue "illustrative" de l'activité jour par jour.
export function TrendAreaChart({ title, data, height = 260 }: Props) {
  const theme = useTheme();
  const chartData = data.map((d) => ({
    date: formatDate(d.date),
    Entrées: d.totalIn,
    Sorties: -d.totalOut,
    Net: d.net
  }));

  return (
    <Card sx={{ height: '100%', overflow: 'hidden' }}>
      <CardContent sx={{ minWidth: 0 }}>
        <Typography variant="subtitle1" fontWeight={600} gutterBottom>{title}</Typography>
        {data.every((d) => d.totalIn === 0 && d.totalOut === 0) ? (
          <Typography variant="body2" color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
            Aucune activité sur cette période.
          </Typography>
        ) : (
          <ResponsiveContainer width="100%" height={height}>
            <ComposedChart data={chartData} margin={{ top: 4, right: 8, left: -12, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke={theme.palette.divider} />
              <XAxis dataKey="date" tick={{ fontSize: 11 }} stroke={theme.palette.text.secondary} />
              <YAxis tick={{ fontSize: 11 }} stroke={theme.palette.text.secondary}
                tickFormatter={(v) => formatNumber(Math.abs(v as number))} />
              <Tooltip
                formatter={(value, name) => [formatNumber(Math.abs(Number(value))), String(name)]}
                contentStyle={{ borderRadius: 8, fontSize: 12 }}
              />
              <Legend wrapperStyle={{ fontSize: 12 }} />
              <Area type="monotone" dataKey="Entrées" stroke={theme.palette.success.main}
                fill={theme.palette.success.main} fillOpacity={0.25} strokeWidth={2} />
              <Area type="monotone" dataKey="Sorties" stroke={theme.palette.error.main}
                fill={theme.palette.error.main} fillOpacity={0.25} strokeWidth={2} />
              <Line type="monotone" dataKey="Net" stroke={theme.palette.primary.main} strokeWidth={2.5} dot={{ r: 3 }} />
            </ComposedChart>
          </ResponsiveContainer>
        )}
      </CardContent>
    </Card>
  );
}
