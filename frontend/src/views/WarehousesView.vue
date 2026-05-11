<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Tag from 'primevue/tag';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Dropdown from 'primevue/dropdown';
import { useToast } from 'primevue/usetoast';
import { api, type ApiResponse } from '@/services/api';
import { useAuthStore } from '@/stores/auth';

type WarehouseRow = {
  id: string;
  name: string;
  address?: string | null;
  city?: string | null;
  isActive: boolean;
  locationCount: number;
};

type LocationRow = {
  id: string;
  corridor: string;
  shelf: string;
  floor: string;
  code: string;
  warehouseId: string;
  warehouseName: string;
  maxCapacity?: number | null;
  pickSortOrder?: number | null;
};

type Paged<T> = {
  items: T[];
  totalCount: number;
};

const toast = useToast();
const auth = useAuthStore();

const loading = ref(false);
const saving = ref(false);
const deletingId = ref<string | null>(null);
const updatingStatusId = ref<string | null>(null);

const rows = ref<WarehouseRow[]>([]);
const total = ref(0);

const dialogVisible = ref(false);
const editingId = ref<string | null>(null);

const form = reactive<{ name: string; address: string; city: string; isActive: boolean }>({
  name: '',
  address: '',
  city: '',
  isActive: true,
});

const activeOptions = [
  { label: 'Aktif', value: true },
  { label: 'Pasif', value: false },
];

const canManage = computed(() => auth.hasRole(['Admin', 'Manager']));
const canEditLocations = computed(() => auth.hasRole(['Admin', 'Manager', 'WarehouseStaff']));
const canDeleteLocations = computed(() => auth.hasRole(['Admin', 'Manager']));
const dialogHeader = computed(() => (editingId.value ? 'Depoyu D\u00fczenle' : 'Yeni Depo Ekle'));

const locationDialogVisible = ref(false);
const locationsLoading = ref(false);
const locationSaving = ref(false);
const locationDeletingId = ref<string | null>(null);
const selectedWarehouse = ref<WarehouseRow | null>(null);
const locations = ref<LocationRow[]>([]);
const editingLocationId = ref<string | null>(null);

const locationForm = reactive<{
  corridor: string;
  shelf: string;
  floor: string;
  maxCapacity: number | null;
  pickSortOrder: number | null;
}>({
  corridor: '',
  shelf: '',
  floor: '',
  maxCapacity: null,
  pickSortOrder: null,
});

const locationDialogHeader = computed(() =>
  selectedWarehouse.value ? `Lokasyonlar &middot; ${selectedWarehouse.value.name}` : 'Lokasyonlar',
);

function resetForm() {
  form.name = '';
  form.address = '';
  form.city = '';
  form.isActive = true;
  editingId.value = null;
}

function openCreate() {
  resetForm();
  dialogVisible.value = true;
}

function openEdit(row: WarehouseRow) {
  editingId.value = row.id;
  form.name = row.name;
  form.address = row.address ?? '';
  form.city = row.city ?? '';
  form.isActive = row.isActive;
  dialogVisible.value = true;
}

async function loadWarehouses() {
  loading.value = true;
  try {
    const { data } = await api.get<ApiResponse<Paged<WarehouseRow>>>('/api/warehouses', {
      params: { pageNumber: 1, pageSize: 100 },
    });
    if (!data.success || !data.data) throw new Error(data.message || 'Depo listesi al\u0131namad\u0131');
    rows.value = data.data.items;
    total.value = data.data.totalCount;
  } catch (e: unknown) {
    toast.add({
      severity: 'error',
      summary: 'Hata',
      detail: e instanceof Error ? e.message : 'Depo listesi al\u0131namad\u0131',
      life: 3500,
    });
  } finally {
    loading.value = false;
  }
}

