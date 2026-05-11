<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Tag from 'primevue/tag';
import Dropdown from 'primevue/dropdown';
import Button from 'primevue/button';
import InputNumber from 'primevue/inputnumber';
import InputText from 'primevue/inputtext';
import { useToast } from 'primevue/usetoast';
import { api, type ApiResponse } from '@/services/api';
import { useAuthStore } from '@/stores/auth';

const auth = useAuthStore();
const canWriteStock = computed(() => auth.hasRole(['Admin', 'Manager', 'WarehouseStaff']));

type Movement = {
  id: string;
  movementType: string;
  quantity: number;
  productName: string;
  fromLocationCode?: string | null;
  toLocationCode?: string | null;
  userFullName: string;
  createdDate: string;
};

type ProductOption = {
  id: string;
  name: string;
  sku: string;
};

type LocationOption = {
  id: string;
  code: string;
  warehouseName: string;
};

type Paged<T> = {
  items: T[];
  totalCount: number;
};

const toast = useToast();
const loading = ref(false);
const submittingEntry = ref(false);
const submittingExit = ref(false);
const rows = ref<Movement[]>([]);
const selectedType = ref<string | null>(null);
const products = ref<ProductOption[]>([]);
const locations = ref<LocationOption[]>([]);

const entryForm = reactive({
  productId: null as string | null,
  locationId: null as string | null,
  quantity: null as number | null,
  description: '',
});

const exitForm = reactive({
  productId: null as string | null,
  locationId: null as string | null,
  quantity: null as number | null,
  description: '',
});

const movementTypes = [
  { label: 'T\u00fcm\u00fc', value: null },
  { label: 'Entry', value: 'entry' },
  { label: 'Exit', value: 'exit' },
  { label: 'Transfer', value: 'transfer' },
  { label: 'CountDifference', value: 'countdifference' },
];

const productOptions = computed(() =>
  products.value.map((p) => ({
    label: `${p.sku} - ${p.name}`,
    value: p.id,
  })),
);

const locationOptions = computed(() =>
  locations.value.map((l) => ({
    label: `${l.code} (${l.warehouseName})`,
    value: l.id,
  })),
);

const filtered = computed(() => {
  if (!selectedType.value) return rows.value;
  return rows.value.filter((x) => x.movementType.toLowerCase() === selectedType.value);
});

function severity(type: string) {
  switch (type.toLowerCase()) {
    case 'entry': return 'success';
    case 'exit': return 'danger';
    case 'transfer': return 'info';
    default: return 'warning';
  }
}

function formatDate(value: string) {
  return new Date(value).toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' });
}

async function loadMovements() {
  loading.value = true;
  try {
    const { data } = await api.get<ApiResponse<Paged<Movement>>>('/api/stock/movements', {
      params: { pageNumber: 1, pageSize: 100, sortDescending: true },
    });
    if (!data.success || !data.data) throw new Error(data.message || 'Hareket listesi al\u0131namad\u0131');
    rows.value = data.data.items;
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Hata', life: 3500 });
  } finally {
    loading.value = false;
  }
}

async function loadProducts() {
  const { data } = await api.get<ApiResponse<Paged<ProductOption>>>('/api/products', {
    params: { pageNumber: 1, pageSize: 200, isActive: true },
  });
  if (!data.success || !data.data) throw new Error(data.message || '\u00dcr\u00fcnler al\u0131namad\u0131');
  products.value = data.data.items;
}

async function loadLocations() {
  const { data } = await api.get<ApiResponse<Paged<LocationOption>>>('/api/locations', {
    params: { pageNumber: 1, pageSize: 200 },
  });
  if (!data.success || !data.data) throw new Error(data.message || 'Lokasyonlar al\u0131namad\u0131');
  locations.value = data.data.items;
}

async function submitEntry() {
  if (!entryForm.productId || !entryForm.locationId || !entryForm.quantity || entryForm.quantity <= 0) {
    toast.add({ severity: 'warn', summary: 'Eksik bilgi', detail: 'L\u00fctfen \u00fcr\u00fcn, lokasyon ve miktar girin.', life: 3000 });
    return;
  }

  submittingEntry.value = true;
  try {
    const { data } = await api.post<ApiResponse<unknown>>('/api/stock/entry', {
      productId: entryForm.productId,
      locationId: entryForm.locationId,
      quantity: entryForm.quantity,
      description: entryForm.description || null,
    });
    if (!data.success) throw new Error(data.message || 'Stok giri\u015fi yap\u0131lamad\u0131');

    entryForm.quantity = null;
    entryForm.description = '';
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Stok giri\u015fi kaydedildi.', life: 2500 });
    await loadMovements();
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : '\u0130\u015flem ba\u015far\u0131s\u0131z', life: 3500 });
  } finally {
    submittingEntry.value = false;
  }
}

