<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Tag from 'primevue/tag';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Textarea from 'primevue/textarea';
import Dropdown from 'primevue/dropdown';
import { useToast } from 'primevue/usetoast';
import { api, type ApiResponse } from '@/services/api';
import { useAuthStore } from '@/stores/auth';

type Status = 1 | 2 | 3 | 4 | 5;

type LineDto = {
  id?: string;
  productId: string;
  productName?: string;
  sku?: string;
  quantity: number;
  notes?: string | null;
};

type QuoteDto = {
  id: string;
  supplierId: string;
  supplierName: string;
  totalAmount: number;
  currency: string;
  status: number;
};

type RequisitionRow = {
  id: string;
  requestNumber: string;
  title?: string | null;
  status: Status;
  warehouseId: string;
  warehouseName: string;
  requestedByUserId: string;
  requestedByName: string;
  approvedByUserId?: string | null;
  approvedByName?: string | null;
  approvedDate?: string | null;
  notes?: string | null;
  lines: LineDto[];
  quotes: QuoteDto[];
};

type Paged<T> = { items: T[]; totalCount: number };
type ProductRow = { id: string; name: string; sku: string };
type WarehouseRow = { id: string; name: string };
type SupplierRow = { id: string; name: string };
type SuggestionRow = {
  productId: string;
  name: string;
  sku: string;
  currentQuantity: number;
  minimumStockLevel: number;
  suggestedOrderQuantity: number;
};

const toast = useToast();
const auth = useAuthStore();

const loading = ref(false);
const rows = ref<RequisitionRow[]>([]);
const total = ref(0);

const statusFilter = ref<Status | null>(null);

const products = ref<ProductRow[]>([]);
const warehouses = ref<WarehouseRow[]>([]);
const suppliers = ref<SupplierRow[]>([]);
const suggestions = ref<SuggestionRow[]>([]);

const createDialogVisible = ref(false);
const createSaving = ref(false);

const createForm = reactive<{
  warehouseId: string;
  title: string;
  notes: string;
  lines: Array<{ productId: string; quantity: number; notes: string }>;
}>({
  warehouseId: '',
  title: '',
  notes: '',
  lines: [],
});

const detailDialogVisible = ref(false);
const detailRow = ref<RequisitionRow | null>(null);
const detailBusy = ref(false);

const quoteForm = reactive<{ supplierId: string; totalAmount: number | null; currency: string; notes: string }>({
  supplierId: '',
  totalAmount: null,
  currency: 'TRY',
  notes: '',
});
const quoteSaving = ref(false);

const canManage = computed(() => auth.hasRole(['Admin', 'Manager', 'Operations']));
const canApprove = computed(() => auth.hasRole(['Admin', 'Manager']));

const statusOptions: Array<{ label: string; value: Status | null }> = [
  { label: 'T&uuml;m\u00fc', value: null },
  { label: 'Taslak', value: 1 },
  { label: 'Onay Bekliyor', value: 2 },
  { label: 'Onayland\u0131', value: 3 },
  { label: 'Reddedildi', value: 4 },
  { label: 'Tamamland\u0131', value: 5 },
];

function statusLabel(s: number) {
  switch (s) {
    case 1: return 'Taslak';
    case 2: return 'Onay Bekliyor';
    case 3: return 'Onayland\u0131';
    case 4: return 'Reddedildi';
    case 5: return 'Tamamland\u0131';
    default: return String(s);
  }
}

function statusSeverity(s: number) {
  switch (s) {
    case 2: return 'warning';
    case 3: return 'success';
    case 4: return 'danger';
    case 5: return 'info';
    default: return 'secondary';
  }
}

function quoteStatusLabel(s: number) {
  switch (s) {
    case 1: return 'Bekliyor';
    case 2: return 'Kabul';
    case 3: return 'Red';
    default: return String(s);
  }
}

function quoteStatusSeverity(s: number) {
  switch (s) {
    case 1: return 'warning';
    case 2: return 'success';
    case 3: return 'danger';
    default: return 'secondary';
  }
}

function formatMoney(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: currency || 'TRY' }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

