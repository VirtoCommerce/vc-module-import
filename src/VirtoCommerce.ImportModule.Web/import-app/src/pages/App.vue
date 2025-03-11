<template>
  <VcApp
    :is-ready="isReady"
    :logo="uiSettings.logo"
    :title="uiSettings.title"
    :version="version"
    disable-menu
  >
    <template #toolbar:user-dropdown="{ userDropdown }">
      <component
        :is="userDropdown"
        disabled
      >
      </component>
    </template>
  </VcApp>
</template>

<script lang="ts" setup>
import { useSettings, useUser } from "@vc-shell/framework";
import { onMounted, ref } from "vue";
import { useRoute } from "vue-router";
// eslint-disable-next-line import/no-unresolved
import logoImage from "/assets/logo.svg";
import { useOrganizationDetails } from "../composables/useOrganizationDetails";

const { isAuthenticated } = useUser();
const { uiSettings, applySettings } = useSettings();
const { organizationDetails, getOrganizationInfo } = useOrganizationDetails();
const route = useRoute();
const isReady = ref(false);
const version = import.meta.env.PACKAGE_VERSION;

onMounted(async () => {
  try {
    if (isAuthenticated.value) {
      await customizationHandler();

      isReady.value = true;
    }
  } catch (e) {
    console.error(e);
    throw e;
  }
});

console.debug(`Initializing App`);

async function customizationHandler() {
  await getOrganizationInfo(route?.params?.sellerId as string);

  applySettings({
    logo: organizationDetails.value?.organizationLogoUrl || logoImage,
    title: organizationDetails.value?.organizationName || "Vendor Portal",
  });
}
</script>

<style lang="scss">
@import "./../styles/index.scss";
@import "./../styles/base.scss";
</style>