async function saveWarehouse() {
  if (!form.name.trim()) {
    toast.add({ severity: 'warn', summary: 'Eksik bilgi', detail: 'Depo ad\u0131 zorunludur.', life: 3000 });
    return;
  }

  saving.value = true;
  try {
    const payload = {
      name: form.name.trim(),
      address: form.address.trim() || null,
      city: form.city.trim() || null,
      isActive: form.isActive,
    };

    if (editingId.value) {
      const { data } = await api.put<ApiResponse<WarehouseRow>>(`/api/warehouses/${editingId.value}`, payload);
      if (!data.success) throw new Error(data.message || 'Depo g\u00fcncellenemedi');
      toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Depo g\u00fcncellendi.', life: 2500 });
    } else {
      const { data } = await api.post<ApiResponse<WarehouseRow>>('/api/warehouses', payload);
      if (!data.success) throw new Error(data.message || 'Depo eklenemedi');
      toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Depo eklendi.', life: 2500 });
    }

    dialogVisible.value = false;
    resetForm();
    await loadWarehouses();
  } catch (e: unknown) {
    toast.add({
      severity: 'error',
      summary: 'Hata',
      detail: e instanceof Error ? e.message : 'Kaydetme ba\u015far\u0131s\u0131z',
      life: 3500,
    });
  } finally {
    saving.value = false;
  }
}

async function deleteWarehouse(row: WarehouseRow) {
  const ok = window.confirm(`"${row.name}" deposunu silmek istiyor musunuz?`);
  if (!ok) return;

  deletingId.value = row.id;
  try {
    const { data } = await api.delete<ApiResponse<unknown>>(`/api/warehouses/${row.id}`);
    if (!data.success) throw new Error(data.message || 'Depo silinemedi');
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Depo silindi.', life: 2500 });
    await loadWarehouses();
  } catch (e: unknown) {
    toast.add({
      severity: 'error',
      summary: 'Hata',
      detail: e instanceof Error ? e.message : 'Depo silinemedi',
      life: 3500,
    });
  } finally {
    deletingId.value = null;
  }
}

function resetLocationForm() {
  locationForm.corridor = '';
  locationForm.shelf = '';
  locationForm.floor = '';
  locationForm.maxCapacity = null;
  locationForm.pickSortOrder = null;
  editingLocationId.value = null;
}

async function openLocations(row: WarehouseRow) {
  selectedWarehouse.value = row;
  resetLocationForm();
  locationDialogVisible.value = true;
  await loadLocations(row.id);
}

async function loadLocations(warehouseId: string) {
  locationsLoading.value = true;
  try {
    const { data } = await api.get<ApiResponse<Paged<LocationRow>>>('/api/locations', {
      params: { pageNumber: 1, pageSize: 200, warehouseId },
    });
    if (!data.success || !data.data) throw new Error(data.message || 'Lokasyonlar al\u0131namad\u0131');
    locations.value = data.data.items;
  } catch (e: unknown) {
    toast.add({
      severity: 'error',
      summary: 'Hata',
      detail: e instanceof Error ? e.message : 'Lokasyonlar al\u0131namad\u0131',
      life: 3500,
    });
  } finally {
    locationsLoading.value = false;
  }
}

function startEditLocation(loc: LocationRow) {
  editingLocationId.value = loc.id;
  locationForm.corridor = loc.corridor;
  locationForm.shelf = loc.shelf;
  locationForm.floor = loc.floor;
  locationForm.maxCapacity = loc.maxCapacity ?? null;
  locationForm.pickSortOrder = loc.pickSortOrder ?? null;
}

