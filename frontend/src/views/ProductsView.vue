<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Tag from 'primevue/tag';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Dropdown from 'primevue/dropdown';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import { useToast } from 'primevue/usetoast';
import { api, type ApiResponse } from '@/services/api';
import { useAuthStore } from '@/stores/auth';

const auth = useAuthStore();
const canWriteProducts = computed(() => auth.hasRole(['Admin', 'Manager', 'WarehouseStaff']));
const canDeleteProducts = computed(() => auth.hasRole(['Admin', 'Manager']));

type Product = {
  id: string;
  sku: string;
  name: string;
  categoryName: string;
  totalQuantity: number;
  minimumStockLevel: number;
  isActive: boolean;
};

type Category = {
  id: string;
  name: string;
  parentCategoryId?: string | null;
};

type Paged<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
};

const toast = useToast();
const loading = ref(false);
const saving = ref(false);
const creatingCategory = ref(false);
const deletingId = ref<string | null>(null);
const createDialogVisible = ref(false);
const newProductButtonLabel = 'Yeni \u00dcr\u00fcn';
const rows = ref<Product[]>([]);
const categories = ref<Category[]>([]);
const total = ref(0);
const query = ref('');
const newCategoryName = ref('');

const createForm = reactive({
  name: '',
  sku: '',
  barcode: '',
  categoryId: '',
  unitPrice: 0,
  minimumStockLevel: 0,
  isActive: true,
  imageUrl: '',
});

const filteredRows = computed(() => {
  const term = query.value.trim().toLowerCase();
  if (!term) return rows.value;
  return rows.value.filter((p) =>
    p.name.toLowerCase().includes(term) ||
    p.sku.toLowerCase().includes(term) ||
    p.categoryName.toLowerCase().includes(term),
  );
});

const categoryOptions = computed(() =>
  categories.value.map((c) => ({
    label: c.name,
    value: c.id,
  })),
);

function resetCreateForm() {
  createForm.name = '';
  createForm.sku = '';
  createForm.barcode = '';
  createForm.categoryId = '';
  createForm.unitPrice = 0;
  createForm.minimumStockLevel = 0;
  createForm.isActive = true;
  createForm.imageUrl = '';
}

async function loadProducts() {
  const { data } = await api.get<ApiResponse<Paged<Product>>>('/api/products', {
    params: { pageNumber: 1, pageSize: 100 },
  });
  if (!data.success || !data.data) throw new Error(data.message || 'Liste al\u0131namad\u0131');
  rows.value = data.data.items;
  total.value = data.data.totalCount;
}

async function loadCategories() {
  const { data } = await api.get<ApiResponse<Category[]>>('/api/categories');
  if (!data.success || !data.data) throw new Error(data.message || 'Kategoriler al\u0131namad\u0131');
  categories.value = data.data;
}

async function loadAll() {
  loading.value = true;
  try {
    await Promise.all([loadProducts(), loadCategories()]);
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Y\u00fckleme hatas\u0131', life: 3500 });
  } finally {
    loading.value = false;
  }
}

async function createProduct() {
  if (!createForm.name.trim() || !createForm.sku.trim() || !createForm.categoryId) {
    toast.add({ severity: 'warn', summary: 'Eksik bilgi', detail: '\u00dcr\u00fcn ad\u0131, SKU ve kategori zorunludur.', life: 3000 });
    return;
  }

  saving.value = true;
  try {
    const { data } = await api.post<ApiResponse<Product>>('/api/products', {
      name: createForm.name.trim(),
      sku: createForm.sku.trim(),
      barcode: createForm.barcode.trim() || null,
      imageUrl: createForm.imageUrl.trim() || null,
      unitPrice: createForm.unitPrice ?? 0,
      minimumStockLevel: createForm.minimumStockLevel ?? 0,
      isActive: createForm.isActive,
      categoryId: createForm.categoryId,
      generateQr: true,
    });

    if (!data.success) throw new Error(data.message || '\u00dcr\u00fcn eklenemedi');

    createDialogVisible.value = false;
    resetCreateForm();
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: '\u00dcr\u00fcn eklendi.', life: 2500 });
    await loadProducts();
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : '\u00dcr\u00fcn ekleme ba\u015far\u0131s\u0131z', life: 3500 });
  } finally {
    saving.value = false;
  }
}

async function createCategoryQuick() {
  const name = newCategoryName.value.trim();
  if (!name) {
    toast.add({ severity: 'warn', summary: 'Eksik bilgi', detail: 'Kategori ad\u0131 zorunludur.', life: 2800 });
    return;
  }

  creatingCategory.value = true;
  try {
    const { data } = await api.post<ApiResponse<Category>>('/api/categories', {
      name,
      description: null,
      parentCategoryId: null,
    });

    if (!data.success || !data.data) throw new Error(data.message || 'Kategori eklenemedi');

    categories.value = [data.data, ...categories.value];
    createForm.categoryId = data.data.id;
    newCategoryName.value = '';
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Kategori eklendi ve se\u00e7ildi.', life: 2500 });
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Kategori ekleme ba\u015far\u0131s\u0131z', life: 3500 });
  } finally {
    creatingCategory.value = false;
  }
}

async function deleteProduct(item: Product) {
  const ok = window.confirm(`${item.name} \u00fcr\u00fcn\u00fc silmek istiyor musunuz?`);
  if (!ok) return;

  deletingId.value = item.id;
  try {
    const { data } = await api.delete<ApiResponse<unknown>>(`/api/products/${item.id}`);
    if (!data.success) throw new Error(data.message || '\u00dcr\u00fcn silinemedi');

    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: '\u00dcr\u00fcn silindi.', life: 2500 });
    await loadProducts();
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : '\u00dcr\u00fcn silme ba\u015far\u0131s\u0131z', life: 3500 });
  } finally {
    deletingId.value = null;
  }
}

