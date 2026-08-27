import type { ReactNode } from 'react';
import { useIsFeatureEnabled } from '@/hooks/useFeatures';

interface Props {
  feature: string;
  fallback?: ReactNode;
  children: ReactNode;
}

export function FeatureGuard({ feature, fallback = null, children }: Props) {
  const enabled = useIsFeatureEnabled(feature);
  return <>{enabled ? children : fallback}</>;
}
