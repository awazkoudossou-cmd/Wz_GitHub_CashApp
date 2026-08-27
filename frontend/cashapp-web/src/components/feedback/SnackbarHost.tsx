import { Alert, Snackbar, Stack } from '@mui/material';
import { useNotificationStore } from '@/app/store/notificationStore';

// Affiche en pile (top-right) les notifications du store.
export function SnackbarHost() {
  const notifications = useNotificationStore((s) => s.notifications);
  const dismiss = useNotificationStore((s) => s.dismiss);

  return (
    <Stack
      sx={{ position: 'fixed', top: 80, right: 16, zIndex: (t) => t.zIndex.snackbar, gap: 1, pointerEvents: 'none' }}
    >
      {notifications.map((n) => (
        <Snackbar
          key={n.id}
          open
          autoHideDuration={n.durationMs ?? 4000}
          onClose={(_, reason) => { if (reason !== 'clickaway') dismiss(n.id); }}
          anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
          sx={{ position: 'static', transform: 'none', pointerEvents: 'auto' }}
        >
          <Alert
            onClose={() => dismiss(n.id)}
            severity={n.severity}
            variant="filled"
            sx={{ minWidth: 280, boxShadow: 3 }}
          >
            {n.message}
          </Alert>
        </Snackbar>
      ))}
    </Stack>
  );
}
