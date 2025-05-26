import { RouteRecordRaw } from "vue-router";
import App from "../pages/App.vue";
import { BladeVNode, useBladeNavigation } from "@vc-shell/framework";

const sellerIdRegex = "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";

export const routes: RouteRecordRaw[] = [
  {
    path: `/:sellerId(${sellerIdRegex})?`,
    component: App,
    name: "App",
    meta: {
      root: true,
    },
    children: [],
    beforeEnter: (to) => {
      const { sellerId } = to.params;

      if (!sellerId || new RegExp(sellerIdRegex).test(sellerId as string)) {
        return true;
      } else {
        return { path: (to.matched[1].components?.default as BladeVNode).type.url as string };
      }
    },
    redirect: (to) => {
      if (to.name === "App") {
        return { path: to.params.sellerId ? to.path + "/import" : "/import", params: to.params };
      }
      return to.path;
    },
  },
  {
    path: `/:sellerId(${sellerIdRegex})?/:pathMatch(.*)*`,
    component: App,
    beforeEnter: async (to) => {
      const { routeResolver } = useBladeNavigation();
      return routeResolver(to);
    },
  },
];
