import { Alert, Button, Card, CardContent, Grid, MenuItem, Stack, TextField } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { useAuthStore } from '@/app/store/authStore';
import { useCreateBankDeposit } from '@/modules/bank-deposits/hooks';
import { extractErrorMessage } from '@/api/client';

export function BankDepositFormPage() {
  const navigate = useNavigate();
  const registers = useAuthStore((s) => s.cashRegisters);
  const create = useCreateBankDeposit();

  const [cashRegisterId, setCashRegisterId] = useState<number | ''>(registers[0]?.id ?? '');
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('XOF');
  const [bankName, setBankName] = useState('');
  const [accountRef, setAccountRef] = useState('');
  const [slipRef, setSlipRef] = useState('');
  const [description, setDescription] = useState('');

  const submit = async () => {
    try {
      const r = await create.mutateAsync({
        cashRegisterId: Number(cashRegisterId),
        depositDate: date,
        amount: Number(amount),
        currencyCode: currency,
        bankName,
        accountReference: accountRef || undefined,
        depositSlipReference: slipRef || undefined,
        description: description || undefined
      });
      navigate(`/bank-deposits/${r.id}`);
    } catch (e) { alert(extractErrorMessage(e)); }
  };

  return (
    <PageContainer maxWidth="md">
      <PageHeader title="Nouveau dépôt banque" />
      <Card>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <TextField select fullWidth size="small" label="Caisse source" value={cashRegisterId} onChange={(e) => setCashRegisterId(Number(e.target.value))}>
                {registers.map((r) => <MenuItem key={r.id} value={r.id}>{r.code} — {r.name}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={3}>
              <TextField fullWidth size="small" type="date" label="Date" InputLabelProps={{ shrink: true }} value={date} onChange={(e) => setDate(e.target.value)} />
            </Grid>
            <Grid item xs={12} sm={3}>
              <TextField fullWidth size="small" type="number" label="Montant" value={amount} onChange={(e) => setAmount(e.target.value)} inputProps={{ step: '0.01', min: 0 }} />
            </Grid>
            <Grid item xs={6} sm={3}><TextField fullWidth size="small" label="Devise" value={currency} onChange={(e) => setCurrency(e.target.value)} /></Grid>
            <Grid item xs={12} sm={6}><TextField fullWidth size="small" label="Banque" value={bankName} onChange={(e) => setBankName(e.target.value)} /></Grid>
            <Grid item xs={12} sm={6}><TextField fullWidth size="small" label="N° compte" value={accountRef} onChange={(e) => setAccountRef(e.target.value)} /></Grid>
            <Grid item xs={12} sm={6}><TextField fullWidth size="small" label="Réf. bordereau" value={slipRef} onChange={(e) => setSlipRef(e.target.value)} /></Grid>
            <Grid item xs={12}><TextField fullWidth multiline rows={2} size="small" label="Description" value={description} onChange={(e) => setDescription(e.target.value)} /></Grid>
            {create.isError && <Grid item xs={12}><Alert severity="error">{extractErrorMessage(create.error)}</Alert></Grid>}
            <Grid item xs={12}>
              <Stack direction="row" spacing={2}>
                <Button variant="contained" disabled={create.isPending || !cashRegisterId || !amount || !bankName} onClick={submit}>Créer</Button>
                <Button onClick={() => navigate('/bank-deposits')}>Annuler</Button>
              </Stack>
            </Grid>
          </Grid>
        </CardContent>
      </Card>
    </PageContainer>
  );
}
