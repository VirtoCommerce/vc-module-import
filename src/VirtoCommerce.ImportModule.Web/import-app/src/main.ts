import VirtoShellFramework, { notification, useUser } from "@vc-shell/framework";
import * as modules from "@virtocommerce/import-app";
import { createApp } from "vue";
import { router } from "./router";
import * as locales from "./locales";
import { RouterView } from "vue-router";
import { bootstrap } from "./bootstrap";

// Load required CSS
import "@fortawesome/fontawesome-free/css/all.min.css";
import "roboto-fontface/css/roboto/roboto-fontface.css";
import "@vc-shell/framework/dist/index.css";

async function startApp() {
  const { loadUser } = useUser();
  await loadUser();

  const app = createApp(RouterView);

  app.use(VirtoShellFramework, {
    router,
    i18n: {
      locale: import.meta.env.APP_I18N_LOCALE,
      fallbackLocale: import.meta.env.APP_I18N_FALLBACK_LOCALE,
    },
  });

  Object.values(modules.default).forEach((module) => {
    app.use(module.default, { router });
  });

  app.use(router);

  Object.entries(locales).forEach(([key, message]) => {
    app.config.globalProperties.$mergeLocaleMessage(key, message);
  });

  // Should be after merging locales
  bootstrap(app);

  // Global error handler
  app.config.errorHandler = (err: unknown) => {
    notification.error((err as Error).toString(), {
      timeout: 5000,
    });
  };

  await router.isReady();
  app.mount("#app");
}

startApp();