import { RouteRecordRaw } from "vue-router";
import App from "../pages/App.vue";
import { useBladeNavigation } from "@vc-shell/framework";

export const routes: RouteRecordRaw[] = [
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
    beforeEnter: async (to) => {
      const { routeResolver } = useBladeNavigation();
      return routeResolver(to);
    },
  },
];
