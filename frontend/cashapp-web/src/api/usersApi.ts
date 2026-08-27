import type { CreateUserPayload, UpdateUserPayload, UserDetail, UserListItem } from '@/types';
import { apiClient } from './client';

export const usersApi = {
  list: () => apiClient.get<UserListItem[]>('/api/users').then((r) => r.data),
  get: (id: number) => apiClient.get<UserDetail>(`/api/users/${id}`).then((r) => r.data),
  create: (p: CreateUserPayload) =>
    apiClient.post<UserDetail>('/api/users', p).then((r) => r.data),
  update: (id: number, p: UpdateUserPayload) =>
    apiClient.put<UserDetail>(`/api/users/${id}`, p).then((r) => r.data),
  updateStatus: (id: number, isActive: boolean) =>
    apiClient.patch(`/api/users/${id}/status`, { isActive }).then((r) => r.data),
  resetPassword: (id: number, newPassword: string) =>
    apiClient.post(`/api/users/${id}/reset-password`, { newPassword }).then((r) => r.data)
};
