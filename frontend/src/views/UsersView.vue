<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Tag from 'primevue/tag';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Password from 'primevue/password';
import Dropdown from 'primevue/dropdown';
import { useToast } from 'primevue/usetoast';
import { api, type ApiResponse } from '@/services/api';

type UserRow = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roleId: string;
  roleName: string;
  isActive: boolean;
};

type Paged<T> = {
  items: T[];
  totalCount: number;
};

type Role = {
  id: string;
  name: string;
};

const toast = useToast();
const loading = ref(false);
const saving = ref(false);
const updatingStatusId = ref<string | null>(null);
const deletingId = ref<string | null>(null);
const createDialogVisible = ref(false);
const rows = ref<UserRow[]>([]);
const total = ref(0);
const roles = ref<Role[]>([]);

const createForm = reactive({
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  roleId: '',
  isActive: true,
});

const activeOptions = [
  { label: 'Aktif', value: true },
  { label: 'Pasif', value: false },
];

function resetCreateForm() {
  createForm.email = '';
  createForm.password = '';
  createForm.firstName = '';
  createForm.lastName = '';
  createForm.roleId = roles.value[0]?.id ?? '';
  createForm.isActive = true;
}

async function loadUsers() {
  const { data } = await api.get<ApiResponse<Paged<UserRow>>>('/api/users', { params: { pageNumber: 1, pageSize: 50 } });
  if (!data.success || !data.data) throw new Error(data.message || 'Liste al\u0131namad\u0131');
  rows.value = data.data.items;
  total.value = data.data.totalCount;
}

async function loadRoles() {
  const { data } = await api.get<ApiResponse<Role[]>>('/api/roles');
  if (!data.success || !data.data) throw new Error(data.message || 'Roller al\u0131namad\u0131');
  roles.value = data.data;
}

async function loadAll() {
  loading.value = true;
  try {
    await Promise.all([loadUsers(), loadRoles()]);
    if (!createForm.roleId && roles.value.length) {
      createForm.roleId = roles.value[0].id;
    }
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Hata', life: 3500 });
  } finally {
    loading.value = false;
  }
}

async function createUser() {
  if (!createForm.firstName.trim() || !createForm.lastName.trim() || !createForm.email.trim() || !createForm.roleId || !createForm.password.trim()) {
    toast.add({ severity: 'warn', summary: 'Eksik bilgi', detail: 'Ad, soyad, e-posta, rol ve parola zorunludur.', life: 3200 });
    return;
  }

  if (createForm.password.trim().length < 8) {
    toast.add({ severity: 'warn', summary: 'Ge\u00e7ersiz parola', detail: 'Parola en az 8 karakter olmal\u0131d\u0131r.', life: 3200 });
    return;
  }

  saving.value = true;
  try {
    const { data } = await api.post<ApiResponse<UserRow>>('/api/users', {
      email: createForm.email.trim(),
      password: createForm.password.trim(),
      firstName: createForm.firstName.trim(),
      lastName: createForm.lastName.trim(),
      roleId: createForm.roleId,
      isActive: createForm.isActive,
    });

    if (!data.success) throw new Error(data.message || 'Kullan\u0131c\u0131 eklenemedi');

    createDialogVisible.value = false;
    resetCreateForm();
    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Kullan\u0131c\u0131 eklendi.', life: 2500 });
    await loadUsers();
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Kullan\u0131c\u0131 ekleme ba\u015far\u0131s\u0131z', life: 3500 });
  } finally {
    saving.value = false;
  }
}

async function deleteUser(user: UserRow) {
  const ok = window.confirm(`${user.firstName} ${user.lastName} kullan\u0131c\u0131s\u0131n\u0131 silmek istiyor musunuz?`);
  if (!ok) return;

  deletingId.value = user.id;
  try {
    const { data } = await api.delete<ApiResponse<unknown>>(`/api/users/${user.id}`);
    if (!data.success) throw new Error(data.message || 'Kullan\u0131c\u0131 silinemedi');

    toast.add({ severity: 'success', summary: 'Ba\u015far\u0131l\u0131', detail: 'Kullan\u0131c\u0131 silindi.', life: 2500 });
    await loadUsers();
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Kullan\u0131c\u0131 silme ba\u015far\u0131s\u0131z', life: 3500 });
  } finally {
    deletingId.value = null;
  }
}

async function toggleUserStatus(user: UserRow) {
  updatingStatusId.value = user.id;
  const nextStatus = !user.isActive;
  try {
    const { data } = await api.put<ApiResponse<UserRow>>(`/api/users/${user.id}`, {
      firstName: user.firstName,
      lastName: user.lastName,
      roleId: user.roleId,
      isActive: nextStatus,
      newPassword: null,
    });

    if (!data.success) throw new Error(data.message || 'Durum g\u00fcncellenemedi');

    user.isActive = nextStatus;
    toast.add({
      severity: 'success',
      summary: 'Ba\u015far\u0131l\u0131',
      detail: nextStatus ? 'Kullan\u0131c\u0131 aktif yap\u0131ld\u0131.' : 'Kullan\u0131c\u0131 pasif yap\u0131ld\u0131.',
      life: 2500,
    });
  } catch (e: unknown) {
    toast.add({ severity: 'error', summary: 'Hata', detail: e instanceof Error ? e.message : 'Durum g\u00fcncelleme ba\u015far\u0131s\u0131z', life: 3500 });
  } finally {
    updatingStatusId.value = null;
  }
}

