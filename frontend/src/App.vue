<script setup lang="ts">
import { RouterView } from 'vue-router';
import Toast from 'primevue/toast';
import { useThemeStore } from '@/stores/theme';

const theme = useThemeStore();
theme.apply();
</script>

<template>
  <RouterView />
  <Toast position="top-right" />
</template>
