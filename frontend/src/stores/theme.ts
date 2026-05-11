import { defineStore } from 'pinia';

const LS_THEME = 'wms.theme';

export const useThemeStore = defineStore('theme', {
  state: () => ({
    dark: localStorage.getItem(LS_THEME) === 'dark',
  }),
  actions: {
    toggle() {
      this.dark = !this.dark;
      localStorage.setItem(LS_THEME, this.dark ? 'dark' : 'light');
      this.apply();
    },
    apply() {
      const root = document.documentElement;
      if (this.dark) root.classList.add('dark');
      else root.classList.remove('dark');
    },
  },
});
