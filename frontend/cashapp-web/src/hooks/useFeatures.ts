import { useAuthStore } from '@/app/store/authStore';

export function useFeatures() {
  return useAuthStore((s) => s.features);
}

export function useIsFeatureEnabled(featureCode: string): boolean {
  return useAuthStore(
    (s) => s.features.find((f) => f.featureCode === featureCode)?.isEnabled ?? false
  );
}