async function saveLocation() {
  if (!selectedWarehouse.value) return;

  const corridor = locationForm.corridor.trim();
  const shelf = locationForm.shelf.trim();
  const floor = locationForm.floor.trim();

  if (!corridor || !shelf || !floor) {
    toast.add({
      severity: 'warn',
      summary: 'Eksik bilgi',
      detail: 'Koridor, raf ve kat alanlar\u0131 zorunludur.',
      life: 3000,
    });
    return;
  }

  locationSaving.value = true;
  try {
    if (editingLocationId.value) {
      const { data } = await api.put<ApiResponse<LocationRow>>(`/api/locations/${editingLocationId.value}`, {
        corridor,
        shelf,
        floor,
        maxCapacity: locationForm.maxCapacity ?? null,
        pickSortOrder: locationForm.pickSortOrder ?? null,
      });
      if (!data.success) throw new Error(data.message || 'Lokasyon g\u00fcncellenemedi');
      toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Lokasyon g\u00fcncellendi.', life: 2200 });
    } else {
      const { data } = await api.post<ApiResponse<LocationRow>>('/api/locations', {
        warehouseId: selectedWarehouse.value.id,
        corridor,
        shelf,
        floor,
        maxCapacity: locationForm.maxCapacity ?? null,
        pickSortOrder: locationForm.pickSortOrder ?? null,
      });
      if (!data.success) throw new Error(data.message || 'Lokasyon eklenemedi');
      toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Lokasyon eklendi.', life: 2200 });
    }

    resetLocationForm();
    await loadLocations(selectedWarehouse.value.id);
    await loadWarehouses();
  } catch (e: unknown) {
    toast.add({
      severity: 'error',
      summary: 'Hata',
      detail: e instanceof Error ? e.message : 'Kaydetme ba\u015far\u0131s\u0131z',
      life: 3500,
    });
  } finally {
    locationSaving.value = false;
  }
}

async function deleteLocation(loc: LocationRow) {
  if (!selectedWarehouse.value) return;
  const ok = window.confirm(`"${loc.code}" lokasyonunu silmek istiyor musunuz?`);
  if (!ok) return;

  locationDeletingId.value = loc.id;
  try {
    const { data } = await api.delete<ApiResponse<unknown>>(`/api/locations/${loc.id}`);
    if (!data.success) throw new Error(data.message || 'Lokasyon silinemedi');
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Lokasyon silindi.', life: 2200 });
    await loadLocations(selectedWarehouse.value.id);
    await loadWarehouses();
  } catch (e: unknown) {
    toast.add({
      severity: 'error',
      summary: 'Hata',
      detail: e instanceof Error ? e.message : 'Lokasyon silinemedi',
      life: 3500,
    });
  } finally {
    locationDeletingId.value = null;
  }
}

function onLocationDialogHide() {
  selectedWarehouse.value = null;
  locations.value = [];
  resetLocationForm();
}

async function toggleStatus(row: WarehouseRow) {
  updatingStatusId.value = row.id;
  const next = !row.isActive;
  try {
    const { data } = await api.put<ApiResponse<WarehouseRow>>(`/api/warehouses/${row.id}`, {
      name: row.name,
      address: row.address ?? null,
      city: row.city ?? null,
      isActive: next,
    });
    if (!data.success) throw new Error(data.message || 'Durum g\u00fcncellenemedi');
    row.isActive = next;
    toast.add({
      severity: 'success',
      summary: 'Ba\u015far\u0131l\u0131',
      detail: next ? 'Depo aktif yap\u0131ld\u0131.' : 'Depo pasif yap\u0131ld\u0131.',
      life: 2500,
    });
  } catch (e: unknown) {
    toast.add({
      severity: 'error',
      summary: 'Hata',
      detail: e instanceof Error ? e.message : 'Durum g\u00fcncelleme ba\u015far\u0131s\u0131z',
      life: 3500,
    });
  } finally {
    updatingStatusId.value = null;
  }
}

onMounted(loadWarehouses);
</script>

