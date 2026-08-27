import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export type ThemeMode = 'light' | 'dark';

interface ThemeState {
  mode: ThemeMode;
  sidebarCollapsed: boolean;
  collapsedSections: string[];   // ids des groupes pliés dans la sidebar
  toggle: () => void;
  setMode: (mode: ThemeMode) => void;
  toggleSidebar: () => void;
  setSidebarCollapsed: (collapsed: boolean) => void;
  toggleSection: (id: string) => void;
  setSectionCollapsed: (id: string, collapsed: boolean) => void;
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set) => ({
      mode: 'light',
      sidebarCollapsed: false,
      collapsedSections: [],
      toggle: () => set((s) => ({ mode: s.mode === 'light' ? 'dark' : 'light' })),
      setMode: (mode) => set({ mode }),
      toggleSidebar: () => set((s) => ({ sidebarCollapsed: !s.sidebarCollapsed })),
      setSidebarCollapsed: (sidebarCollapsed) => set({ sidebarCollapsed }),
      toggleSection: (id) =>
        set((s) => ({
          collapsedSections: s.collapsedSections.includes(id)
            ? s.collapsedSections.filter((x) => x !== id)
            : [...s.collapsedSections, id]
        })),
      setSectionCollapsed: (id, collapsed) =>
        set((s) => {
          const isIn = s.collapsedSections.includes(id);
          if (collapsed && !isIn) return { collapsedSections: [...s.collapsedSections, id] };
          if (!collapsed && isIn) return { collapsedSections: s.collapsedSections.filter((x) => x !== id) };
          return s;
        })
    }),
    { name: 'cashapp.theme' }
  )
);
