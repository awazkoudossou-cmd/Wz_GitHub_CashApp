import { Card, CardActionArea, CardContent, Stack, Typography } from '@mui/material';
import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';

interface Props {
  icon: ReactNode;
  label: string;
  count: number;
  hint: string;
  to: string;
  color: 'primary' | 'warning' | 'error';
}

// Carte "action à traiter" cliquable — met en avant un compteur (demandes en attente,
// écarts non justifiés…) et renvoie directement vers la liste filtrée correspondante.
export function ActionWidget({ icon, label, count, hint, to, color }: Props) {
  const navigate = useNavigate();
  return (
    <Card sx={{ height: '100%', borderLeft: 4, borderColor: `${color}.main` }}>
      <CardActionArea onClick={() => navigate(to)} sx={{ height: '100%' }}>
        <CardContent>
          <Stack direction="row" spacing={1.5} alignItems="center">
            <Stack sx={{ color: `${color}.main` }}>{icon}</Stack>
            <Stack flex={1}>
              <Typography variant="h5" fontWeight={700} sx={{ color: count > 0 ? `${color}.main` : 'text.primary' }}>
                {count}
              </Typography>
              <Typography variant="body2" color="text.secondary">{label}</Typography>
            </Stack>
          </Stack>
          <Typography variant="caption" color="text.secondary" display="block" mt={1}>{hint}</Typography>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
