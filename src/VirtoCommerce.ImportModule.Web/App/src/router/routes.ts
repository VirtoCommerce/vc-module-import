import { RouteRecordRaw } from "vue-router";
import App from "../pages/App.vue";

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
    redirect: (to) => {
      if (to.name === "App") {
        return { path: to.params.sellerId ? to.path + "/import" : "/import", params: to.params };
      }
      return to.path;
    },
  },
];
