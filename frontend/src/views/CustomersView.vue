<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Tag from 'primevue/tag';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Dropdown from 'primevue/dropdown';
import { useToast } from 'primevue/usetoast';
import { api, type ApiResponse } from '@/services/api';
import { useAuthStore } from '@/stores/auth';

type CustomerRow = {
  id: string;
  name: string;
  companyName?: string | null;
  taxNumber?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  city?: string | null;
  notes?: string | null;
  isActive: boolean;
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

const rows = ref<CustomerRow[]>([]);
const total = ref(0);
const search = ref('');

const dialogVisible = ref(false);
const editingId = ref<string | null>(null);

const form = reactive<{
  name: string;
  companyName: string;
  taxNumber: string;
  phone: string;
  email: string;
  address: string;
  city: string;
  notes: string;
  isActive: boolean;
}>({
  name: '',
  companyName: '',
  taxNumber: '',
  phone: '',
  email: '',
  address: '',
  city: '',
  notes: '',
  isActive: true,
});

const activeOptions = [
  { label: 'Aktif', value: true },
  { label: 'Pasif', value: false },
];

const canManage = computed(() => auth.hasRole(['Admin', 'Manager']));
const dialogHeader = computed(() => (editingId.value ? 'M\u00fc\u015fteriyi D\u00fczenle' : 'Yeni M\u00fc\u015fteri Ekle'));

function resetForm() {
  form.name = '';
  form.companyName = '';
  form.taxNumber = '';
  form.phone = '';
  form.email = '';
  form.address = '';
  form.city = '';
  form.notes = '';
  form.isActive = true;
  editingId.value = null;
}

function openCreate() {
  resetForm();
  dialogVisible.value = true;
}

function openEdit(row: CustomerRow) {
  editingId.value = row.id;
  form.name = row.name;
  form.companyName = row.companyName ?? '';
  form.taxNumber = row.taxNumber ?? '';
  form.phone = row.phone ?? '';
  form.email = row.email ?? '';
  form.address = row.address ?? '';
  form.city = row.city ?? '';
  form.notes = row.notes ?? '';
  form.isActive = row.isActive;
  dialogVisible.value = true;
}

async function loadCustomers() {
  loading.value = true;
  try {
    const { data } = await api.get<ApiResponse<Paged<CustomerRow>>>('/api/customers', {
      params: { pageNumber: 1, pageSize: 200, search: search.value || undefined },
    });
    if (!data.success || !data.data) throw new Error(data.message || 'M\u00fc\u015fteri listesi al\u0131namad\u0131');
    rows.value = data.data.items;
    total.value = data.data.totalCount;
  } catch (e: unknown) {
    toast.add({
      severity: 'error',
      summary: 'Hata',
      detail: e instanceof Error ? e.message : 'M\u00fc\u015fteri listesi al\u0131namad\u0131',
      life: 3500,
    });
  } finally {
    loading.value = false;
  }
}

function buildPayload() {
  return {
    name: form.name.trim(),
    companyName: form.companyName.trim() || null,
    taxNumber: form.taxNumber.trim() || null,
    phone: form.phone.trim() || null,
    email: form.email.trim() || null,
    address: form.address.trim() || null,
    city: form.city.trim() || null,
    notes: form.notes.trim() || null,
    isActive: form.isActive,
  };
}

async function saveCustomer() {
  if (!form.name.trim()) {
    toast.add({ severity: 'warn', summary: 'Eksik bilgi', detail: 'M\u00fc\u015fteri ad\u0131 zorunludur.', life: 3000 });
    return;
  }

  saving.value = true;
  try {
    const payload = buildPayload();
    if (editingId.value) {
      const { data } = await api.put<ApiResponse<CustomerRow>>(`/api/customers/${editingId.value}`, payload);
      if (!data.success) throw new Error(data.message || 'G\u00fcncellenemedi');
      toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'M\u00fc\u015fteri g\u00fcncellendi.', life: 2500 });
    } else {
      const { data } = await api.post<ApiResponse<CustomerRow>>('/api/customers', payload);
      if (!data.success) throw new Error(data.message || 'Eklenemedi');
      toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'M\u00fc\u015fteri eklendi.', life: 2500 });
    }
    dialogVisible.value = false;
    resetForm();
    await loadCustomers();
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

async function deleteCustomer(row: CustomerRow) {
  const ok = window.confirm(`"${row.name}" m\u00fc\u015fterisini silmek istiyor musunuz?`);
  if (!ok) return;

  deletingId.value = row.id;
  try {
    const { data } = await api.delete<ApiResponse<unknown>>(`/api/customers/${row.id}`);
    if (!data.success) throw new Error(data.message || 'Silinemedi');
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'M\u00fc\u015fteri silindi.', life: 2500 });
    await loadCustomers();
  } catch (e: unknown) {
    toast.add({
      severity: 'error',
      summary: 'Hata',
      detail: e instanceof Error ? e.message : 'M\u00fc\u015fteri silinemedi',
      life: 3500,
    });
  } finally {
    deletingId.value = null;
  }
}

