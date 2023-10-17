import { createRouter, createWebHashHistory, RouteRecordRaw } from "vue-router";
import { usePermissions, useUser, BladePageComponent, notification, useBladeNavigation } from "@vc-shell/framework";
import { inject } from "vue";
/**
 * Pages
 */
import App from "./../pages/App.vue";

const routes: RouteRecordRaw[] = [
  {
    path: "/:userId?",
    component: App,
    name: "App",
    meta: {
      root: true,
    },
    children: [],
  },
  {
    path: "/",
    redirect: "/import",
  },
  {
    path: "/:pathMatch(.*)*",
    component: App,
    beforeEnter: (to) => {
      const { resolveUnknownRoutes } = useBladeNavigation();

      return resolveUnknownRoutes(to);
    },
  },
];

export const router = createRouter({
  history: createWebHashHistory(import.meta.env.APP_BASE_PATH as string),
  routes,
});

router.beforeEach(async (to, from) => {
  const { fetchUserPermissions, checkPermission } = usePermissions();
  const { resolveBlades } = useBladeNavigation();
  const { isAuthenticated } = useUser();
  const pages = inject<BladePageComponent[]>("pages");

  if (to.name !== "Login" && to.name !== "ResetPassword" && to.name !== "Invite") {
    try {
      // Fetch permissions if not any
      await fetchUserPermissions();

      const component = pages.find((blade) => blade?.url === to.path);

      const hasAccess = checkPermission(component?.permissions);

      if (!(await isAuthenticated()) && to.name !== "Login") {
        return { name: "Login" };
      } else if (hasAccess && to.name !== "Login") {
        const resolvedBladeUrl = resolveBlades(to);
        return resolvedBladeUrl ? resolvedBladeUrl : true;
      } else if (!hasAccess) {
        notification.error("Access restricted", {
          timeout: 3000,
        });
        return from.path;
      }
    } catch (e) {
      return { name: "Login" };
    }
  } else return true;
});
