import { useLanguages } from "@vc-shell/framework";
import { App } from "vue";

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function bootstrap(app: App) {
  const { setLocale, currentLocale } = useLanguages();

  setLocale(currentLocale.value);
}
