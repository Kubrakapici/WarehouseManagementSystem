<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { RouterLink } from 'vue-router';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Tag from 'primevue/tag';
import Skeleton from 'primevue/skeleton';
import { useToast } from 'primevue/usetoast';
import { api, type ApiResponse } from '@/services/api';
import { createStockHubConnection } from '@/services/realtime';
import { useAuthStore } from '@/stores/auth';

type Summary = {
  totalProducts: number;
  totalWarehouses: number;
  todayStockEntries: number;
  todayStockExits: number;
  criticalStockProducts: Array<{ id: string; name: string; sku: string; totalQuantity: number; minimumStockLevel: number }>;
  recentMovements: Array<{ id: string; movementType: string; quantity: number; productName: string; createdDate: string }>;
  onlineUsers?: number;
  warehouseOccupancy?: Array<{ warehouseId: string; warehouseName: string; totalLocations: number; usedLocations: number; occupancyRatio: number }>;
  topMovedProducts?: Array<{ productId: string; name: string; sku: string; movementUnits: number }>;
  lastSevenDaysMovement?: Array<{ label: string; entries: number; exits: number }>;
};

const auth = useAuthStore();
const toast = useToast();
const loading = ref(false);
const summary = ref<Summary | null>(null);
const pendingPurchaseCount = ref(0);
const pendingOrdersCount = ref(0);
const suggestionCount = ref(0);

const canProcurement = computed(() => auth.hasRole(['Admin', 'Manager', 'Operations', 'Viewer']));
const canAdminUsers = computed(() => auth.hasRole(['Admin']));
const isViewerMode = computed(() => auth.hasRole(['Viewer']));
const ordersStatCardClass = computed(() =>
  canProcurement.value ? 'surface-card p-4 sm:col-span-2 xl:col-span-2' : 'surface-card p-4 xl:col-span-4',
);

let hub: ReturnType<typeof createStockHubConnection> | null = null;

type PagedLite = { totalCount: number };

async function loadQuickStats() {
  if (canProcurement.value) {
    try {
      const { data } = await api.get<ApiResponse<PagedLite>>('/api/purchase-requisitions', {
        params: { pageNumber: 1, pageSize: 1, status: 2 },
      });
      pendingPurchaseCount.value = data.success && data.data ? data.data.totalCount : 0;
    } catch {
      pendingPurchaseCount.value = 0;
    }
    try {
      const { data } = await api.get<ApiResponse<unknown[]>>('/api/purchase-requisitions/suggestions');
      suggestionCount.value = data.success && Array.isArray(data.data) ? data.data.length : 0;
    } catch {
      suggestionCount.value = 0;
    }
  } else {
    pendingPurchaseCount.value = 0;
    suggestionCount.value = 0;
  }

  try {
    const { data } = await api.get<ApiResponse<PagedLite>>('/api/orders', {
      params: { pageNumber: 1, pageSize: 1, status: 2 },
    });
    pendingOrdersCount.value = data.success && data.data ? data.data.totalCount : 0;
  } catch {
    pendingOrdersCount.value = 0;
  }
}

async function refreshSummary() {
  const refreshed = await api.get<ApiResponse<Summary>>('/api/dashboard/summary');
  if (refreshed.data.success) summary.value = refreshed.data.data ?? null;
}

const metricCards = computed(() => {
  const data = summary.value;
  return [
    { label: 'Toplam \u00dcr\u00fcn', value: data?.totalProducts ?? 0, icon: 'pi pi-box', color: 'text-indigo-600' },
    { label: 'Toplam Depo', value: data?.totalWarehouses ?? 0, icon: 'pi pi-building', color: 'text-cyan-600' },
    { label: 'G\u00fcnl\u00fck Giri\u015f', value: data?.todayStockEntries ?? 0, icon: 'pi pi-arrow-down-left', color: 'text-emerald-600' },
    { label: 'G\u00fcnl\u00fck \u00c7\u0131k\u0131\u015f', value: data?.todayStockExits ?? 0, icon: 'pi pi-arrow-up-right', color: 'text-rose-600' },
    { label: '\u00c7evrimi\u00e7i Kullan\u0131c\u0131', value: data?.onlineUsers ?? 0, icon: 'pi pi-users', color: 'text-violet-600' },
  ];
});

