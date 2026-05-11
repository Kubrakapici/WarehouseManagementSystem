<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router';
import Button from 'primevue/button';
import Avatar from 'primevue/avatar';
import { useAuthStore } from '@/stores/auth';
import { useThemeStore } from '@/stores/theme';

const auth = useAuthStore();
const theme = useThemeStore();
const route = useRoute();
const router = useRouter();
const logoutLabel = '\u00c7\u0131k\u0131\u015f Yap';
const themeTooltip = 'Tema de\u011fi\u015ftir';

const nav = computed(() => {
  const items = [
    { to: { name: 'home' }, label: 'Ana Sayfa', icon: 'pi pi-home', roles: ['Admin', 'Manager', 'WarehouseStaff', 'Operations', 'Viewer'] },
    { to: '/products', label: '\u00dcr\u00fcn Y\u00f6netimi', icon: 'pi pi-box', roles: ['Admin', 'Manager', 'WarehouseStaff', 'Operations', 'Viewer'] },
    { to: '/stock', label: 'Stok Hareketleri', icon: 'pi pi-sort-alt', roles: ['Admin', 'Manager', 'WarehouseStaff', 'Operations', 'Viewer'] },
    { to: '/warehouses', label: 'Depolar', icon: 'pi pi-building', roles: ['Admin', 'Manager', 'WarehouseStaff', 'Operations', 'Viewer'] },
    { to: '/purchase-requisitions', label: 'Sat\u0131n Alma', icon: 'pi pi-file-edit', roles: ['Admin', 'Manager', 'Operations', 'Viewer'] },
    { to: '/customers', label: 'M\u00fc\u015fteriler', icon: 'pi pi-id-card', roles: ['Admin', 'Manager', 'Operations', 'Viewer'] },
    { to: '/orders', label: 'Sipari\u015fler', icon: 'pi pi-shopping-cart', roles: ['Admin', 'Manager', 'WarehouseStaff', 'Operations', 'Viewer'] },
    { to: '/users', label: 'Kullan\u0131c\u0131lar', icon: 'pi pi-users', roles: ['Admin'] },
  ];

  return items.filter((i) => auth.hasRole(i.roles));
});

const initials = computed(() => {
  const name = auth.fullName?.trim();
  if (!name) return 'WM';
  return name
    .split(' ')
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('');
});

const currentPage = computed(() => {
  const path = route.path;
  if (path === '/' || path === '/dashboard') return 'Ana Sayfa';
  const hit = nav.value.find((x) => typeof x.to === 'string' && (path === x.to || path.startsWith(`${x.to}/`)));
  return hit?.label ?? 'Panel';
});

async function logout() {
  await auth.logout();
  await router.push('/login');
}
</script>

<template>
  <div class="app-shell flex h-full">
    <aside class="relative z-10 hidden w-72 shrink-0 border-r border-white/60 bg-white/70 px-4 py-5 backdrop-blur dark:border-slate-800 dark:bg-slate-900/70 lg:block">
      <div class="mb-8 flex items-center gap-3 px-2">
        <div class="grid h-11 w-11 place-items-center rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-sm font-bold text-white shadow">WMS</div>
        <div>
          <div class="text-sm font-semibold text-slate-900 dark:text-slate-100">Warehouse Management</div>
          <div class="text-xs text-slate-500 dark:text-slate-400">Enterprise Control Panel</div>
        </div>
      </div>

      <nav class="space-y-1.5">
        <RouterLink
          v-for="item in nav"
          :key="item.label"
          :to="item.to"
          class="group flex items-center justify-between rounded-xl px-3 py-2.5 text-sm font-medium text-slate-600 transition hover:bg-white hover:text-brand-700 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-brand-300"
          active-class="bg-brand-50 text-brand-800 shadow-sm dark:bg-slate-800 dark:text-brand-300"
        >
          <span class="flex items-center gap-3">
            <i :class="item.icon" class="text-base" />
            <span>{{ item.label }}</span>
          </span>
          <i class="pi pi-angle-right text-xs opacity-0 transition group-hover:opacity-100" />
        </RouterLink>
      </nav>

      <div class="absolute inset-x-4 bottom-4 rounded-2xl border border-slate-200/80 bg-white/70 p-3 dark:border-slate-700 dark:bg-slate-800/80">
        <div class="mb-2 text-xs font-medium text-slate-500 dark:text-slate-400">Aktif rol</div>
        <div class="glass-pill inline-flex">{{ auth.role || 'Tan\u0131ms\u0131z' }}</div>
      </div>
    </aside>

    <div class="relative z-10 flex min-w-0 flex-1 flex-col">
      <header class="sticky top-0 z-20 border-b border-white/70 bg-white/70 px-4 py-3 backdrop-blur dark:border-slate-800 dark:bg-slate-900/70 md:px-6">
        <div class="flex items-center justify-between gap-4">
          <div>
            <div class="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">Kurumsal Depo Otomasyonu</div>
            <div class="text-lg font-semibold text-slate-900 dark:text-slate-100">{{ currentPage }}</div>
          </div>

          <div class="flex items-center gap-2">
            <Button :icon="theme.dark ? 'pi pi-sun' : 'pi pi-moon'" text rounded @click="theme.toggle" v-tooltip.bottom="themeTooltip" />
            <div class="hidden items-center gap-2 rounded-xl border border-slate-200/80 bg-white/80 px-2 py-1 dark:border-slate-700 dark:bg-slate-800/80 sm:flex">
              <Avatar :label="initials" shape="circle" class="bg-brand-600 text-white" />
              <div class="pr-2 leading-tight">
                <div class="text-xs font-semibold text-slate-800 dark:text-slate-100">{{ auth.fullName || 'Kullan\u0131c\u0131' }}</div>
                <div class="text-[11px] text-slate-500 dark:text-slate-400">{{ auth.email }}</div>
              </div>
            </div>
            <Button :label="logoutLabel" icon="pi pi-sign-out" severity="danger" outlined @click="logout" />
          </div>
        </div>
      </header>

      <main class="flex-1 overflow-auto p-4 md:p-6">
        <RouterView />
      </main>
    </div>
  </div>
</template>
