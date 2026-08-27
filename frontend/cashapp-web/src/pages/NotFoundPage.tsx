import { Button, Stack, Typography } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';

export function NotFoundPage() {
  return (
    <PageContainer maxWidth="sm">
      <Stack alignItems="center" spacing={2} mt={6}>
        <Typography variant="h3" color="text.disabled">404</Typography>
        <Typography variant="h6">Page introuvable</Typography>
        <Button component={RouterLink} to="/dashboard" variant="contained">Retour au tableau de bord</Button>
      </Stack>
    </PageContainer>
  );
}