async function load() {
  loading.value = true;
  try {
    const { data } = await api.get<ApiResponse<Paged<RequisitionRow>>>('/api/purchase-requisitions', {
      params: { pageNumber: 1, pageSize: 100, status: statusFilter.value ?? undefined },
    });
    if (!data.success || !data.data) throw new Error(data.message || 'Liste al\u0131namad\u0131');
    rows.value = data.data.items;
    total.value = data.data.totalCount;
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Y\u00fckleme hatas\u0131', life: 3500 });
  } finally {
    loading.value = false;
  }
}

async function loadLookups() {
  try {
    const [w, p, s] = await Promise.all([
      api.get<ApiResponse<Paged<WarehouseRow>>>('/api/warehouses', { params: { pageNumber: 1, pageSize: 200 } }),
      api.get<ApiResponse<Paged<ProductRow>>>('/api/products', { params: { pageNumber: 1, pageSize: 500 } }),
      api.get<ApiResponse<Paged<SupplierRow>>>('/api/suppliers', { params: { pageNumber: 1, pageSize: 200 } }),
    ]);
    if (w.data.success && w.data.data) warehouses.value = w.data.data.items;
    if (p.data.success && p.data.data) products.value = p.data.data.items;
    if (s.data.success && s.data.data) suppliers.value = s.data.data.items;
  } catch {
    /* ignore lookup errors */
  }
}

async function loadSuggestions() {
  try {
    const { data } = await api.get<ApiResponse<SuggestionRow[]>>('/api/purchase-requisitions/suggestions');
    if (data.success && Array.isArray(data.data)) suggestions.value = data.data;
  } catch {
    suggestions.value = [];
  }
}

function openCreate() {
  createForm.warehouseId = warehouses.value[0]?.id ?? '';
  createForm.title = '';
  createForm.notes = '';
  createForm.lines = [];
  createDialogVisible.value = true;
}

function addLine(prefill?: Partial<{ productId: string; quantity: number; notes: string }>) {
  createForm.lines.push({
    productId: prefill?.productId ?? products.value[0]?.id ?? '',
    quantity: prefill?.quantity ?? 1,
    notes: prefill?.notes ?? '',
  });
}

function removeLine(index: number) {
  createForm.lines.splice(index, 1);
}

function loadFromSuggestions() {
  if (!suggestions.value.length) {
    toast.add({ severity: 'info', summary: '\u00d6neri yok', detail: 'Kritik stok seviyesinde \u00fcr\u00fcn bulunamad\u0131.', life: 2500 });
    return;
  }
  for (const s of suggestions.value) {
    if (createForm.lines.some(l => l.productId === s.productId)) continue;
    createForm.lines.push({ productId: s.productId, quantity: s.suggestedOrderQuantity, notes: 'Otomatik \u00f6neri' });
  }
}

async function submitCreate() {
  if (!createForm.warehouseId) {
    toast.add({ severity: 'warn', summary: 'Eksik bilgi', detail: 'Depo se\u00e7iniz.', life: 3000 });
    return;
  }
  if (!createForm.lines.length) {
    toast.add({ severity: 'warn', summary: 'Eksik bilgi', detail: 'En az bir sat\u0131r ekleyiniz.', life: 3000 });
    return;
  }
  for (const l of createForm.lines) {
    if (!l.productId || !l.quantity || l.quantity <= 0) {
      toast.add({ severity: 'warn', summary: 'Ge\u00e7ersiz sat\u0131r', detail: '\u00dcr\u00fcn ve s\u0131f\u0131rdan b\u00fcy\u00fck miktar gerekli.', life: 3000 });
      return;
    }
  }

  createSaving.value = true;
  try {
    const { data } = await api.post<ApiResponse<RequisitionRow>>('/api/purchase-requisitions', {
      warehouseId: createForm.warehouseId,
      title: createForm.title.trim() || null,
      notes: createForm.notes.trim() || null,
      lines: createForm.lines.map(l => ({
        productId: l.productId,
        quantity: l.quantity,
        notes: l.notes.trim() || null,
      })),
    });
    if (!data.success) throw new Error(data.message || 'Talep olu\u015fturulamad\u0131');
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Talep olu\u015fturuldu (Taslak).', life: 2500 });
    createDialogVisible.value = false;
    await load();
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Olu\u015fturma hatas\u0131', life: 3500 });
  } finally {
    createSaving.value = false;
  }
}

