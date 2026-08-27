import { Navigate, Outlet, useLocation } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useAuthStore } from '@/app/store/authStore';
import { useBootstrapMe } from '@/hooks/useAuth';
import { useIsInAnyRole } from '@/hooks/useCurrentUser';
import { useIsFeatureEnabled } from '@/hooks/useFeatures';
import { LoadingScreen } from '@/components/common/LoadingScreen';

interface Props {
  feature?: string;
  roles?: string[];
  children?: ReactNode;
}

export function ProtectedRoute({ feature, roles, children }: Props) {
  const location = useLocation();
  const token = useAuthStore((s) => s.token);
  const user = useAuthStore((s) => s.user);
  const me = useBootstrapMe(!!token);
  const featureOk = useIsFeatureEnabled(feature ?? '__none__') || !feature;
  const roleOk = useIsInAnyRole(roles ?? []) || !roles?.length;

  if (!token) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (me.isLoading && !user) {
    return <LoadingScreen fullScreen message="Chargement de la session…" />;
  }

  if (feature && !featureOk) return <Navigate to="/403" replace />;
  if (roles?.length && !roleOk) return <Navigate to="/403" replace />;

  return <>{children ?? <Outlet />}</>;
}
