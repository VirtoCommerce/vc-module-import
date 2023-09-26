<template>
  <VcLoading
    v-if="!isReady"
    active
    class="app__loader"
  />
  <VcApp
    v-else
    :toolbar-items="toolbarItems"
    :is-ready="isReady"
    :is-authorized="isAuthorized"
    :logo="uiSettings.logo"
    :title="uiSettings.title"
    :version="version"
    :pages="pages"
    :blades-refs="bladesRefs"
    @backlink:click="closeBlade($event)"
    @close="closeBlade($event)"
    @logo:click="openDashboard"
  >
    <!-- App Switcher -->
    <template
      v-if="appsList && appsList.length"
      #appSwitcher
    >
      <VcAppSwitcher
        :apps-list="appsList"
        @on-click="switchApp($event)"
      />
    </template>

    <template
      v-if="isAuthorized"
      #bladeNavigation
    >
      <VcBladeNavigation
        ref="bladeNavigationRefs"
        :blades="blades"
        :workspace-options="workspaceOptions"
        :workspace-param="workspaceParam"
        @on-close="closeBlade($event)"
        @on-parent-call="(e) => onParentCall(e.id, e.args)"
        @vue:mounted="resolveLastBlade(pages)"
      ></VcBladeNavigation>
    </template>

    <template #modals>
      <VcPopupContainer />
    </template>
  </VcApp>
</template>

