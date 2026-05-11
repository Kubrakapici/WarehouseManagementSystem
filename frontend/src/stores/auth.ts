import { defineStore } from 'pinia';
import { api, type ApiResponse } from '@/services/api';

type LoginResponse = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiry: string;
  userId: string;
  email: string;
  fullName: string;
  role: string;
};

const LS_ACCESS = 'wms.accessToken';
const LS_REFRESH = 'wms.refreshToken';
const LS_ROLE = 'wms.role';
const LS_EMAIL = 'wms.email';
const LS_NAME = 'wms.fullName';

export const useAuthStore = defineStore('auth', {
  state: () => ({
    accessToken: localStorage.getItem(LS_ACCESS) ?? '',
    refreshToken: localStorage.getItem(LS_REFRESH) ?? '',
    role: localStorage.getItem(LS_ROLE) ?? '',
    email: localStorage.getItem(LS_EMAIL) ?? '',
    fullName: localStorage.getItem(LS_NAME) ?? '',
  }),
  getters: {
    isAuthenticated: (s) => !!s.accessToken,
    hasRole: (s) => {
      return (roles: string[]) => roles.map((r) => r.toLowerCase()).includes(s.role.toLowerCase());
    },
  },
  actions: {
    persist(payload: LoginResponse) {
      this.accessToken = payload.accessToken;
      this.refreshToken = payload.refreshToken;
      this.role = payload.role;
      this.email = payload.email;
      this.fullName = payload.fullName;
      localStorage.setItem(LS_ACCESS, payload.accessToken);
      localStorage.setItem(LS_REFRESH, payload.refreshToken);
      localStorage.setItem(LS_ROLE, payload.role);
      localStorage.setItem(LS_EMAIL, payload.email);
      localStorage.setItem(LS_NAME, payload.fullName);
    },
    clear() {
      this.accessToken = '';
      this.refreshToken = '';
      this.role = '';
      this.email = '';
      this.fullName = '';
      localStorage.removeItem(LS_ACCESS);
      localStorage.removeItem(LS_REFRESH);
      localStorage.removeItem(LS_ROLE);
      localStorage.removeItem(LS_EMAIL);
      localStorage.removeItem(LS_NAME);
    },
    async login(email: string, password: string) {
      const { data } = await api.post<ApiResponse<LoginResponse>>('/api/auth/login', { email, password });
      if (!data.success || !data.data) throw new Error(data.message || 'Giri\u015f ba\u015far\u0131s\u0131z');
      this.persist(data.data);
    },
    async refresh() {
      const { data } = await api.post<ApiResponse<LoginResponse>>('/api/auth/refresh', {
        refreshToken: this.refreshToken,
      });
      if (!data.success || !data.data) throw new Error('refresh failed');
      this.persist(data.data);
    },
    async logout() {
      try {
        await api.post('/api/auth/logout');
      } catch {
        /* ignore */
      }
      this.clear();
    },
  },
});