async function toggleStatus(row: CustomerRow) {
  updatingStatusId.value = row.id;
  const next = !row.isActive;
  try {
    const { data } = await api.put<ApiResponse<CustomerRow>>(`/api/customers/${row.id}`, {
      name: row.name,
      companyName: row.companyName ?? null,
      taxNumber: row.taxNumber ?? null,
      phone: row.phone ?? null,
      email: row.email ?? null,
      address: row.address ?? null,
      city: row.city ?? null,
      notes: row.notes ?? null,
      isActive: next,
    });
    if (!data.success) throw new Error(data.message || 'Durum g\u00fcncellenemedi');
    row.isActive = next;
    toast.add({
      severity: 'success',
      summary: 'Ba\u015far\u0131l\u0131',
      detail: next ? 'M\u00fc\u015fteri aktif yap\u0131ld\u0131.' : 'M\u00fc\u015fteri pasif yap\u0131ld\u0131.',
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

let searchDebounce: ReturnType<typeof setTimeout> | null = null;
function onSearchChange() {
  if (searchDebounce) clearTimeout(searchDebounce);
  searchDebounce = setTimeout(loadCustomers, 350);
}

onMounted(loadCustomers);
</script>

<template>
  <div class="space-y-5">
    <div class="surface-card flex flex-wrap items-end justify-between gap-4 p-5 md:p-6">
      <div>
        <h1 class="section-title">M&uuml;&#351;teri Y&ouml;netimi</h1>
        <p class="section-subtitle">M&uuml;&#351;teri kart&#305;n&#305; olu&#351;turun, ileti&#351;im bilgilerini g&uuml;ncelleyin ve aktiflik durumunu y&ouml;netin</p>
      </div>
      <div class="flex flex-wrap items-center gap-3">
        <span class="glass-pill">{{ total }} m&uuml;&#351;teri</span>
        <span class="p-input-icon-left">
          <i class="pi pi-search" />
          <InputText v-model="search" placeholder="Ara: ad, &#351;irket, e-posta" @input="onSearchChange" />
        </span>
        <Button v-if="canManage" label="Yeni M&uuml;&#351;teri" icon="pi pi-plus" @click="openCreate" />
      </div>
    </div>

    <div class="table-shell">
      <DataTable :value="rows" :loading="loading" responsive-layout="scroll" class="p-2" stripedRows>
        <template #empty>
          <div class="py-8 text-center text-sm text-slate-500">Kay&#305;tl&#305; m&uuml;&#351;teri bulunmuyor.</div>
        </template>
        <Column field="name" header="Ad" />
        <Column header="&#350;irket">
          <template #body="{ data }">{{ data.companyName || '&mdash;' }}</template>
        </Column>
        <Column header="&#350;ehir">
          <template #body="{ data }">{{ data.city || '&mdash;' }}</template>
        </Column>
        <Column header="Telefon">
          <template #body="{ data }">{{ data.phone || '&mdash;' }}</template>
        </Column>
        <Column header="E-posta">
          <template #body="{ data }">{{ data.email || '&mdash;' }}</template>
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
                @click="deleteCustomer(data)"
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
      :style="{ width: '52rem' }"
      @hide="resetForm"
    >
      <div class="user-dialog-hero">
        <div class="user-dialog-icon">
          <i class="pi pi-id-card text-lg" />
        </div>
        <div>
          <p class="user-dialog-title">{{ editingId ? 'M&uuml;&#351;teri bilgilerini g&uuml;ncelle' : 'Yeni m&uuml;&#351;teri kart&#305; olu&#351;tur' }}</p>
          <p class="user-dialog-subtitle">Ad zorunludur. Di&#287;er alanlar opsiyoneldir; doldurduk&ccedil;a m&uuml;&#351;teri profili zenginle&#351;ir.</p>
        </div>
      </div>

      <div class="user-dialog-section">
        <div class="grid grid-cols-2 gap-3">
          <div class="grid gap-2">
            <label class="user-field-label">Ad / Soyad</label>
            <InputText v-model="form.name" placeholder="&Ouml;r: Ay&#351;e Demir" />
          </div>
          <div class="grid gap-2">
            <label class="user-field-label">&#350;irket</label>
            <InputText v-model="form.companyName" placeholder="&Ouml;r: Demir Tekstil A.&#350;." />
          </div>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div class="grid gap-2">
            <label class="user-field-label">Vergi / TC No</label>
            <InputText v-model="form.taxNumber" placeholder="opsiyonel" />
          </div>
          <div class="grid gap-2">
            <label class="user-field-label">Telefon</label>
            <InputText v-model="form.phone" placeholder="&Ouml;r: +90 555 000 0000" />
          </div>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div class="grid gap-2">
            <label class="user-field-label">E-posta</label>
            <InputText v-model="form.email" placeholder="ornek@firma.com" />
          </div>
          <div class="grid gap-2">
            <label class="user-field-label">&#350;ehir</label>
            <InputText v-model="form.city" placeholder="&Ouml;r: &#304;stanbul" />
          </div>
        </div>
        <div class="grid gap-2">
          <label class="user-field-label">Adres</label>
          <InputText v-model="form.address" placeholder="A&ccedil;&#305;k adres" />
        </div>
        <div class="grid grid-cols-3 gap-3">
          <div class="grid gap-2 col-span-2">
            <label class="user-field-label">Notlar</label>
            <Textarea v-model="form.notes" rows="3" autoResize placeholder="opsiyonel" />
          </div>
          <div class="grid gap-2">
            <label class="user-field-label">Durum</label>
            <Dropdown v-model="form.isActive" :options="activeOptions" optionLabel="label" optionValue="value" />
          </div>
        </div>
      </div>

      <template #footer>
        <div class="product-dialog-footer">
          <Button label="Vazge&ccedil;" icon="pi pi-times" class="product-btn product-btn-cancel" @click="dialogVisible = false" />
          <Button
            :label="editingId ? 'De&#287;i&#351;iklikleri Kaydet' : 'M&uuml;&#351;teriyi Kaydet'"
            icon="pi pi-check"
            class="product-btn product-btn-save"
            :loading="saving"
            @click="saveCustomer"
          />
        </div>
      </template>
    </Dialog>
  </div>
</template>
