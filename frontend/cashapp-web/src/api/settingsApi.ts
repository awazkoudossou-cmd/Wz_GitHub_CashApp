import type { AppMode, CompanyInfo, FeatureSetting, GeneralSettings, UpdateFeatureSettings } from '@/types';
import { apiClient } from './client';

export const settingsApi = {
  getGeneral: () => apiClient.get<GeneralSettings>('/api/settings/general').then((r) => r.data),
  updateGeneral: (p: GeneralSettings) =>
    apiClient.put<GeneralSettings>('/api/settings/general', p).then((r) => r.data),
  getAppMode: () => apiClient.get<{ mode: AppMode }>('/api/settings/app-mode').then((r) => r.data),
  updateAppMode: (mode: AppMode) =>
    apiClient.put<{ mode: AppMode }>('/api/settings/app-mode', { mode }).then((r) => r.data),
  getFeatures: () => apiClient.get<FeatureSetting[]>('/api/settings/features').then((r) => r.data),
  updateFeatures: (p: UpdateFeatureSettings) =>
    apiClient.put<FeatureSetting[]>('/api/settings/features', p).then((r) => r.data),
  getCompany: () => apiClient.get<CompanyInfo>('/api/settings/company').then((r) => r.data),
  updateCompany: (p: CompanyInfo) =>
    apiClient.put<CompanyInfo>('/api/settings/company', p).then((r) => r.data),
  resetDatabase: (confirmationCode: string) =>
    apiClient.post('/api/settings/reset-database', { confirmationCode }).then((r) => r.data)
};
