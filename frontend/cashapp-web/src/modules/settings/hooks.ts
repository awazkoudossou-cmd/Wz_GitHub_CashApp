import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { settingsApi } from '@/api/settingsApi';
import { authApi } from '@/api/authApi';
import { useAuthStore } from '@/app/store/authStore';
import type { AppMode, CompanyInfo, GeneralSettings, UpdateFeatureSettings } from '@/types';

export function useGeneralSettings() {
  return useQuery({ queryKey: ['settings', 'general'], queryFn: settingsApi.getGeneral });
}

export function useUpdateGeneralSettings() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: GeneralSettings) => settingsApi.updateGeneral(p),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['settings', 'general'] })
  });
}

export function useAppMode() {
  return useQuery({ queryKey: ['settings', 'app-mode'], queryFn: settingsApi.getAppMode });
}

export function useUpdateAppMode() {
  const qc = useQueryClient();
  const refresh = useAuthStore((s) => s.refresh);
  return useMutation({
    mutationFn: (mode: AppMode) => settingsApi.updateAppMode(mode),
    onSuccess: async () => {
      qc.invalidateQueries({ queryKey: ['settings', 'app-mode'] });
      qc.invalidateQueries({ queryKey: ['settings', 'features'] });
      // Re-fetch immédiat de /api/auth/me pour synchroniser features dans le store auth
      // (la sidebar lit depuis useAuthStore, pas TanStack Query).
      try {
        const me = await authApi.me();
        refresh(me);
      } catch { /* silencieux */ }
    }
  });
}

export function useFeatureSettings() {
  return useQuery({ queryKey: ['settings', 'features'], queryFn: settingsApi.getFeatures });
}

export function useUpdateFeatureSettings() {
  const qc = useQueryClient();
  const refresh = useAuthStore((s) => s.refresh);
  return useMutation({
    mutationFn: (p: UpdateFeatureSettings) => settingsApi.updateFeatures(p),
    onSuccess: async () => {
      qc.invalidateQueries({ queryKey: ['settings', 'features'] });
      // Resync store auth → la sidebar reflète immédiatement les changements
      try {
        const me = await authApi.me();
        refresh(me);
      } catch { /* silencieux */ }
    }
  });
}

export function useCompanyInfo() {
  return useQuery({ queryKey: ['settings', 'company'], queryFn: settingsApi.getCompany });
}

export function useUpdateCompanyInfo() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CompanyInfo) => settingsApi.updateCompany(p),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['settings', 'company'] })
  });
}
