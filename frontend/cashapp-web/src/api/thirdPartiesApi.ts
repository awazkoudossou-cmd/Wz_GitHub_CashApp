import type { ThirdParty } from '@/types';
import { apiClient } from './client';

export const thirdPartiesApi = {
  list: (search?: string) =>
    apiClient.get<ThirdParty[]>('/api/third-parties', { params: { search } }).then((r) => r.data)
};
