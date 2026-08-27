import { Alert, Button, Card, CardContent, Grid, MenuItem, Stack, TextField } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { useAuthStore } from '@/app/store/authStore';
import { useCreateCashTransfer } from '@/modules/cash-transfers-v2/hooks';
import { extractErrorMessage } from '@/api/client';

export function CashTransferFormPage() {
  const navigate = useNavigate();
  const registers = useAuthStore((s) => s.cashRegisters);
  const create = useCreateCashTransfer();

  const [source, setSource] = useState<number | ''>('');
  const [dest, setDest] = useState<number | ''>('');
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('XOF');
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [reason, setReason] = useState('');

  const submit = async () => {
    try {
      const r = await create.mutateAsync({
        sourceCashRegisterId: Number(source),
        destinationCashRegisterId: Number(dest),
        amount: Number(amount),
        currencyCode: currency,
        transferDate: date,
        reason
      });
      navigate(`/cash-transfers/${r.id}`);
    } catch (e) { alert(extractErrorMessage(e)); }
  };

  const canSubmit = source && dest && source !== dest && amount && reason;

  return (
    <PageContainer maxWidth="md">
      <PageHeader title="Nouveau transfert inter-caisses" />
      <Card>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <TextField select fullWidth size="small" label="Caisse source" value={source} onChange={(e) => setSource(Number(e.target.value))}>
                {registers.map((r) => <MenuItem key={r.id} value={r.id}>{r.code} — {r.name}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField select fullWidth size="small" label="Caisse destination" value={dest} onChange={(e) => setDest(Number(e.target.value))}>
                {registers.filter((r) => r.id !== source).map((r) => <MenuItem key={r.id} value={r.id}>{r.code} — {r.name}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={4}>
              <TextField fullWidth size="small" type="number" label="Montant" value={amount} onChange={(e) => setAmount(e.target.value)} inputProps={{ step: '0.01', min: 0 }} />
            </Grid>
            <Grid item xs={12} sm={4}>
              <TextField fullWidth size="small" label="Devise" value={currency} onChange={(e) => setCurrency(e.target.value)} />
            </Grid>
            <Grid item xs={12} sm={4}>
              <TextField fullWidth size="small" type="date" label="Date" InputLabelProps={{ shrink: true }} value={date} onChange={(e) => setDate(e.target.value)} />
            </Grid>
            <Grid item xs={12}>
              <TextField fullWidth size="small" multiline rows={2} label="Motif" value={reason} onChange={(e) => setReason(e.target.value)} />
            </Grid>
            {create.isError && <Grid item xs={12}><Alert severity="error">{extractErrorMessage(create.error)}</Alert></Grid>}
            <Grid item xs={12}>
              <Stack direction="row" spacing={2}>
                <Button variant="contained" disabled={!canSubmit || create.isPending} onClick={submit}>Créer</Button>
                <Button onClick={() => navigate('/cash-transfers')}>Annuler</Button>
              </Stack>
            </Grid>
          </Grid>
        </CardContent>
      </Card>
    </PageContainer>
  );
}
