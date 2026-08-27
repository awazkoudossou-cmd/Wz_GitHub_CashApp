import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Box, Button, Card, CardContent, Stack, TextField, Typography } from '@mui/material';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { AuthLayout } from '@/components/layout/AuthLayout';
import { useLogin } from '@/hooks/useAuth';
import { extractErrorMessage } from '@/api/client';

const schema = z.object({
  username: z.string().min(1, 'Requis'),
  password: z.string().min(1, 'Requis')
});

type FormValues = z.infer<typeof schema>;

export function LoginPage() {
  const navigate = useNavigate();
  const login = useLogin();
  const { register, handleSubmit, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { username: '', password: '' }
  });

  const onSubmit = handleSubmit(async (values) => {
    await login.mutateAsync(values);
    navigate('/dashboard', { replace: true });
  });

  return (
    <AuthLayout>
      <Card>
        <CardContent sx={{ p: 4 }}>
          <Box mb={3}>
            <Typography variant="h5" color="primary" gutterBottom>CashApp</Typography>
            <Typography variant="body2" color="text.secondary">Connectez-vous pour continuer.</Typography>
          </Box>
          <form onSubmit={onSubmit}>
            <Stack spacing={2}>
              <TextField
                label="Nom d'utilisateur" size="small" fullWidth autoFocus
                {...register('username')}
                error={!!formState.errors.username}
                helperText={formState.errors.username?.message}
              />
              <TextField
                label="Mot de passe" type="password" size="small" fullWidth
                {...register('password')}
                error={!!formState.errors.password}
                helperText={formState.errors.password?.message}
              />
              {login.isError && (
                <Alert severity="error">{extractErrorMessage(login.error)}</Alert>
              )}
              <Button type="submit" variant="contained" size="large" disabled={login.isPending}>
                {login.isPending ? 'Connexion…' : 'Se connecter'}
              </Button>
            </Stack>
          </form>
        </CardContent>
      </Card>
    </AuthLayout>
  );
}
