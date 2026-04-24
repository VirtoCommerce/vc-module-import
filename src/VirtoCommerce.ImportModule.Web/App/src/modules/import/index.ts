import * as pages from "./pages";
import * as locales from "./locales";
import ImportPushNotification from "./components/notifications/ImportPushNotification.vue";
import { defineAppModule } from "@vc-shell/framework";

export default defineAppModule({
  blades: pages,
  locales,
  notifications: {
    ImportPushNotification: {
      template: ImportPushNotification,
      toast: {
        mode: "progress",
      },
    },
  },
});

export * from "./pages";
export * from "./components";
export * from "./composables";
