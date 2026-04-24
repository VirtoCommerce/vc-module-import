import { useLanguages } from "@vc-shell/framework";
import { App } from "vue";

export async function bootstrap(app: App) {
  const { setLocale, currentLocale } = useLanguages();

  setLocale(currentLocale.value);

  // Load backend module localizations (e.g., setting display names defined in platform modules)
  await loadPlatformLocalization(app, currentLocale.value);
}

async function loadPlatformLocalization(app: App, locale: string) {
  try {
    const response = await fetch(`/api/platform/localization?lang=${locale}`);
    if (response.ok) {
      const messages = await response.json();
      // Only merge UPPERCASE top-level keys — these are setting name translations
      // used by VcDynamicProperty (which resolves labels via name.toUpperCase()).
      // This avoids merging ~220 KB of unrelated platform/admin UI localizations.
      const filtered: Record<string, unknown> = {};
      for (const key of Object.keys(messages)) {
        if (key === key.toUpperCase()) {
          filtered[key] = messages[key];
        }
      }
      app.config.globalProperties.$mergeLocaleMessage(locale, filtered);
    }
  } catch (e) {
    console.warn("Failed to load platform localizations:", e);
  }
}
