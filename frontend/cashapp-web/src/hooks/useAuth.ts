import { useMutation, useQuery } from '@tanstack/react-query';
import { authApi } from '@/api/authApi';
import { useAuthStore } from '@/app/store/authStore';
import type { LoginRequest } from '@/types';

export function useLogin() {
  const setSession = useAuthStore((s) => s.setSession);
  return useMutation({
    mutationFn: (payload: LoginRequest) => authApi.login(payload),
    onSuccess: (data) => setSession(data)
  });
}

export function useBootstrapMe(enabled: boolean) {
  const refresh = useAuthStore((s) => s.refresh);
  return useQuery({
    queryKey: ['auth', 'me'],
    queryFn: async () => {
      const me = await authApi.me();
      refresh(me);
      return me;
    },
    enabled,
    staleTime: 60_000
  });
}

export function useLogout() {
  const logout = useAuthStore((s) => s.logout);
  return () => logout();
}
