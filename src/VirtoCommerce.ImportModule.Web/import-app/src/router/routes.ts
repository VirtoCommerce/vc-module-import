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
    redirect: (to) => {
      console.log("to", to);
      if (to.name === "App") {
        return { path: to.params.userId ? to.path + "/import" : "/import", params: to.params };
      }
      return to.path;
    },
  },
  {
    path: "/:userId?/:pathMatch(.*)*",
    component: App,
    beforeEnter: async (to) => {
      const { routeResolver } = useBladeNavigation();
      return routeResolver(to);
    },
  },
];
