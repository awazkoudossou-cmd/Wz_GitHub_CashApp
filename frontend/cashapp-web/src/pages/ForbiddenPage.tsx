import { Button, Stack, Typography } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';

export function ForbiddenPage() {
  return (
    <PageContainer maxWidth="sm">
      <Stack alignItems="center" spacing={2} mt={6}>
        <Typography variant="h3" color="warning.main">403</Typography>
        <Typography variant="h6">Accès refusé</Typography>
        <Typography variant="body2" color="text.secondary">
          Cette section n'est pas disponible pour votre rôle ou le module est désactivé.
        </Typography>
        <Button component={RouterLink} to="/dashboard" variant="contained">Retour au tableau de bord</Button>
      </Stack>
    </PageContainer>
  );
}
