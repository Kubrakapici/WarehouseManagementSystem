import axios from 'axios';
import { useAuthStore } from '@/stores/auth';

const baseURL = import.meta.env.VITE_API_URL?.trim() || '';

export const api = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const auth = useAuthStore();
  if (auth.accessToken) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${auth.accessToken}`;
  }
  return config;
});

api.interceptors.response.use(
  (r) => r,
  async (error) => {
    const original = error.config;
    if (error.response?.status === 401 && !original._retry && useAuthStore().refreshToken) {
      original._retry = true;
      try {
        await useAuthStore().refresh();
        original.headers.Authorization = `Bearer ${useAuthStore().accessToken}`;
        return api(original);
      } catch {
        useAuthStore().clear();
      }
    }
    return Promise.reject(error);
  },
);

export type ApiResponse<T> = {
  success: boolean;
  message?: string | null;
  data?: T;
  errors?: string[] | null;
};
