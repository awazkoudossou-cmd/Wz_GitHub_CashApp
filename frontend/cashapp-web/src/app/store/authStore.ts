import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type {
  AppMode,
  CurrentCashRegister,
  CurrentUser,
  FeatureDto,
  LoginResponse
} from '@/types';

interface AuthState {
  token: string | null;
  user: CurrentUser | null;
  features: FeatureDto[];
  appMode: AppMode | null;
  cashRegisters: CurrentCashRegister[];
  isAuthenticated: boolean;
  selectedCashRegisterId: number | null;

  setSession: (data: LoginResponse) => void;
  refresh: (data: LoginResponse) => void;
  setSelectedCashRegister: (id: number | null) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      features: [],
      appMode: null,
      cashRegisters: [],
      isAuthenticated: false,
      selectedCashRegisterId: null,

      setSession: (data) =>
        set({
          token: data.token,
          user: data.user,
          features: data.features,
          appMode: data.appMode,
          cashRegisters: data.cashRegisters,
          isAuthenticated: true,
          selectedCashRegisterId: data.cashRegisters[0]?.id ?? null
        }),
      refresh: (data) =>
        set((s) => ({
          user: data.user,
          features: data.features,
          appMode: data.appMode,
          cashRegisters: data.cashRegisters,
          isAuthenticated: true,
          selectedCashRegisterId:
            s.selectedCashRegisterId && data.cashRegisters.some((c) => c.id === s.selectedCashRegisterId)
              ? s.selectedCashRegisterId
              : data.cashRegisters[0]?.id ?? null
        })),
      setSelectedCashRegister: (id) => set({ selectedCashRegisterId: id }),
      logout: () =>
        set({
          token: null,
          user: null,
          features: [],
          appMode: null,
          cashRegisters: [],
          isAuthenticated: false,
          selectedCashRegisterId: null
        })
    }),
    {
      name: 'cashapp.auth',
      partialize: (s) => ({
        token: s.token,
        user: s.user,
        features: s.features,
        appMode: s.appMode,
        cashRegisters: s.cashRegisters,
        isAuthenticated: s.isAuthenticated,
        selectedCashRegisterId: s.selectedCashRegisterId
      })
    }
  )
);

export function isFeatureEnabled(featureCode: string): boolean {
  return useAuthStore.getState().features.find((f) => f.featureCode === featureCode)?.isEnabled ?? false;
}