onMounted(loadAll);
</script>

<template>
  <div class="space-y-5">
    <div class="surface-card flex flex-wrap items-end justify-between gap-4 p-5 md:p-6">
      <div>
        <h1 class="section-title">Kullan&#305;c&#305; ve Yetki Y&ouml;netimi</h1>
        <p class="section-subtitle">Rol da&#287;&#305;l&#305;m&#305; ve kullan&#305;c&#305; aktivasyon durumlar&#305;</p>
      </div>
      <div class="flex items-center gap-3">
        <span class="glass-pill">{{ total }} kullan&#305;c&#305;</span>
        <Button label="Yeni Kullan&#305;c&#305;" icon="pi pi-plus" @click="createDialogVisible = true" />
      </div>
    </div>

    <div class="table-shell">
      <DataTable :value="rows" :loading="loading" responsive-layout="scroll" class="p-2" stripedRows>
        <template #empty>
          <div class="py-8 text-center text-sm text-slate-500">Kullan&#305;c&#305; kayd&#305; bulunamad&#305;.</div>
        </template>
        <Column field="email" header="E-posta" />
        <Column header="Ad Soyad">
          <template #body="{ data }">{{ data.firstName }} {{ data.lastName }}</template>
        </Column>
        <Column field="roleName" header="Rol" />
        <Column header="Durum">
          <template #body="{ data }">
            <div class="flex items-center gap-2">
              <Tag :severity="data.isActive ? 'success' : 'danger'" :value="data.isActive ? 'Aktif' : 'Pasif'" />
              <Button
                :label="data.isActive ? 'Pasife Al' : 'Aktif Et'"
                :icon="data.isActive ? 'pi pi-eye-slash' : 'pi pi-check-circle'"
                class="p-button-sm"
                severity="secondary"
                text
                :loading="updatingStatusId === data.id"
                @click="toggleUserStatus(data)"
              />
            </div>
          </template>
        </Column>
        <Column header="&#304;&#351;lem">
          <template #body="{ data }">
            <Button
              icon="pi pi-trash"
              label="Sil"
              severity="danger"
              text
              :loading="deletingId === data.id"
              @click="deleteUser(data)"
            />
          </template>
        </Column>
      </DataTable>
    </div>

    <Dialog v-model:visible="createDialogVisible" modal header="Yeni Kullan&#305;c&#305; Ekle" class="user-create-dialog" :style="{ width: '42rem' }">
      <div class="user-dialog-hero">
        <div class="user-dialog-icon">
          <i class="pi pi-user-plus text-lg" />
        </div>
        <div>
          <p class="user-dialog-title">Yeni kullan&#305;c&#305; hesab&#305; olu&#351;tur</p>
          <p class="user-dialog-subtitle">Ekip i&ccedil;in temel hesap bilgilerini girin, rol ve durum atamas&#305;n&#305; tamamlay&#305;n.</p>
        </div>
      </div>

      <div class="user-dialog-section">
        <div class="grid grid-cols-2 gap-3">
          <div class="grid gap-2">
            <label class="user-field-label">Ad</label>
            <InputText v-model="createForm.firstName" placeholder="&Ouml;r: Ahmet" />
          </div>
          <div class="grid gap-2">
            <label class="user-field-label">Soyad</label>
            <InputText v-model="createForm.lastName" placeholder="&Ouml;r: Y&#305;lmaz" />
          </div>
        </div>
        <div class="grid gap-2">
          <label class="user-field-label">E-posta</label>
          <InputText v-model="createForm.email" placeholder="ornek@firma.com" />
        </div>
      </div>

      <div class="user-dialog-section">
        <div class="grid gap-2">
          <label class="user-field-label">Parola</label>
          <Password v-model="createForm.password" :feedback="false" toggleMask />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div class="grid gap-2">
            <label class="user-field-label">Rol</label>
            <Dropdown v-model="createForm.roleId" :options="roles" optionLabel="name" optionValue="id" placeholder="Rol se&ccedil;in" />
          </div>
          <div class="grid gap-2">
            <label class="user-field-label">Durum</label>
            <Dropdown v-model="createForm.isActive" :options="activeOptions" optionLabel="label" optionValue="value" />
          </div>
        </div>
      </div>

      <template #footer>
        <div class="product-dialog-footer">
          <Button label="Vazge&ccedil;" icon="pi pi-times" class="product-btn product-btn-cancel" @click="createDialogVisible = false" />
          <Button label="Kullan&#305;c&#305;y&#305; Kaydet" icon="pi pi-check" class="product-btn product-btn-save" :loading="saving" @click="createUser" />
        </div>
      </template>
    </Dialog>
  </div>
</template>
