import { Chip } from '@mui/material';

interface Props {
  status: string;
  variant?: 'session' | 'active' | 'direction';
}

const sessionColor: Record<string, 'success' | 'default'> = {
  OPEN: 'success',
  CLOSED: 'default'
};

const directionColor: Record<string, 'success' | 'warning'> = {
  IN: 'success',
  OUT: 'warning'
};

export function StatusChip({ status, variant = 'session' }: Props) {
  let color: 'success' | 'warning' | 'default' = 'default';
  let label = status;
  if (variant === 'session') color = sessionColor[status] ?? 'default';
  if (variant === 'direction') {
    color = directionColor[status] ?? 'default';
    label = status === 'IN' ? 'Entrée' : status === 'OUT' ? 'Sortie' : status;
  }
  if (variant === 'active') {
    color = status === 'true' ? 'success' : 'default';
    label = status === 'true' ? 'Actif' : 'Inactif';
  }
  return <Chip label={label} color={color} size="small" />;
}