async function openDetail(row: RequisitionRow) {
  try {
    const { data } = await api.get<ApiResponse<RequisitionRow>>(`/api/purchase-requisitions/${row.id}`);
    if (!data.success || !data.data) throw new Error(data.message || 'Detay al\u0131namad\u0131');
    detailRow.value = data.data;
    quoteForm.supplierId = suppliers.value[0]?.id ?? '';
    quoteForm.totalAmount = null;
    quoteForm.currency = 'TRY';
    quoteForm.notes = '';
    detailDialogVisible.value = true;
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Detay al\u0131namad\u0131', life: 3500 });
  }
}

async function refreshDetail() {
  if (!detailRow.value) return;
  const { data } = await api.get<ApiResponse<RequisitionRow>>(`/api/purchase-requisitions/${detailRow.value.id}`);
  if (data.success && data.data) detailRow.value = data.data;
}

async function submitForApproval() {
  if (!detailRow.value) return;
  detailBusy.value = true;
  try {
    const { data } = await api.post<ApiResponse<unknown>>(`/api/purchase-requisitions/${detailRow.value.id}/submit`);
    if (!data.success) throw new Error(data.message || 'G\u00f6nderilemedi');
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Onaya g\u00f6nderildi.', life: 2200 });
    await refreshDetail();
    await load();
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'G\u00f6nderme hatas\u0131', life: 3500 });
  } finally {
    detailBusy.value = false;
  }
}

async function approve() {
  if (!detailRow.value) return;
  detailBusy.value = true;
  try {
    const { data } = await api.post<ApiResponse<unknown>>(`/api/purchase-requisitions/${detailRow.value.id}/approve`);
    if (!data.success) throw new Error(data.message || 'Onaylanamad\u0131');
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Talep onayland\u0131.', life: 2200 });
    await refreshDetail();
    await load();
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Onay hatas\u0131', life: 3500 });
  } finally {
    detailBusy.value = false;
  }
}

async function reject() {
  if (!detailRow.value) return;
  const reason = window.prompt('Red sebebi (opsiyonel):') ?? null;
  detailBusy.value = true;
  try {
    const { data } = await api.post<ApiResponse<unknown>>(`/api/purchase-requisitions/${detailRow.value.id}/reject`, { reason });
    if (!data.success) throw new Error(data.message || 'Reddedilemedi');
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Talep reddedildi.', life: 2200 });
    await refreshDetail();
    await load();
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Red hatas\u0131', life: 3500 });
  } finally {
    detailBusy.value = false;
  }
}

async function addQuote() {
  if (!detailRow.value) return;
  if (!quoteForm.supplierId) {
    toast.add({ severity: 'warn', summary: 'Eksik bilgi', detail: 'Tedarik\u00e7i se\u00e7iniz.', life: 3000 });
    return;
  }
  if (!quoteForm.totalAmount || quoteForm.totalAmount <= 0) {
    toast.add({ severity: 'warn', summary: 'Eksik bilgi', detail: 'S\u0131f\u0131rdan b\u00fcy\u00fck tutar giriniz.', life: 3000 });
    return;
  }
  quoteSaving.value = true;
  try {
    const { data } = await api.post<ApiResponse<QuoteDto>>(`/api/purchase-requisitions/${detailRow.value.id}/quotes`, {
      supplierId: quoteForm.supplierId,
      totalAmount: quoteForm.totalAmount,
      currency: quoteForm.currency || 'TRY',
      notes: quoteForm.notes.trim() || null,
    });
    if (!data.success) throw new Error(data.message || 'Teklif eklenemedi');
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Teklif eklendi.', life: 2200 });
    quoteForm.totalAmount = null;
    quoteForm.notes = '';
    await refreshDetail();
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Teklif hatas\u0131', life: 3500 });
  } finally {
    quoteSaving.value = false;
  }
}

