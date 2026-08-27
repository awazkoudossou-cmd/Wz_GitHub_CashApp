import type { LoginRequest, LoginResponse } from '@/types';
import { apiClient } from './client';

export const authApi = {
  login: (payload: LoginRequest) =>
    apiClient.post<LoginResponse>('/api/auth/login', payload).then((r) => r.data),
  me: () => apiClient.get<LoginResponse>('/api/auth/me').then((r) => r.data)
};
