import axios, { AxiosError, type AxiosInstance } from 'axios';
import { useAuthStore } from '@/app/store/authStore';

const baseURL = import.meta.env.VITE_API_BASE_URL || '/';

export const apiClient: AxiosInstance = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' }
});

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<{ message?: string; errors?: string[] }>) => {
    if (error.response?.status === 401) {
      const { logout } = useAuthStore.getState();
      logout();
      if (typeof window !== 'undefined' && !window.location.pathname.startsWith('/login')) {
        window.location.assign('/login');
      }
    }
    return Promise.reject(error);
  }
);

export function extractErrorMessage(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as { message?: string; errors?: string[] } | undefined;
    if (data?.message) {
      return data.errors?.length ? `${data.message} — ${data.errors.join(' ; ')}` : data.message;
    }
    return err.message;
  }
  return (err as Error)?.message ?? 'Erreur inconnue';
}
