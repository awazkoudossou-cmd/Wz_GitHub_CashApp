import { useRef, useState } from 'react';
import {
  Alert,
  Button,
  Card,
  CardContent,
  Grid,
  MenuItem,
  Stack,
  TextField
} from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { AppTable, type Column } from '@/components/tables/AppTable';
import { StatusBadge } from '@/components/common/StatusBadge';
import { useImports, useUploadImport } from '@/modules/imports-v2/hooks';
import { useAuthStore } from '@/app/store/authStore';
import { ImportBatchType } from '@/types/v2Enums';
import { formatDate } from '@/utils/format';
import { extractErrorMessage } from '@/api/client';
import type { ImportBatchListItem } from '@/types';

const IMPORT_FORMAT_HINTS: Record<keyof typeof ImportBatchType, string> = {
  CASH_OPERATIONS: "Colonnes : date, direction (IN/OUT), category_code, amount, payment_method, label, [description], [third_party].",
  CATEGORIES: "Colonnes : code, label, direction (IN/OUT), [group], [is_active]. Sans groupe précisé, la catégorie est classée dans « Non classé ». Une catégorie déjà existante (ou dupliquée dans le fichier) est rejetée.",
  ACCOUNTING_ACCOUNTS: "Colonnes : account_number, name, nature (ASSET/LIABILITY/EXPENSE/REVENUE/BANK/CASH/EQUITY/OTHER), [is_active]. Un compte déjà présent dans le Plan Comptable (ou dupliqué dans le fichier) est rejeté."
};

export function ImportsListPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const registers = useAuthStore((s) => s.cashRegisters);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const presetType = searchParams.get('type');
  const [batchType, setBatchType] = useState<keyof typeof ImportBatchType>(
    presetType && presetType in ImportBatchType ? (presetType as keyof typeof ImportBatchType) : 'CASH_OPERATIONS'
  );
  const [cashRegisterId, setCashRegisterId] = useState<number | ''>(registers[0]?.id ?? '');
  const fileRef = useRef<HTMLInputElement>(null);
  const upload = useUploadImport();

  const { data, isLoading } = useImports(page, pageSize);

  const onUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      const r = await upload.mutateAsync({ batchType, file, cashRegisterId: cashRegisterId ? Number(cashRegisterId) : undefined });
      navigate(`/imports/${r.id}`);
    } catch (err) { alert(extractErrorMessage(err)); }
    finally { if (fileRef.current) fileRef.current.value = ''; }
  };

  const columns: Column<ImportBatchListItem>[] = [
    { key: 'ref', header: 'Référence', render: (r) => r.batchRef },
    { key: 'type', header: 'Type', render: (r) => r.batchType },
    { key: 'file', header: 'Fichier', render: (r) => r.originalFileName },
    { key: 'date', header: 'Téléversé le', render: (r) => formatDate(r.uploadedAt, true) },
    { key: 'user', header: 'Par', render: (r) => r.uploadedByName },
    { key: 'lines', header: 'Lignes', align: 'right', render: (r) => `${r.validLines} OK / ${r.invalidLines} KO / ${r.totalLines}` },
    { key: 'imported', header: 'Importées', align: 'right', render: (r) => r.importedLines },
    { key: 'status', header: 'Statut', render: (r) => <StatusBadge value={r.status} /> }
  ];

  return (
    <PageContainer>
      <PageHeader title="Imports Excel / CSV" />
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} sm={3}>
              <TextField select fullWidth size="small" label="Type" value={batchType} onChange={(e) => setBatchType(e.target.value as keyof typeof ImportBatchType)}>
                {Object.values(ImportBatchType).map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
              </TextField>
            </Grid>
            {batchType === 'CASH_OPERATIONS' && (
              <Grid item xs={12} sm={4}>
                <TextField select fullWidth size="small" label="Caisse cible" value={cashRegisterId} onChange={(e) => setCashRegisterId(Number(e.target.value))}>
                  {registers.map((r) => <MenuItem key={r.id} value={r.id}>{r.code} — {r.name}</MenuItem>)}
                </TextField>
              </Grid>
            )}
            <Grid item xs={12} sm="auto">
              <Stack direction="row" spacing={1}>
                <Button variant="contained" startIcon={<CloudUploadIcon />} onClick={() => fileRef.current?.click()} disabled={upload.isPending}>
                  Téléverser
                </Button>
                <input ref={fileRef} type="file" hidden accept=".csv,.xlsx,.xls" onChange={onUpload} />
              </Stack>
            </Grid>
            <Grid item xs={12}>
              <Alert severity="info">{IMPORT_FORMAT_HINTS[batchType]}</Alert>
            </Grid>
          </Grid>
        </CardContent>
      </Card>
      <AppTable
        columns={columns} rows={data?.items} rowKey={(r) => r.id} isLoading={isLoading}
        onRowClick={(r) => navigate(`/imports/${r.id}`)}
        maxHeight="calc(100vh - 360px)"
        pagination={{ page, pageSize, total: data?.totalCount ?? 0, onPageChange: setPage, onPageSizeChange: (s) => { setPageSize(s); setPage(1); } }}
      />
    </PageContainer>
  );
}
