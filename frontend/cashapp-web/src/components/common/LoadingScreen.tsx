import { Box, CircularProgress, Typography } from '@mui/material';

interface Props {
  message?: string;
  fullScreen?: boolean;
}

export function LoadingScreen({ message = 'Chargement…', fullScreen = false }: Props) {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 2,
        py: 6,
        minHeight: fullScreen ? '100vh' : undefined
      }}
    >
      <CircularProgress />
      <Typography variant="body2" color="text.secondary">
        {message}
      </Typography>
    </Box>
  );
}