<template>
  <div class="space-y-5">
    <div class="surface-card flex flex-wrap items-end justify-between gap-4 p-5 md:p-6">
      <div>
        <h1 class="section-title">Depo Y&ouml;netimi</h1>
        <p class="section-subtitle">Depolar&#305; ekleyin, ad &amp; adres bilgilerini d&uuml;zenleyin veya aktiflik durumunu de&#287;i&#351;tirin</p>
      </div>
      <div class="flex items-center gap-3">
        <span class="glass-pill">{{ total }} depo</span>
        <Button v-if="canManage" label="Yeni Depo" icon="pi pi-plus" @click="openCreate" />
      </div>
    </div>

    <div class="table-shell">
      <DataTable :value="rows" :loading="loading" responsive-layout="scroll" class="p-2" stripedRows>
        <template #empty>
          <div class="py-8 text-center text-sm text-slate-500">Kay&#305;tl&#305; depo bulunmuyor.</div>
        </template>
        <Column field="name" header="Ad" />
        <Column field="city" header="&#350;ehir">
          <template #body="{ data }">{{ data.city || '&mdash;' }}</template>
        </Column>
        <Column field="address" header="Adres">
          <template #body="{ data }">{{ data.address || '&mdash;' }}</template>
        </Column>
        <Column header="Lokasyon">
          <template #body="{ data }">
            <Button
              :label="String(data.locationCount)"
              icon="pi pi-th-large"
              class="p-button-sm"
              severity="secondary"
              text
              @click="openLocations(data)"
            />
          </template>
        </Column>
        <Column header="Durum">
          <template #body="{ data }">
            <div class="flex items-center gap-2">
              <Tag :severity="data.isActive ? 'success' : 'danger'" :value="data.isActive ? 'Aktif' : 'Pasif'" />
              <Button
                v-if="canManage"
                :label="data.isActive ? 'Pasife Al' : 'Aktif Et'"
                :icon="data.isActive ? 'pi pi-eye-slash' : 'pi pi-check-circle'"
                class="p-button-sm"
                severity="secondary"
                text
                :loading="updatingStatusId === data.id"
                @click="toggleStatus(data)"
              />
            </div>
          </template>
        </Column>
        <Column header="&#304;&#351;lem">
          <template #body="{ data }">
            <div class="flex flex-wrap items-center gap-1">
              <Button
                icon="pi pi-th-large"
                label="Lokasyonlar"
                severity="info"
                text
                @click="openLocations(data)"
              />
              <Button
                icon="pi pi-pencil"
                label="D&uuml;zenle"
                severity="secondary"
                text
                :disabled="!canManage"
                @click="openEdit(data)"
              />
              <Button
                icon="pi pi-trash"
                label="Sil"
                severity="danger"
                text
                :disabled="!canManage"
                :loading="deletingId === data.id"
                @click="deleteWarehouse(data)"
              />
            </div>
          </template>
        </Column>
      </DataTable>
    </div>

    <Dialog
      v-model:visible="dialogVisible"
      modal
      :header="dialogHeader"
      class="user-create-dialog"
      :style="{ width: '42rem' }"
      @hide="resetForm"
    >
      <div class="user-dialog-hero">
        <div class="user-dialog-icon">
          <i class="pi pi-building text-lg" />
        </div>
        <div>
          <p class="user-dialog-title">{{ editingId ? 'Depo bilgilerini g&uuml;ncelle' : 'Yeni depo olu&#351;tur' }}</p>
          <p class="user-dialog-subtitle">Ad, &#351;ehir ve adres bilgilerini girin; aktiflik durumunu se&ccedil;in.</p>
        </div>
      </div>

      <div class="user-dialog-section">
        <div class="grid gap-2">
          <label class="user-field-label">Depo Ad&#305;</label>
          <InputText v-model="form.name" placeholder="&Ouml;r: Merkez Depo" />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div class="grid gap-2">
            <label class="user-field-label">&#350;ehir</label>
            <InputText v-model="form.city" placeholder="&Ouml;r: &#304;stanbul" />
          </div>
          <div class="grid gap-2">
            <label class="user-field-label">Durum</label>
            <Dropdown v-model="form.isActive" :options="activeOptions" optionLabel="label" optionValue="value" />
          </div>
        </div>
        <div class="grid gap-2">
          <label class="user-field-label">Adres</label>
          <InputText v-model="form.address" placeholder="A&ccedil;&#305;k adres" />
        </div>
      </div>

      <template #footer>
        <div class="product-dialog-footer">
          <Button label="Vazge&ccedil;" icon="pi pi-times" class="product-btn product-btn-cancel" @click="dialogVisible = false" />
          <Button
            :label="editingId ? 'De&#287;i&#351;iklikleri Kaydet' : 'Depoyu Kaydet'"
            icon="pi pi-check"
            class="product-btn product-btn-save"
            :loading="saving"
            @click="saveWarehouse"
          />
        </div>
      </template>
    </Dialog>

    <Dialog
      v-model:visible="locationDialogVisible"
      modal
      :header="locationDialogHeader"
      class="user-create-dialog"
      :style="{ width: '64rem' }"
      @hide="onLocationDialogHide"
    >
      <div class="user-dialog-hero">
        <div class="user-dialog-icon">
          <i class="pi pi-th-large text-lg" />
        </div>
        <div>
          <p class="user-dialog-title">Depoya ait lokasyonlar</p>
          <p class="user-dialog-subtitle">
            Koridor, raf ve kat bilgilerini girerek yeni lokasyon ekleyin; kapasite ve toplama s&#305;ras&#305; opsiyoneldir.
          </p>
        </div>
      </div>

      <div v-if="canEditLocations" class="user-dialog-section">
        <div class="grid grid-cols-1 gap-3 md:grid-cols-5">
          <div class="grid gap-2">
            <label class="user-field-label">Koridor</label>
            <InputText v-model="locationForm.corridor" placeholder="&Ouml;r: A" />
          </div>
          <div class="grid gap-2">
            <label class="user-field-label">Raf</label>
            <InputText v-model="locationForm.shelf" placeholder="&Ouml;r: 01" />
          </div>
          <div class="grid gap-2">
            <label class="user-field-label">Kat</label>
            <InputText v-model="locationForm.floor" placeholder="&Ouml;r: B-03" />
          </div>
          <div class="grid gap-2">
            <label class="user-field-label">Kapasite</label>
            <InputNumber v-model="locationForm.maxCapacity" :min="0" placeholder="opsiyonel" showButtons buttonLayout="horizontal" />
          </div>
          <div class="grid gap-2">
            <label class="user-field-label">Toplama S&#305;ras&#305;</label>
            <InputNumber v-model="locationForm.pickSortOrder" :min="0" placeholder="opsiyonel" showButtons buttonLayout="horizontal" />
          </div>
        </div>
        <div class="mt-3 flex justify-end gap-2">
          <Button
            v-if="editingLocationId"
            label="Vazge&ccedil;"
            icon="pi pi-times"
            severity="secondary"
            text
            @click="resetLocationForm"
          />
          <Button
            :label="editingLocationId ? 'Lokasyonu G&uuml;ncelle' : 'Lokasyon Ekle'"
            :icon="editingLocationId ? 'pi pi-check' : 'pi pi-plus'"
            :loading="locationSaving"
            @click="saveLocation"
          />
        </div>
      </div>

      <div class="table-shell mt-4">
        <DataTable :value="locations" :loading="locationsLoading" responsive-layout="scroll" class="p-2" stripedRows>
          <template #empty>
            <div class="py-8 text-center text-sm text-slate-500">Bu depoda hen&uuml;z lokasyon yok.</div>
          </template>
          <Column field="code" header="Kod" />
          <Column field="corridor" header="Koridor" />
          <Column field="shelf" header="Raf" />
          <Column field="floor" header="Kat" />
          <Column header="Kapasite">
            <template #body="{ data }">{{ data.maxCapacity ?? '&mdash;' }}</template>
          </Column>
          <Column header="Toplama">
            <template #body="{ data }">{{ data.pickSortOrder ?? '&mdash;' }}</template>
          </Column>
          <Column header="&#304;&#351;lem">
            <template #body="{ data }">
              <div class="flex flex-wrap items-center gap-1">
                <Button
                  icon="pi pi-pencil"
                  label="D&uuml;zenle"
                  severity="secondary"
                  text
                  :disabled="!canEditLocations"
                  @click="startEditLocation(data)"
                />
                <Button
                  icon="pi pi-trash"
                  label="Sil"
                  severity="danger"
                  text
                  :disabled="!canDeleteLocations"
                  :loading="locationDeletingId === data.id"
                  @click="deleteLocation(data)"
                />
              </div>
            </template>
          </Column>
        </DataTable>
      </div>

      <template #footer>
        <div class="product-dialog-footer">
          <Button label="Kapat" icon="pi pi-times" class="product-btn product-btn-cancel" @click="locationDialogVisible = false" />
        </div>
      </template>
    </Dialog>
  </div>
</template>
