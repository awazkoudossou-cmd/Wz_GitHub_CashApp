import {
  FormControlLabel,
  MenuItem,
  Switch,
  TextField,
  type TextFieldProps
} from '@mui/material';
import { Controller, useFormContext } from 'react-hook-form';
import type { ReactNode } from 'react';

interface BaseProps {
  name: string;
  label: string;
}

type AppTextFieldProps = BaseProps & Omit<TextFieldProps, 'name' | 'label'>;

export function AppTextField({ name, label, ...rest }: AppTextFieldProps) {
  const { control } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...field}
          {...rest}
          label={label}
          fullWidth
          size="small"
          value={field.value ?? ''}
          error={!!fieldState.error}
          helperText={fieldState.error?.message ?? rest.helperText}
        />
      )}
    />
  );
}

interface SelectOption {
  value: string | number;
  label: string;
}

interface AppSelectProps extends BaseProps {
  options: SelectOption[];
  disabled?: boolean;
  helperText?: string;
  children?: ReactNode;
}

export function AppSelect({ name, label, options, disabled, helperText }: AppSelectProps) {
  const { control } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          select
          fullWidth
          size="small"
          label={label}
          value={field.value ?? ''}
          onChange={field.onChange}
          disabled={disabled}
          error={!!fieldState.error}
          helperText={fieldState.error?.message ?? helperText}
        >
          {options.map((opt) => (
            <MenuItem key={opt.value} value={opt.value}>
              {opt.label}
            </MenuItem>
          ))}
        </TextField>
      )}
    />
  );
}

interface AppCurrencyFieldProps extends BaseProps {
  helperText?: string;
}

export function AppCurrencyField({ name, label, helperText }: AppCurrencyFieldProps) {
  const { control } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...field}
          type="number"
          inputProps={{ step: '0.01', min: 0 }}
          fullWidth
          size="small"
          label={label}
          value={field.value ?? ''}
          error={!!fieldState.error}
          helperText={fieldState.error?.message ?? helperText}
        />
      )}
    />
  );
}

interface AppDateFieldProps extends BaseProps {
  helperText?: string;
}

export function AppDateField({ name, label, helperText }: AppDateFieldProps) {
  const { control } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...field}
          type="date"
          fullWidth
          size="small"
          InputLabelProps={{ shrink: true }}
          label={label}
          value={field.value ?? ''}
          error={!!fieldState.error}
          helperText={fieldState.error?.message ?? helperText}
        />
      )}
    />
  );
}

interface AppSwitchFieldProps extends BaseProps {
  helperText?: string;
}

export function AppSwitchField({ name, label, helperText }: AppSwitchFieldProps) {
  const { control } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <FormControlLabel
          control={<Switch checked={!!field.value} onChange={(_, v) => field.onChange(v)} />}
          label={
            <span>
              {label}
              {helperText && <span style={{ color: 'rgba(0,0,0,0.5)', fontSize: 12, marginLeft: 8 }}>{helperText}</span>}
            </span>
          }
        />
      )}
    />
  );
}