const warehouseOccupancy = computed(() => summary.value?.warehouseOccupancy ?? []);
const lastSevenDays = computed(() => summary.value?.lastSevenDaysMovement ?? []);
const topMovedProducts = computed(() => summary.value?.topMovedProducts ?? []);

const maxDayUnits = computed(() => {
  const rows = lastSevenDays.value;
  if (!rows.length) return 1;
  return Math.max(1, ...rows.map((r) => Math.max(r.entries, r.exits)));
});

function pct(value: number) {
  return `${Math.round(Math.min(1, Math.max(0, value)) * 100)}%`;
}

function movementSeverity(type: string) {
  switch (type.toLowerCase()) {
    case 'entry': return 'success';
    case 'exit': return 'danger';
    case 'transfer': return 'info';
    default: return 'warning';
  }
}

function formatDate(value: string) {
  return new Date(value).toLocaleString('tr-TR', {
    dateStyle: 'short',
    timeStyle: 'short',
  });
}

onMounted(async () => {
  loading.value = true;
  try {
    const { data } = await api.get<ApiResponse<Summary>>('/api/dashboard/summary');
    if (!data.success) throw new Error(data.message || '\u00d6zet al\u0131namad\u0131');
    summary.value = data.data ?? null;
    await loadQuickStats();

    hub = createStockHubConnection();
    const onLive = async (toastOpts: { severity: 'success' | 'info' | 'warn' | 'error'; summary: string; detail: string; life: number } | null) => {
      if (toastOpts) toast.add(toastOpts);
      await refreshSummary();
      await loadQuickStats();
    };

    hub.on('StockUpdated', () =>
      onLive({ severity: 'success', summary: 'Canl\u0131 G\u00fcncelleme', detail: 'Dashboard verileri yenilendi.', life: 2000 }),
    );
    hub.on('DashboardChanged', () =>
      onLive({ severity: 'info', summary: 'Dashboard', detail: 'Yeni operasyon verisi.', life: 1800 }),
    );
    hub.on('OrderStatusChanged', () =>
      onLive({ severity: 'info', summary: 'Sipari\u015f', detail: 'Sipari\u015f durumu g\u00fcncellendi.', life: 1800 }),
    );

    await hub.start();
    await hub.invoke('SubscribeDashboard');
    if (auth.hasRole(['Admin', 'Manager', 'WarehouseStaff', 'Operations'])) {
      await hub.invoke('SubscribeOrders');
    }
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : 'Dashboard y\u00fcklenemedi';
    toast.add({ severity: 'error', summary: 'Hata', detail: msg, life: 4000 });
  } finally {
    loading.value = false;
  }
});

onUnmounted(async () => {
  try {
    if (hub?.state === 'Connected') {
      await hub.invoke('UnsubscribeDashboard');
      if (auth.hasRole(['Admin', 'Manager', 'WarehouseStaff', 'Operations'])) {
        await hub.invoke('UnsubscribeOrders');
      }
    }
  } catch {
    /* ignore */
  }
  try {
    await hub?.stop();
  } catch {
    /* ignore */
  }
  hub = null;
});
</script>

