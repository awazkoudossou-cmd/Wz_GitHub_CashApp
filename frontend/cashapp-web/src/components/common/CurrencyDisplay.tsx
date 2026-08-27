import { formatCurrency } from '@/utils/format';

interface Props {
  value: number | null | undefined;
  currency?: string;
}

export function CurrencyDisplay({ value, currency = 'XOF' }: Props) {
  return <span>{formatCurrency(value, currency)}</span>;
}