onMounted(loadAll);
</script>

<template>
  <div class="space-y-5">
    <div class="surface-card flex flex-wrap items-end justify-between gap-4 p-5 md:p-6">
      <div>
        <h1 class="section-title">&Uuml;r&uuml;n Y&ouml;netimi</h1>
        <p class="section-subtitle">Yeni &uuml;r&uuml;n ekleyin, var olan &uuml;r&uuml;nleri y&ouml;netin ve stok durumunu izleyin</p>
      </div>
      <div class="flex items-center gap-3">
        <span class="glass-pill">Toplam {{ total }} &uuml;r&uuml;n</span>
        <span class="glass-pill">G&ouml;sterilen {{ filteredRows.length }}</span>
        <Button v-if="canWriteProducts" :label="newProductButtonLabel" icon="pi pi-plus" @click="createDialogVisible = true" />
      </div>
    </div>

    <div class="surface-card p-4">
      <span class="p-input-icon-left block max-w-sm">
        <i class="pi pi-search" />
        <InputText v-model="query" class="w-full" placeholder="&Uuml;r&uuml;n, SKU veya kategori ara" />
      </span>
    </div>

    <div class="table-shell">
      <DataTable :value="filteredRows" :loading="loading" responsive-layout="scroll" class="p-2" stripedRows>
        <template #empty>
          <div class="py-8 text-center text-sm text-slate-500">Aramaya uygun &uuml;r&uuml;n bulunamad&#305;.</div>
        </template>
        <Column field="sku" header="SKU" />
        <Column field="name" header="&Uuml;r&uuml;n" />
        <Column field="categoryName" header="Kategori" />
        <Column header="Stok">
          <template #body="{ data }">
            <Tag :severity="data.totalQuantity <= data.minimumStockLevel ? 'warning' : 'success'" :value="`${data.totalQuantity} / min ${data.minimumStockLevel}`" />
          </template>
        </Column>
        <Column header="Durum">
          <template #body="{ data }">
            <Tag :severity="data.isActive ? 'success' : 'danger'" :value="data.isActive ? 'Aktif' : 'Pasif'" />
          </template>
        </Column>
        <Column v-if="canDeleteProducts" header="&#304;&#351;lem">
          <template #body="{ data }">
            <Button
              icon="pi pi-trash"
              label="Sil"
              severity="danger"
              text
              :loading="deletingId === data.id"
              @click="deleteProduct(data)"
            />
          </template>
        </Column>
      </DataTable>
    </div>

    <Dialog v-model:visible="createDialogVisible" modal header="Yeni &Uuml;r&uuml;n Ekle" class="product-create-dialog" :style="{ width: '42rem' }">
      <div class="product-dialog-hero">
        <div class="product-dialog-icon">
          <i class="pi pi-box text-lg" />
        </div>
        <div>
          <p class="product-dialog-title">Yeni &uuml;r&uuml;n kart&#305; olu&#351;tur</p>
          <p class="product-dialog-subtitle">Temel bilgileri girin, stok ve fiyat takibi i&ccedil;in &uuml;r&uuml;n&uuml; sisteme ekleyin.</p>
        </div>
      </div>

      <div class="product-dialog-section">
        <div class="grid gap-2">
          <label class="product-field-label">&Uuml;r&uuml;n Ad&#305;</label>
          <InputText v-model="createForm.name" placeholder="&Ouml;r: End&uuml;striyel Matkap 900W" />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div class="grid gap-2">
            <label class="product-field-label">SKU</label>
            <InputText v-model="createForm.sku" placeholder="&Ouml;r: URN-000245" />
          </div>
          <div class="grid gap-2">
            <label class="product-field-label">Barkod</label>
            <InputText v-model="createForm.barcode" placeholder="Opsiyonel" />
          </div>
        </div>
      </div>

      <div class="product-dialog-section">
        <div class="grid gap-2">
          <label class="product-field-label">Kategori</label>
          <Dropdown v-model="createForm.categoryId" :options="categoryOptions" optionLabel="label" optionValue="value" placeholder="Kategori se&ccedil;in" />
        </div>
        <div class="quick-category-row">
          <InputText v-model="newCategoryName" class="flex-1" placeholder="Yeni kategori ad&#305;" @keydown.enter.prevent="createCategoryQuick" />
          <Button
            label="Kategori Ekle"
            icon="pi pi-plus"
            class="product-btn product-btn-add-category"
            :loading="creatingCategory"
            @click="createCategoryQuick"
          />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div class="grid gap-2">
            <label class="product-field-label">Birim Fiyat</label>
            <InputNumber v-model="createForm.unitPrice" mode="decimal" :min="0" />
          </div>
          <div class="grid gap-2">
            <label class="product-field-label">Minimum Stok</label>
            <InputNumber v-model="createForm.minimumStockLevel" :min="0" />
          </div>
        </div>
        <div class="grid gap-2">
          <label class="product-field-label">G&ouml;rsel URL (opsiyonel)</label>
          <InputText v-model="createForm.imageUrl" placeholder="https://..." />
        </div>
      </div>

      <template #footer>
        <div class="product-dialog-footer">
          <Button
            label="Vazge&ccedil;"
            icon="pi pi-times"
            class="product-btn product-btn-cancel"
            @click="createDialogVisible = false"
          />
          <Button
            label="&Uuml;r&uuml;n&uuml; Kaydet"
            icon="pi pi-check"
            class="product-btn product-btn-save"
            :loading="saving"
            @click="createProduct"
          />
        </div>
      </template>
    </Dialog>
  </div>
</template>
