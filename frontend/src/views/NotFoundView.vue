<script setup lang="ts">
import Button from 'primevue/button';
import { useRouter } from 'vue-router';

const router = useRouter();
</script>

<template>
  <div class="grid min-h-[70vh] place-items-center p-8">
    <div class="max-w-lg text-center space-y-3">
      <div class="text-5xl font-bold text-brand-700 dark:text-brand-300">404</div>
      <div class="text-lg font-semibold">Sayfa bulunamad&#305;</div>
      <Button label="Ana sayfaya d&#246;n" icon="pi pi-home" @click="router.push('/')" />
    </div>
  </div>
</template>
