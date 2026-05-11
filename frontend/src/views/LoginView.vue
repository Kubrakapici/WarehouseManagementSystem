<script setup lang="ts">
import { ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import InputText from 'primevue/inputtext';
import Password from 'primevue/password';
import Button from 'primevue/button';
import { useToast } from 'primevue/usetoast';
import { useAuthStore } from '@/stores/auth';

const email = ref('izleyici@wms.local');
const password = ref('Viewer@123');
const loading = ref(false);

const auth = useAuthStore();
const router = useRouter();
const route = useRoute();
const toast = useToast();

async function submit() {
  loading.value = true;
  try {
    await auth.login(email.value, password.value);
    const redirect = (route.query.redirect as string) || '/';
    await router.replace(redirect);
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : 'Giri\u015f ba\u015far\u0131s\u0131z';
    toast.add({ severity: 'error', summary: 'Hata', detail: msg, life: 3500 });
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="relative grid min-h-full place-items-center overflow-hidden bg-slate-950 p-6">
    <div class="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_18%_18%,rgba(104,137,197,0.45),transparent_35%),radial-gradient(circle_at_80%_20%,rgba(14,165,233,0.28),transparent_35%),radial-gradient(circle_at_45%_85%,rgba(59,130,246,0.22),transparent_45%)]" />

    <div class="relative z-10 grid w-full max-w-5xl overflow-hidden rounded-3xl border border-white/20 bg-white/90 shadow-2xl backdrop-blur dark:bg-slate-900/80 lg:grid-cols-2">
      <div class="hidden bg-gradient-to-br from-brand-700 via-brand-600 to-cyan-600 p-10 text-white lg:block">
        <div class="text-xs font-semibold uppercase tracking-[0.2em] text-cyan-100">Warehouse Management Suite</div>
        <h1 class="mt-8 text-4xl font-semibold leading-tight">Kurumsal Depo Operasyonlar&#305;n&#305;
          <span class="text-cyan-100">tek panelden y&ouml;net</span>
        </h1>
        <p class="mt-5 max-w-md text-sm text-cyan-50/90">Stok hareketleri, kritik seviyeler, kullan&#305;c&#305; yetkileri ve raporlamay&#305; modern bir ERP deneyimiyle kontrol edin.</p>
        <div class="mt-8 grid gap-3 text-sm">
          <div class="rounded-xl border border-white/25 bg-white/10 px-4 py-3">Ger&ccedil;ek zamanl&#305; stok takibi</div>
          <div class="rounded-xl border border-white/25 bg-white/10 px-4 py-3">Rol bazl&#305; g&uuml;venli eri&#351;im</div>
          <div class="rounded-xl border border-white/25 bg-white/10 px-4 py-3">Kurumsal raporlama ve d&#305;&#351;a aktar&#305;m</div>
        </div>
      </div>

      <div class="p-7 sm:p-10">
        <div class="mb-8">
          <div class="inline-flex rounded-full border border-slate-200 bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">G&uuml;venli Oturum A&ccedil;ma</div>
          <h2 class="mt-4 text-2xl font-semibold text-slate-900 dark:text-slate-100">WMS Admin Panel</h2>
          <p class="mt-2 text-sm text-slate-500">Devam etmek i&ccedil;in hesap bilgilerinizi girin.</p>
        </div>

        <form class="space-y-4" @submit.prevent="submit">
          <div class="space-y-2">
            <label class="text-sm font-medium text-slate-700 dark:text-slate-300" for="email">E-posta</label>
            <InputText id="email" v-model="email" autocomplete="username" class="w-full" />
          </div>

          <div class="space-y-2">
            <label class="text-sm font-medium text-slate-700 dark:text-slate-300" for="password">&#350;ifre</label>
            <Password id="password" v-model="password" toggle-mask :feedback="false" input-class="w-full" class="w-full" autocomplete="current-password" />
          </div>

          <Button type="submit" label="Panele Giri&#351; Yap" icon="pi pi-arrow-right" icon-pos="right" class="w-full" :loading="loading" />
        </form>

        <div class="mt-6 text-xs text-slate-500">
          Demo (salt okunur): <span class="font-semibold">izleyici@wms.local / Viewer@123</span>
        </div>
      </div>
    </div>
  </div>
</template>
