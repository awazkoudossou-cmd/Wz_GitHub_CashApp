import { Box, Container } from '@mui/material';
import type { ReactNode } from 'react';

interface Props {
  children: ReactNode;
}

export function AuthLayout({ children }: Props) {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        bgcolor: 'background.default'
      }}
    >
      <Container maxWidth="sm">{children}</Container>
    </Box>
  );
}