<template>
  <div class="space-y-5">
    <div class="surface-card p-5 md:p-6">
      <h1 class="section-title">Operasyon &Ouml;zeti</h1>
      <p class="section-subtitle">Canl&#305; stok sinyalleri ile merkezi depo performans g&ouml;r&uuml;n&uuml;m&uuml;</p>
      <div v-if="isViewerMode" class="mt-4 flex items-start gap-3 rounded-xl border border-amber-200/70 bg-amber-50/70 p-3 text-amber-900 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-200">
        <i class="pi pi-eye mt-0.5 text-base" />
        <div class="text-sm">
          <div class="font-semibold">&#304;zleme modu aktif</div>
          <div class="text-xs opacity-80">Bu hesap salt okunur eri&#351;ime sahiptir; veri ekleme, d&uuml;zenleme veya silme i&#351;lemleri yap&#305;lamaz.</div>
        </div>
      </div>
    </div>

    <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
      <div v-for="card in metricCards" :key="card.label" class="metric-card">
        <div class="flex items-center justify-between">
          <div class="metric-label">{{ card.label }}</div>
          <i :class="[card.icon, card.color]" class="text-lg" />
        </div>
        <Skeleton v-if="loading" width="6rem" height="2rem" class="mt-3" />
        <div v-else class="metric-value">{{ card.value }}</div>
      </div>
    </div>

    <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      <div v-if="canProcurement" class="surface-card p-4">
        <div class="text-xs font-medium uppercase tracking-wide text-slate-500">Sat&#305;n alma</div>
        <Skeleton v-if="loading" height="1.75rem" width="40%" class="mt-2" />
        <div v-else class="mt-1 text-2xl font-semibold text-slate-900 dark:text-slate-50">{{ pendingPurchaseCount }}</div>
        <div class="mt-1 text-xs text-slate-500">Onay bekleyen talep</div>
      </div>
      <div v-if="canProcurement" class="surface-card p-4">
        <div class="text-xs font-medium uppercase tracking-wide text-slate-500">Sat&#305;n alma &#246;nerileri</div>
        <Skeleton v-if="loading" height="1.75rem" width="40%" class="mt-2" />
        <div v-else class="mt-1 text-2xl font-semibold text-slate-900 dark:text-slate-50">{{ suggestionCount }}</div>
        <div class="mt-1 text-xs text-slate-500">&#214;neri sat&#305;r&#305; (kritik stok)</div>
      </div>
      <div :class="ordersStatCardClass">
        <div class="text-xs font-medium uppercase tracking-wide text-slate-500">Sipari&#351;ler</div>
        <Skeleton v-if="loading" height="1.75rem" width="40%" class="mt-2" />
        <div v-else class="mt-1 text-2xl font-semibold text-slate-900 dark:text-slate-50">{{ pendingOrdersCount }}</div>
        <div class="mt-1 text-xs text-slate-500">Bekleyen sipari&#351; (Pending)</div>
      </div>
    </div>

    <div class="surface-card p-5 md:p-6">
      <h2 class="text-base font-semibold text-slate-800 dark:text-slate-100">H&#305;zl&#305; eri&#351;im</h2>
      <p class="mt-1 text-sm text-slate-500 dark:text-slate-400">S&#305;k kullan&#305;lan mod&#252;llere tek t&#305;kla gidin.</p>
      <div class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
        <RouterLink
          to="/products"
          class="group flex flex-col rounded-xl border border-slate-200/80 bg-white/60 p-4 transition hover:border-brand-300 hover:bg-brand-50/80 dark:border-slate-700 dark:bg-slate-900/40 dark:hover:border-brand-500/50 dark:hover:bg-slate-800/80"
        >
          <i class="pi pi-box text-xl text-indigo-600 dark:text-indigo-400" />
          <span class="mt-2 text-sm font-semibold text-slate-800 dark:text-slate-100">&#220;r&#252;n y&#246;netimi</span>
          <span class="mt-0.5 text-xs text-slate-500">Katalog ve stok seviyeleri</span>
        </RouterLink>
        <RouterLink
          to="/stock"
          class="group flex flex-col rounded-xl border border-slate-200/80 bg-white/60 p-4 transition hover:border-brand-300 hover:bg-brand-50/80 dark:border-slate-700 dark:bg-slate-900/40 dark:hover:border-brand-500/50 dark:hover:bg-slate-800/80"
        >
          <i class="pi pi-sort-alt text-xl text-cyan-600 dark:text-cyan-400" />
          <span class="mt-2 text-sm font-semibold text-slate-800 dark:text-slate-100">Stok hareketleri</span>
          <span class="mt-0.5 text-xs text-slate-500">Giri&#351; / &#231;&#305;k&#305;&#351; kay&#305;tlar&#305;</span>
        </RouterLink>
        <RouterLink
          to="/warehouses"
          class="group flex flex-col rounded-xl border border-slate-200/80 bg-white/60 p-4 transition hover:border-brand-300 hover:bg-brand-50/80 dark:border-slate-700 dark:bg-slate-900/40 dark:hover:border-brand-500/50 dark:hover:bg-slate-800/80"
        >
          <i class="pi pi-building text-xl text-sky-600 dark:text-sky-400" />
          <span class="mt-2 text-sm font-semibold text-slate-800 dark:text-slate-100">Depolar</span>
          <span class="mt-0.5 text-xs text-slate-500">Ekle, d&uuml;zenle ve sil</span>
        </RouterLink>
        <RouterLink
          v-if="canProcurement"
          to="/purchase-requisitions"
          class="group flex flex-col rounded-xl border border-slate-200/80 bg-white/60 p-4 transition hover:border-brand-300 hover:bg-brand-50/80 dark:border-slate-700 dark:bg-slate-900/40 dark:hover:border-brand-500/50 dark:hover:bg-slate-800/80"
        >
          <i class="pi pi-file-edit text-xl text-amber-600 dark:text-amber-400" />
          <span class="mt-2 text-sm font-semibold text-slate-800 dark:text-slate-100">Sat&#305;n alma</span>
          <span class="mt-0.5 text-xs text-slate-500">Talepler ve onay ak&#305;&#351;&#305;</span>
        </RouterLink>
        <RouterLink
          v-if="canProcurement"
          to="/customers"
          class="group flex flex-col rounded-xl border border-slate-200/80 bg-white/60 p-4 transition hover:border-brand-300 hover:bg-brand-50/80 dark:border-slate-700 dark:bg-slate-900/40 dark:hover:border-brand-500/50 dark:hover:bg-slate-800/80"
        >
          <i class="pi pi-id-card text-xl text-rose-600 dark:text-rose-400" />
          <span class="mt-2 text-sm font-semibold text-slate-800 dark:text-slate-100">M&uuml;&#351;teriler</span>
          <span class="mt-0.5 text-xs text-slate-500">Kart, ileti&#351;im ve aktiflik</span>
        </RouterLink>
        <RouterLink
          to="/orders"
          class="group flex flex-col rounded-xl border border-slate-200/80 bg-white/60 p-4 transition hover:border-brand-300 hover:bg-brand-50/80 dark:border-slate-700 dark:bg-slate-900/40 dark:hover:border-brand-500/50 dark:hover:bg-slate-800/80"
        >
          <i class="pi pi-shopping-cart text-xl text-emerald-600 dark:text-emerald-400" />
          <span class="mt-2 text-sm font-semibold text-slate-800 dark:text-slate-100">Sipari&#351;ler</span>
          <span class="mt-0.5 text-xs text-slate-500">Durum ve sevkiyat</span>
        </RouterLink>
        <RouterLink
          v-if="canAdminUsers"
          to="/users"
          class="group flex flex-col rounded-xl border border-slate-200/80 bg-white/60 p-4 transition hover:border-brand-300 hover:bg-brand-50/80 dark:border-slate-700 dark:bg-slate-900/40 dark:hover:border-brand-500/50 dark:hover:bg-slate-800/80"
        >
          <i class="pi pi-users text-xl text-violet-600 dark:text-violet-400" />
          <span class="mt-2 text-sm font-semibold text-slate-800 dark:text-slate-100">Kullan&#305;c&#305;lar</span>
          <span class="mt-0.5 text-xs text-slate-500">Rol ve eri&#351;im</span>
        </RouterLink>
      </div>
    </div>

    <div class="grid gap-4 lg:grid-cols-2">
      <div class="surface-card overflow-hidden">
        <div class="border-b border-slate-200/70 px-4 py-3 text-sm font-semibold text-slate-700 dark:border-slate-800 dark:text-slate-200">
          Depo Doluluk Oran&#305; (lokasyon bazl&#305;)
        </div>
        <div class="space-y-4 p-4">
          <template v-if="loading">
            <Skeleton height="3rem" class="w-full" />
            <Skeleton height="3rem" class="w-full" />
          </template>
          <template v-else-if="!warehouseOccupancy.length">
            <p class="py-6 text-center text-sm text-slate-500">Depo verisi bulunamad&#305;.</p>
          </template>
          <div v-for="w in warehouseOccupancy" :key="w.warehouseId" class="space-y-2">
            <div class="flex items-center justify-between text-sm">
              <span class="font-medium text-slate-800 dark:text-slate-100">{{ w.warehouseName }}</span>
              <span class="text-slate-500">{{ w.usedLocations }} / {{ w.totalLocations }} lokasyon</span>
            </div>
            <div class="h-2.5 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-700">
              <div
                class="h-full rounded-full bg-gradient-to-r from-cyan-500 to-indigo-500 transition-all"
                :style="{ width: pct(w.occupancyRatio) }"
              />
            </div>
            <div class="text-right text-xs text-slate-500">{{ pct(w.occupancyRatio) }} kullan&#305;mda</div>
          </div>
        </div>
      </div>

      <div class="surface-card overflow-hidden">
        <div class="border-b border-slate-200/70 px-4 py-3 text-sm font-semibold text-slate-700 dark:border-slate-800 dark:text-slate-200">
          Son 7 G&#252;n Stok Hareketi
        </div>
        <div class="space-y-3 p-4">
          <template v-if="loading">
            <Skeleton v-for="n in 5" :key="n" height="2.5rem" class="w-full" />
          </template>
          <template v-else-if="!lastSevenDays.length">
            <p class="py-6 text-center text-sm text-slate-500">Grafik verisi yok.</p>
          </template>
          <div v-for="day in lastSevenDays" :key="day.label" class="space-y-1">
            <div class="flex justify-between text-xs text-slate-500">
              <span>{{ day.label }}</span>
              <span>Giri&#351; {{ day.entries }} / &#199;&#305;k&#305;&#351; {{ day.exits }}</span>
            </div>
            <div class="flex gap-1">
              <div
                class="h-8 flex-1 rounded-md bg-emerald-500/90 transition-all"
                :style="{ flexGrow: Math.max(0.05, day.entries / maxDayUnits) }"
                title="Giri&#351;"
              />
              <div
                class="h-8 flex-1 rounded-md bg-rose-500/90 transition-all"
                :style="{ flexGrow: Math.max(0.05, day.exits / maxDayUnits) }"
                title="&#199;&#305;k&#305;&#351;"
              />
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="table-shell">
      <div class="border-b border-slate-200/70 px-4 py-3 text-sm font-semibold text-slate-700 dark:border-slate-800 dark:text-slate-200">
        En &#199;ok Hareket G&#246;ren &#220;r&#252;nler (son 30 g&#252;n)
      </div>
      <DataTable :value="topMovedProducts" :loading="loading" responsive-layout="scroll" class="p-2" stripedRows>
        <template #empty>
          <div class="py-8 text-center text-sm text-slate-500">Veri bulunamad&#305;.</div>
        </template>
        <Column field="sku" header="SKU" />
        <Column field="name" header="&#220;r&#252;n" />
        <Column field="movementUnits" header="Hareket (adet)" />
      </DataTable>
    </div>

    <div class="grid gap-4 xl:grid-cols-5">
      <div class="table-shell xl:col-span-2">
        <div class="border-b border-slate-200/70 px-4 py-3 text-sm font-semibold text-slate-700 dark:border-slate-800 dark:text-slate-200">Kritik Stok &Uuml;r&uuml;nleri</div>
        <DataTable :value="summary?.criticalStockProducts ?? []" :loading="loading" responsive-layout="scroll" class="p-2" stripedRows>
          <template #empty>
            <div class="py-8 text-center text-sm text-slate-500">Kritik stok &uuml;r&uuml;n&uuml; bulunmuyor.</div>
          </template>
          <Column field="sku" header="SKU" />
          <Column field="name" header="&Uuml;r&uuml;n" />
          <Column header="Durum">
            <template #body="{ data }">
              <Tag severity="warning" :value="`${data.totalQuantity} / min ${data.minimumStockLevel}`" />
            </template>
          </Column>
        </DataTable>
      </div>

      <div class="table-shell xl:col-span-3">
        <div class="border-b border-slate-200/70 px-4 py-3 text-sm font-semibold text-slate-700 dark:border-slate-800 dark:text-slate-200">Son Stok Hareketleri</div>
        <DataTable :value="summary?.recentMovements ?? []" :loading="loading" responsive-layout="scroll" class="p-2" stripedRows>
          <template #empty>
            <div class="py-8 text-center text-sm text-slate-500">Hareket bulunmuyor.</div>
          </template>
          <Column header="Tarih">
            <template #body="{ data }">{{ formatDate(data.createdDate) }}</template>
          </Column>
          <Column field="productName" header="&Uuml;r&uuml;n" />
          <Column header="Tip">
            <template #body="{ data }">
              <Tag :severity="movementSeverity(data.movementType)" :value="data.movementType" />
            </template>
          </Column>
          <Column field="quantity" header="Miktar" />
        </DataTable>
      </div>
    </div>
  </div>
</template>
