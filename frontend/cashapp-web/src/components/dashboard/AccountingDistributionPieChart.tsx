import { Card, CardContent, Typography, useTheme } from '@mui/material';
import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts';
import type { AccountingNamedCount } from '@/types';
import { formatNumber } from '@/utils/format';

interface Props {
  title: string;
  data: AccountingNamedCount[];
  height?: number;
}

const PALETTE = ['#1f4e8f', '#16a34a', '#f59e0b', '#dc2626', '#7c3aed', '#0d9488', '#db2777', '#64748b', '#94a3b8'];

// Camembert générique nom/compte (répartition journaux, répartition comptes…).
export function AccountingDistributionPieChart({ title, data, height = 240 }: Props) {
  const theme = useTheme();
  const chartData = data.map((d) => ({ name: d.name, value: d.count }));

  return (
    <Card sx={{ height: '100%', overflow: 'hidden' }}>
      <CardContent sx={{ minWidth: 0 }}>
        <Typography variant="subtitle1" fontWeight={600} gutterBottom>{title}</Typography>
        {chartData.length === 0 ? (
          <Typography variant="body2" color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
            Aucune écriture générée pour le moment.
          </Typography>
        ) : (
          <ResponsiveContainer width="100%" height={height}>
            <PieChart>
              <Pie data={chartData} dataKey="value" nameKey="name" cx="50%" cy="50%"
                innerRadius={45} outerRadius={80} paddingAngle={2}>
                {chartData.map((_, i) => <Cell key={i} fill={PALETTE[i % PALETTE.length]} />)}
              </Pie>
              <Tooltip formatter={(value) => formatNumber(Number(value))} contentStyle={{ borderRadius: 8, fontSize: 12 }} />
              <Legend layout="vertical" verticalAlign="middle" align="right"
                wrapperStyle={{ fontSize: 11, color: theme.palette.text.secondary }} />
            </PieChart>
          </ResponsiveContainer>
        )}
      </CardContent>
    </Card>
  );
}
