import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', component: () => import('@/views/LoginView.vue'), meta: { public: true } },
    {
      path: '/',
      component: () => import('@/layouts/MainLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        { path: '', name: 'home', component: () => import('@/views/DashboardView.vue'), meta: { roles: ['Admin', 'WarehouseStaff', 'Operations', 'Manager', 'Viewer'] } },
        { path: 'dashboard', redirect: '/' },
        { path: 'products', component: () => import('@/views/ProductsView.vue'), meta: { roles: ['Admin', 'WarehouseStaff', 'Operations', 'Manager', 'Viewer'] } },
        { path: 'stock', component: () => import('@/views/StockView.vue'), meta: { roles: ['Admin', 'WarehouseStaff', 'Operations', 'Manager', 'Viewer'] } },
        { path: 'warehouses', component: () => import('@/views/WarehousesView.vue'), meta: { roles: ['Admin', 'Manager', 'WarehouseStaff', 'Operations', 'Viewer'] } },
        { path: 'purchase-requisitions', component: () => import('@/views/PurchaseRequisitionsView.vue'), meta: { roles: ['Admin', 'Manager', 'Operations', 'Viewer'] } },
        { path: 'customers', component: () => import('@/views/CustomersView.vue'), meta: { roles: ['Admin', 'Manager', 'Operations', 'Viewer'] } },
        { path: 'orders', component: () => import('@/views/OrdersView.vue'), meta: { roles: ['Admin', 'WarehouseStaff', 'Operations', 'Manager', 'Viewer'] } },
        { path: 'users', component: () => import('@/views/UsersView.vue'), meta: { roles: ['Admin'] } },
      ],
    },
    { path: '/:pathMatch(.*)*', component: () => import('@/views/NotFoundView.vue') },
  ],
});

router.beforeEach(async (to) => {
  const auth = useAuthStore();
  if (to.meta.public) return true;
  if (to.meta.requiresAuth && !auth.isAuthenticated) return { path: '/login', query: { redirect: to.fullPath } };

  const roles = (to.meta.roles as string[] | undefined) ?? [];
  if (roles.length && !auth.hasRole(roles)) return { path: '/' };

  return true;
});

export default router;
