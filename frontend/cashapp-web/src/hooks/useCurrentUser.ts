import { useAuthStore } from '@/app/store/authStore';

export function useCurrentUser() {
  return useAuthStore((s) => s.user);
}

export function useIsInRole(roleCode: string): boolean {
  return useAuthStore((s) => s.user?.roleCode === roleCode);
}

export function useIsInAnyRole(roleCodes: string[]): boolean {
  return useAuthStore((s) => (s.user ? roleCodes.includes(s.user.roleCode) : false));
}