async function acceptQuote(quote: QuoteDto) {
  if (!detailRow.value) return;
  const ok = window.confirm(`${quote.supplierName} teklifini kabul edip talebi tamamlamak istiyor musunuz?`);
  if (!ok) return;
  detailBusy.value = true;
  try {
    const { data } = await api.post<ApiResponse<unknown>>(`/api/purchase-requisitions/${detailRow.value.id}/quotes/${quote.id}/accept`);
    if (!data.success) throw new Error(data.message || 'Teklif kabul edilemedi');
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Teklif kabul edildi, talep tamamland\u0131.', life: 2500 });
    await refreshDetail();
    await load();
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Teklif kabul hatas\u0131', life: 3500 });
  } finally {
    detailBusy.value = false;
  }
}

onMounted(async () => {
  await Promise.all([load(), loadLookups(), loadSuggestions()]);
});
</script>

<template>
  <div class="space-y-5">
    <div class="surface-card flex flex-wrap items-end justify-between gap-4 p-5 md:p-6">
      <div>
        <h1 class="section-title">Sat&#305;n Alma Talepleri</h1>
        <p class="section-subtitle">Talepler, onay ak&#305;&#351;&#305; ve tedarik&ccedil;i teklifleri</p>
      </div>
      <div class="flex flex-wrap items-center gap-3">
        <span class="glass-pill">{{ total }} kay&#305;t</span>
        <Dropdown
          v-model="statusFilter"
          :options="statusOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="Duruma g&ouml;re filtrele"
          class="w-56"
          @change="load"
        />
        <Button label="Yenile" icon="pi pi-refresh" text @click="load" />
        <Button v-if="canManage" label="Yeni Talep" icon="pi pi-plus" @click="openCreate" />
      </div>
    </div>

    <div class="table-shell">
      <DataTable :value="rows" :loading="loading" responsive-layout="scroll" class="p-2" stripedRows>
        <template #empty>
          <div class="py-8 text-center text-sm text-slate-500">Kay&#305;t yok.</div>
        </template>
        <Column field="requestNumber" header="Talep No" />
        <Column header="Ba&#351;l&#305;k">
          <template #body="{ data }">{{ data.title || '&mdash;' }}</template>
        </Column>
        <Column field="warehouseName" header="Depo" />
        <Column field="requestedByName" header="Talep Eden" />
        <Column header="Sat&#305;r">
          <template #body="{ data }">{{ data.lines?.length ?? 0 }}</template>
        </Column>
        <Column header="Teklif">
          <template #body="{ data }">{{ data.quotes?.length ?? 0 }}</template>
        </Column>
        <Column header="Durum">
          <template #body="{ data }">
            <Tag :severity="statusSeverity(data.status)" :value="statusLabel(data.status)" />
          </template>
        </Column>
        <Column header="&#304;&#351;lem">
          <template #body="{ data }">
            <Button icon="pi pi-eye" label="Detay" severity="secondary" text @click="openDetail(data)" />
          </template>
        </Column>
      </DataTable>
    </div>

    <Dialog
      v-model:visible="createDialogVisible"
      modal
      header="Yeni Sat&#305;n Alma Talebi"
      class="user-create-dialog"
      :style="{ width: '64rem' }"
    >
      <div class="user-dialog-hero">
        <div class="user-dialog-icon">
          <i class="pi pi-file-edit text-lg" />
        </div>
        <div>
          <p class="user-dialog-title">Yeni talep olu&#351;tur</p>
          <p class="user-dialog-subtitle">Depo, ba&#351;l&#305;k ve sat&#305;rlar&#305; girin. Kaydetti&#287;inizde talep <strong>Taslak</strong> olu&#351;ur; detay ekran&#305;ndan onaya g&ouml;nderebilirsiniz.</p>
        </div>
      </div>

      <div class="user-dialog-section">
        <div class="grid grid-cols-1 gap-3 md:grid-cols-3">
          <div class="grid gap-2">
            <label class="user-field-label">Depo</label>
            <Dropdown v-model="createForm.warehouseId" :options="warehouses" optionLabel="name" optionValue="id" placeholder="Depo se&ccedil;in" />
          </div>
          <div class="grid gap-2 md:col-span-2">
            <label class="user-field-label">Ba&#351;l&#305;k (opsiyonel)</label>
            <InputText v-model="createForm.title" placeholder="&Ouml;r: Hafta i&ccedil;i kritik stok tamamlama" />
          </div>
        </div>
        <div class="grid gap-2">
          <label class="user-field-label">Notlar (opsiyonel)</label>
          <Textarea v-model="createForm.notes" rows="2" autoResize />
        </div>
      </div>

      <div class="user-dialog-section">
        <div class="mb-3 flex flex-wrap items-center justify-between gap-2">
          <h3 class="text-sm font-semibold text-slate-800 dark:text-slate-100">Sat&#305;rlar ({{ createForm.lines.length }})</h3>
          <div class="flex gap-2">
            <Button label="&#214;nerilerden ekle" icon="pi pi-bolt" severity="secondary" text @click="loadFromSuggestions" />
            <Button label="Sat&#305;r Ekle" icon="pi pi-plus" severity="secondary" @click="() => addLine()" />
          </div>
        </div>
        <div v-if="!createForm.lines.length" class="rounded-lg border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500 dark:border-slate-700">
          Hen&uuml;z sat&#305;r yok. &#220;st sa&#287;dan ekleyebilirsiniz.
        </div>
        <div v-for="(line, idx) in createForm.lines" :key="idx" class="mb-2 grid grid-cols-1 items-end gap-2 rounded-lg border border-slate-200 p-3 md:grid-cols-12 dark:border-slate-700">
          <div class="grid gap-1 md:col-span-5">
            <label class="user-field-label">&#220;r&uuml;n</label>
            <Dropdown v-model="line.productId" :options="products" optionLabel="name" optionValue="id" filter placeholder="&#220;r&uuml;n se&ccedil;in" />
          </div>
          <div class="grid gap-1 md:col-span-2">
            <label class="user-field-label">Miktar</label>
            <InputNumber v-model="line.quantity" :min="1" />
          </div>
          <div class="grid gap-1 md:col-span-4">
            <label class="user-field-label">Not</label>
            <InputText v-model="line.notes" placeholder="opsiyonel" />
          </div>
          <div class="md:col-span-1 md:text-right">
            <Button icon="pi pi-trash" severity="danger" text @click="removeLine(idx)" />
          </div>
        </div>
      </div>

      <template #footer>
        <div class="product-dialog-footer">
          <Button label="Vazge&ccedil;" icon="pi pi-times" class="product-btn product-btn-cancel" @click="createDialogVisible = false" />
          <Button label="Talebi Kaydet" icon="pi pi-check" class="product-btn product-btn-save" :loading="createSaving" @click="submitCreate" />
        </div>
      </template>
    </Dialog>

    <Dialog
      v-model:visible="detailDialogVisible"
      modal
      :header="detailRow ? `Talep · ${detailRow.requestNumber}` : 'Talep Detay&#305;'"
      class="user-create-dialog"
      :style="{ width: '72rem' }"
    >
      <div v-if="detailRow" class="space-y-4">
        <div class="user-dialog-hero">
          <div class="user-dialog-icon">
            <i class="pi pi-file text-lg" />
          </div>
          <div class="flex-1">
            <div class="flex flex-wrap items-center gap-3">
              <p class="user-dialog-title">{{ detailRow.title || 'Ba&#351;l&#305;ks&#305;z talep' }}</p>
              <Tag :severity="statusSeverity(detailRow.status)" :value="statusLabel(detailRow.status)" />
            </div>
            <p class="user-dialog-subtitle">
              Depo: <strong>{{ detailRow.warehouseName }}</strong> &middot; Talep Eden: <strong>{{ detailRow.requestedByName }}</strong>
              <template v-if="detailRow.approvedByName">
                &middot; Onay/Red: <strong>{{ detailRow.approvedByName }}</strong>
              </template>
            </p>
          </div>
        </div>

        <div v-if="detailRow.notes" class="surface-card p-3 text-sm text-slate-600 dark:text-slate-300">
          <strong class="block text-xs text-slate-500">Notlar</strong>
          <pre class="whitespace-pre-wrap font-sans">{{ detailRow.notes }}</pre>
        </div>

        <div class="flex flex-wrap gap-2">
          <Button
            v-if="detailRow.status === 1 && canManage"
            label="Onaya G&ouml;nder"
            icon="pi pi-send"
            severity="info"
            :loading="detailBusy"
            @click="submitForApproval"
          />
          <Button
            v-if="detailRow.status === 2 && canApprove"
            label="Onayla"
            icon="pi pi-check-circle"
            severity="success"
            :loading="detailBusy"
            @click="approve"
          />
          <Button
            v-if="detailRow.status === 2 && canApprove"
            label="Reddet"
            icon="pi pi-times-circle"
            severity="danger"
            :loading="detailBusy"
            @click="reject"
          />
        </div>

        <div class="table-shell">
          <div class="border-b border-slate-200/70 px-4 py-3 text-sm font-semibold text-slate-700 dark:border-slate-800 dark:text-slate-200">Sat&#305;rlar</div>
          <DataTable :value="detailRow.lines" responsive-layout="scroll" class="p-2" stripedRows>
            <template #empty>
              <div class="py-6 text-center text-sm text-slate-500">Sat&#305;r yok.</div>
            </template>
            <Column field="sku" header="SKU" />
            <Column field="productName" header="&#220;r&uuml;n" />
            <Column field="quantity" header="Miktar" />
            <Column header="Not">
              <template #body="{ data }">{{ data.notes || '&mdash;' }}</template>
            </Column>
          </DataTable>
        </div>

        <div class="table-shell">
          <div class="flex flex-wrap items-center justify-between border-b border-slate-200/70 px-4 py-3 text-sm font-semibold text-slate-700 dark:border-slate-800 dark:text-slate-200">
            <span>Tedarik&ccedil;i Teklifleri</span>
            <span class="text-xs text-slate-500">{{ detailRow.quotes.length }} teklif</span>
          </div>
          <DataTable :value="detailRow.quotes" responsive-layout="scroll" class="p-2" stripedRows>
            <template #empty>
              <div class="py-6 text-center text-sm text-slate-500">Hen&uuml;z teklif yok.</div>
            </template>
            <Column field="supplierName" header="Tedarik&ccedil;i" />
            <Column header="Tutar">
              <template #body="{ data }">{{ formatMoney(data.totalAmount, data.currency) }}</template>
            </Column>
            <Column header="Durum">
              <template #body="{ data }">
                <Tag :severity="quoteStatusSeverity(data.status)" :value="quoteStatusLabel(data.status)" />
              </template>
            </Column>
            <Column header="&#304;&#351;lem">
              <template #body="{ data }">
                <Button
                  v-if="detailRow && detailRow.status === 3 && data.status === 1 && canApprove"
                  label="Kabul Et"
                  icon="pi pi-check"
                  severity="success"
                  text
                  :loading="detailBusy"
                  @click="acceptQuote(data)"
                />
              </template>
            </Column>
          </DataTable>

          <div v-if="detailRow.status === 3 && canManage" class="border-t border-slate-200/70 p-4 dark:border-slate-800">
            <h4 class="mb-2 text-sm font-semibold text-slate-800 dark:text-slate-100">Yeni Teklif Ekle</h4>
            <div class="grid grid-cols-1 gap-3 md:grid-cols-12">
              <div class="grid gap-1 md:col-span-4">
                <label class="user-field-label">Tedarik&ccedil;i</label>
                <Dropdown v-model="quoteForm.supplierId" :options="suppliers" optionLabel="name" optionValue="id" filter placeholder="Tedarik&ccedil;i se&ccedil;in" />
              </div>
              <div class="grid gap-1 md:col-span-3">
                <label class="user-field-label">Tutar</label>
                <InputNumber v-model="quoteForm.totalAmount" :min="0" mode="decimal" :minFractionDigits="2" />
              </div>
              <div class="grid gap-1 md:col-span-2">
                <label class="user-field-label">Para Birimi</label>
                <InputText v-model="quoteForm.currency" maxlength="8" />
              </div>
              <div class="grid gap-1 md:col-span-3">
                <label class="user-field-label">Not</label>
                <InputText v-model="quoteForm.notes" placeholder="opsiyonel" />
              </div>
              <div class="md:col-span-12 md:text-right">
                <Button label="Teklif Ekle" icon="pi pi-plus" :loading="quoteSaving" @click="addQuote" />
              </div>
            </div>
          </div>
        </div>
      </div>

      <template #footer>
        <div class="product-dialog-footer">
          <Button label="Kapat" icon="pi pi-times" class="product-btn product-btn-cancel" @click="detailDialogVisible = false" />
        </div>
      </template>
    </Dialog>
  </div>
</template>