async function submitExit() {
  if (!exitForm.productId || !exitForm.locationId || !exitForm.quantity || exitForm.quantity <= 0) {
    toast.add({ severity: 'warn', summary: 'Eksik bilgi', detail: 'L\u00fctfen \u00fcr\u00fcn, lokasyon ve miktar girin.', life: 3000 });
    return;
  }

  submittingExit.value = true;
  try {
    const { data } = await api.post<ApiResponse<unknown>>('/api/stock/exit', {
      productId: exitForm.productId,
      locationId: exitForm.locationId,
      quantity: exitForm.quantity,
      description: exitForm.description || null,
    });
    if (!data.success) throw new Error(data.message || 'Stok \u00e7\u0131k\u0131\u015f\u0131 yap\u0131lamad\u0131');

    exitForm.quantity = null;
    exitForm.description = '';
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Stok \u00e7\u0131k\u0131\u015f\u0131 kaydedildi.', life: 2500 });
    await loadMovements();
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : '\u0130\u015flem ba\u015far\u0131s\u0131z', life: 3500 });
  } finally {
    submittingExit.value = false;
  }
}

onMounted(async () => {
  try {
    await Promise.all([loadProducts(), loadLocations(), loadMovements()]);
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Ba\u015flang\u0131\u00e7 verileri al\u0131namad\u0131', life: 4000 });
  }
});
</script>

<template>
  <div class="space-y-5">
    <div class="surface-card flex flex-wrap items-end justify-between gap-4 p-5 md:p-6">
      <div>
        <h1 class="section-title">Stok Hareket &#304;&#351;lemleri</h1>
        <p class="section-subtitle">&Uuml;r&uuml;n baz&#305;nda stok giri&#351;/&ccedil;&#305;k&#305;&#351; i&#351;lemlerini ger&ccedil;ekle&#351;tirin ve ge&ccedil;mi&#351;i takip edin</p>
      </div>
      <span class="glass-pill">{{ filtered.length }} hareket</span>
    </div>

    <div v-if="canWriteStock" class="grid gap-4 lg:grid-cols-2">
      <div class="surface-card p-4">
        <h2 class="mb-3 text-sm font-semibold uppercase tracking-wide text-emerald-600">Stok Giri&#351;</h2>
        <div class="space-y-3">
          <Dropdown v-model="entryForm.productId" :options="productOptions" optionLabel="label" optionValue="value" placeholder="&Uuml;r&uuml;n se&ccedil;in" class="w-full" />
          <Dropdown v-model="entryForm.locationId" :options="locationOptions" optionLabel="label" optionValue="value" placeholder="Lokasyon se&ccedil;in" class="w-full" />
          <InputNumber v-model="entryForm.quantity" :min="1" showButtons buttonLayout="horizontal" decrementButtonClass="p-button-text" incrementButtonClass="p-button-text" class="w-full" placeholder="Miktar" />
          <InputText v-model="entryForm.description" class="w-full" placeholder="A&ccedil;&#305;klama (opsiyonel)" />
          <Button label="Stok Giri&#351;i Kaydet" icon="pi pi-check" severity="success" class="w-full" :loading="submittingEntry" @click="submitEntry" />
        </div>
      </div>

      <div class="surface-card p-4">
        <h2 class="mb-3 text-sm font-semibold uppercase tracking-wide text-rose-600">Stok &Ccedil;&#305;k&#305;&#351;</h2>
        <div class="space-y-3">
          <Dropdown v-model="exitForm.productId" :options="productOptions" optionLabel="label" optionValue="value" placeholder="&Uuml;r&uuml;n se&ccedil;in" class="w-full" />
          <Dropdown v-model="exitForm.locationId" :options="locationOptions" optionLabel="label" optionValue="value" placeholder="Lokasyon se&ccedil;in" class="w-full" />
          <InputNumber v-model="exitForm.quantity" :min="1" showButtons buttonLayout="horizontal" decrementButtonClass="p-button-text" incrementButtonClass="p-button-text" class="w-full" placeholder="Miktar" />
          <InputText v-model="exitForm.description" class="w-full" placeholder="A&ccedil;&#305;klama (opsiyonel)" />
          <Button label="Stok &Ccedil;&#305;k&#305;&#351;&#305; Kaydet" icon="pi pi-upload" severity="danger" class="w-full" :loading="submittingExit" @click="submitExit" />
        </div>
      </div>
    </div>

    <div class="surface-card p-4">
      <div class="max-w-xs">
        <label class="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-500">Hareket tipi</label>
        <Dropdown v-model="selectedType" :options="movementTypes" optionLabel="label" optionValue="value" class="w-full" />
      </div>
    </div>

    <div class="table-shell">
      <DataTable :value="filtered" :loading="loading" responsive-layout="scroll" class="p-2" stripedRows>
        <template #empty>
          <div class="py-8 text-center text-sm text-slate-500">Se&ccedil;ilen filtreye uygun hareket bulunamad&#305;.</div>
        </template>
        <Column header="Tarih">
          <template #body="{ data }">{{ formatDate(data.createdDate) }}</template>
        </Column>
        <Column field="productName" header="&Uuml;r&uuml;n" />
        <Column header="Tip">
          <template #body="{ data }">
            <Tag :severity="severity(data.movementType)" :value="data.movementType" />
          </template>
        </Column>
        <Column field="quantity" header="Miktar" />
        <Column header="Kaynak">
          <template #body="{ data }">{{ data.fromLocationCode ?? '--' }}</template>
        </Column>
        <Column header="Hedef">
          <template #body="{ data }">{{ data.toLocationCode ?? '--' }}</template>
        </Column>
        <Column field="userFullName" header="Operat&ouml;r" />
      </DataTable>
    </div>
  </div>
</template>
