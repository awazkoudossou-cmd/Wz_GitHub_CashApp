import {
  Box,
  Button,
  Card,
  CardContent,
  IconButton,
  List,
  ListItem,
  ListItemSecondaryAction,
  ListItemText,
  Stack,
  Tooltip,
  Typography
} from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import DownloadIcon from '@mui/icons-material/Download';
import DeleteIcon from '@mui/icons-material/Delete';
import AttachFileIcon from '@mui/icons-material/AttachFile';
import { useRef, useState } from 'react';
import {
  useAttachments,
  useDeleteAttachment,
  useUploadAttachment
} from '@/modules/attachments/hooks';
import { attachmentsApi } from '@/api/attachmentsApi';
import { useIsFeatureEnabled } from '@/hooks/useFeatures';
import { FeatureCodes } from '@/types/enums';
import { formatDate } from '@/utils/format';
import { extractErrorMessage } from '@/api/client';

interface Props {
  entityType: string;
  entityId: number;
  canUpload?: boolean;
  canDelete?: boolean;
  title?: string;
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} o`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} Ko`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} Mo`;
}

export function AttachmentPanel({
  entityType,
  entityId,
  canUpload = true,
  canDelete = true,
  title = 'Pièces jointes'
}: Props) {
  const enabled = useIsFeatureEnabled(FeatureCodes.ADV_ATTACHMENTS);
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const { data, isLoading } = useAttachments(entityType, enabled ? entityId : undefined);
  const upload = useUploadAttachment(entityType, entityId);
  const del = useDeleteAttachment(entityType, entityId);

  if (!enabled) return null;

  const onPick = () => inputRef.current?.click();

  const onChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setBusy(true);
    try {
      await upload.mutateAsync({ file });
    } catch (err) {
      alert(extractErrorMessage(err));
    } finally {
      setBusy(false);
      if (inputRef.current) inputRef.current.value = '';
    }
  };

  return (
    <Card sx={{ mt: 2 }}>
      <CardContent>
        <Stack direction="row" alignItems="center" justifyContent="space-between" mb={1}>
          <Typography variant="subtitle1" fontWeight={600}>
            {title}
          </Typography>
          {canUpload && (
            <Button startIcon={<CloudUploadIcon />} size="small" onClick={onPick} disabled={busy}>
              {busy ? 'Envoi…' : 'Téléverser'}
            </Button>
          )}
        </Stack>
        <input type="file" hidden ref={inputRef} onChange={onChange} />

        {isLoading && <Typography variant="body2" color="text.secondary">Chargement…</Typography>}
        {data && data.length === 0 && (
          <Typography variant="body2" color="text.secondary">Aucune pièce jointe.</Typography>
        )}
        {data && data.length > 0 && (
          <List dense disablePadding>
            {data.map((a) => (
              <ListItem key={a.id} divider>
                <Box sx={{ display: 'flex', alignItems: 'center', mr: 1 }}>
                  <AttachFileIcon fontSize="small" color="action" />
                </Box>
                <ListItemText
                  primary={a.originalFileName}
                  secondary={`${formatSize(a.fileSize)} — ${formatDate(a.uploadedAt, true)} — ${a.uploadedByName}`}
                />
                <ListItemSecondaryAction>
                  <Tooltip title="Télécharger">
                    <IconButton
                      size="small"
                      onClick={() => attachmentsApi.download(a.id, a.originalFileName)}
                    >
                      <DownloadIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  {canDelete && (
                    <Tooltip title="Supprimer">
                      <IconButton
                        size="small"
                        color="error"
                        onClick={() => {
                          if (window.confirm(`Supprimer ${a.originalFileName} ?`)) del.mutate(a.id);
                        }}
                      >
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}
                </ListItemSecondaryAction>
              </ListItem>
            ))}
          </List>
        )}
      </CardContent>
    </Card>
  );
}