<script lang="ts" setup>
import {
  useAppSwitcher,
  useNotifications,
  useSettings,
  useUser,
  useBladeNavigation,
  VcNotificationDropdown,
  usePopup,
  useMenuComposer,
  ChangePassword,
  LanguageSelector,
  UserDropdownButton,
  BladePageComponent,
  NotificationTemplateConstructor,
} from "@vc-shell/framework";
import { computed, inject, onMounted, reactive, ref, Ref, markRaw, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
// eslint-disable-next-line import/no-unresolved
import avatarImage from "/assets/avatar.jpg";
// eslint-disable-next-line import/no-unresolved
import logoImage from "/assets/logo.svg";
import { useOrganizationDetails } from "../composables/useOrganizationDetails";
import { useI18n } from "vue-i18n";

const { open } = usePopup({
  component: ChangePassword,
});

const { t, locale: currentLocale, availableLocales, getLocaleMessage } = useI18n({ useScope: "global" });
const { user, loadUser, signOut } = useUser();
const { notifications, loadFromHistory, markAllAsRead } = useNotifications();
// const { checkPermission } = usePermissions();
const { getUiCustomizationSettings, uiSettings, applySettings } = useSettings();
const { blades, bladesRefs, workspaceOptions, workspaceParam, closeBlade, onParentCall, resolveLastBlade } =
  useBladeNavigation();
const { toolbarComposer } = useMenuComposer();
const { appsList, switchApp, getApps } = useAppSwitcher();
const { organizationDetails, getOrganizationInfo } = useOrganizationDetails();
const route = useRoute();
const router = useRouter();
const isAuthorized = ref(false);
const isReady = ref(false);
const pages = inject<BladePageComponent[]>("pages");
const notificationTemplates = inject<NotificationTemplateConstructor[]>("notificationTemplates");
const isDesktop = inject<Ref<boolean>>("isDesktop");
const isMobile = inject<Ref<boolean>>("isMobile");
const version = import.meta.env.PACKAGE_VERSION;
const bladeNavigationRefs = ref();

onMounted(async () => {
  try {
    await loadUser();
    await getApps();
    langInit();
    await customizationHandler();
    await loadFromHistory();

    isReady.value = true;
  } catch (e) {
    if (!isAuthorized.value) {
      router.push("/login");
    }
    throw e;
  }
});

watch(
  user,
  (value) => {
    isAuthorized.value = !!value?.userName;
  },
  { immediate: true }
);

watch(
  () => bladeNavigationRefs.value?.bladesRefs,
  (newVal) => {
    bladesRefs.value = newVal;
  },
  { deep: true }
);

console.debug(`Initializing App`);

const toolbarItems = computed(() =>
  toolbarComposer([
    {
      component: markRaw(LanguageSelector),
      options: {
        value: currentLocale.value as string,
        title: t("SHELL.TOOLBAR.LANGUAGE"),
        languageItems: availableLocales.map((locale: string) => ({
          lang: locale,
          title: (getLocaleMessage(locale) as { language_name: string }).language_name,
          clickHandler(lang: string) {
            currentLocale.value = lang;
            localStorage.setItem("VC_LANGUAGE_SETTINGS", lang);
          },
        })),
      },
      isVisible: isDesktop.value ? isDesktop.value : isMobile.value ? route.path === "/" : false,
    },
    {
      isAccent: notifications.value.some((item) => item.isNew),
      component: markRaw(VcNotificationDropdown),
      options: {
        title: t("SHELL.TOOLBAR.NOTIFICATIONS"),
        notifications: notifications.value,
        templates: notificationTemplates,
        onOpen() {
          if (notifications.value.some((x) => x.isNew)) {
            markAllAsRead();
          }
        },
      },
    },
    {
      component: markRaw(UserDropdownButton),
      options: {
        avatar: avatarImage,
        name: user.value?.userName,
        role: user.value?.isAdministrator ? "Administrator" : "Seller account",
        menuItems: [],
      },
      isVisible: isDesktop.value,
    },
  ])
);

function langInit() {
  const lang = localStorage.getItem("VC_LANGUAGE_SETTINGS");

  if (lang) {
    currentLocale.value = lang;
  } else {
    currentLocale.value = "en";
  }
}

const openDashboard = () => {
  console.debug(`openDashboard() called.`);

  // Close all opened pages with onBeforeClose callback
  closeBlade(0);

  router.push("/");
};

async function customizationHandler() {
  await getOrganizationInfo(route?.params?.userId as string);
  await getUiCustomizationSettings();

  if (organizationDetails.value.organizationLogoUrl) {
    applySettings({ logo: organizationDetails.value.organizationLogoUrl });
  } else if (!uiSettings.value.logo) {
    applySettings({ logo: logoImage });
  }

  if (organizationDetails.value.organizationName) {
    applySettings({ title: organizationDetails.value.organizationName });
  } else if (!uiSettings.value.title) {
    applySettings({ title: undefined });
  }

  // if (!uiSettings.value.logo) {
  //   applySettings({ logo: logoImage });
  // }
  // if (!uiSettings.value.title) {
  //   applySettings({ title: undefined });
  // }
}
</script>

<style lang="scss">
.vc-theme_light {
  --background-color: #f5f6f9;
  --top-bar-color: #161d25;
  --basic-black-color: #333333;
  --tooltips-color: #a5a5a5;

  --primary-color: #43b0e6;
  --primary-color-hover: #319ed4;
  --primary-color-disabled: #a9ddf6;

  --special-color: #f89406;
  --special-color-hover: #eb8b03;
  --special-color-disabled: #fed498;

  /* Layout variables */
  --app-bar-height: 60px;
  --app-bar-background-color: var(--top-bar-color);
  --app-bar-toolbar-icon-background-hover: #2e3d4e;
  --app-bar-toolbar-item-width: 50px;
  --app-bar-divider-color: #2e3d4e;
  --app-bar-toolbar-icon-color: #7e8e9d;
  --app-bar-account-info-role-color: #838d9a;
}

html,
body,
#app {
  @apply tw-font-roboto tw-h-full tw-w-full tw-m-0 tw-fixed tw-overflow-hidden tw-overscroll-y-none;
}

body {
  @apply tw-text-base;
}

h1,
h2,
h3,
h4,
h5,
h6,
button,
input,
select,
textarea {
  @apply tw-font-roboto;
}
::-webkit-input-placeholder {
  @apply tw-font-roboto;
}
:-moz-placeholder {
  @apply tw-font-roboto;
}
::-moz-placeholder {
  @apply tw-font-roboto;
}
:-ms-input-placeholder {
  @apply tw-font-roboto;
}

.vc-app.vc-theme_light {
  --background-color: #f2f2f2;
  --top-bar-color: #ffffff;
  --app-background: linear-gradient(180deg, #e4f5fb 5.06%, #e8f3f2 100%), linear-gradient(0deg, #e8f2f3, #e8f2f3),
    #eef2f8;
  --app-bar-background-color: #ffffff;
  --app-bar-divider-color: #ffffff;
  --app-bar-toolbar-item-width: 50px;
  --app-bar-toolbar-icon-color: #7e8e9d;
  --app-bar-toolbar-icon-color-hover: #465769;
  --app-bar-toolbar-icon-background-hover: #ffffff;
  --app-bar-account-info-name-color: #161d25;
  --app-bar-account-info-role-color: #7e8e9d;
}

.app {
  &__loader {
    background: var(--app-background);
  }
}
</style>
