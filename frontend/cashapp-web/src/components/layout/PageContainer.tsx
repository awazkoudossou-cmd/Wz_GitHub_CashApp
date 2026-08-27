import { Container } from '@mui/material';
import type { ReactNode } from 'react';

interface Props {
  children: ReactNode;
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | false;
}

export function PageContainer({ children, maxWidth = 'xl' }: Props) {
  return <Container maxWidth={maxWidth} sx={{ py: 3 }}>{children}</Container>;
}
