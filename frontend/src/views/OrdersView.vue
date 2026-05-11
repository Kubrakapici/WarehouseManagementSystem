<script setup lang="ts">
import { onMounted, ref } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Tag from 'primevue/tag';
import Button from 'primevue/button';
import { useToast } from 'primevue/usetoast';
import { api, type ApiResponse } from '@/services/api';

type Row = {
  id: string;
  orderNumber: string;
  statusName: string;
  supplierName?: string | null;
};

type Paged<T> = { items: T[]; totalCount: number; pageNumber: number; pageSize: number };

const toast = useToast();
const loading = ref(false);
const rows = ref<Row[]>([]);
const total = ref(0);

function statusSeverity(name: string) {
  const n = name.toLowerCase();
  if (n.includes('cancel')) return 'danger';
  if (n.includes('complete') || n.includes('shipped')) return 'success';
  if (n.includes('pending') || n.includes('draft')) return 'warning';
  return 'info';
}

async function load() {
  loading.value = true;
  try {
    const { data } = await api.get<ApiResponse<Paged<Row>>>('/api/orders', {
      params: { pageNumber: 1, pageSize: 50 },
    });
    if (!data.success || !data.data) throw new Error(data.message || 'Liste alınamadı');
    rows.value = data.data.items;
    total.value = data.data.totalCount;
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Yükleme hatası', life: 3500 });
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-5">
    <div class="surface-card flex flex-wrap items-end justify-between gap-4 p-5 md:p-6">
      <div>
        <h1 class="section-title">Sipari&#351;ler</h1>
        <p class="section-subtitle">Sipari&#351; listesi ve durumlar</p>
      </div>
      <div class="flex gap-2">
        <span class="glass-pill">{{ total }} kay&#305;t</span>
        <Button label="Yenile" icon="pi pi-refresh" text @click="load" />
      </div>
    </div>

    <div class="table-shell">
      <DataTable :value="rows" :loading="loading" responsive-layout="scroll" class="p-2" stripedRows>
        <template #empty>
          <div class="py-8 text-center text-sm text-slate-500">Kay&#305;t yok.</div>
        </template>
        <Column field="orderNumber" header="Sipari&#351; No" />
        <Column header="Durum">
          <template #body="{ data }">
            <Tag :severity="statusSeverity(data.statusName)" :value="data.statusName" />
          </template>
        </Column>
        <Column field="supplierName" header="Tedarik&#231;i" />
      </DataTable>
    </div>
  </div>
</template>
