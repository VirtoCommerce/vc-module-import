<template>
  <VcBlade
    :title="$t('IMPORT.PAGES.IMPORT_PROFILES.TITLE')"
    width="50%"
    :toolbar-items="bladeToolbar"
  >
    <VcDataTable
      v-model:active-item-id="selectedItemId"
      v-model:search-value="searchValue"
      :loading="profilesLoading"
      :items="importProfiles ?? []"
      :footer="false"
      :searchable="true"
      state-key="importProfiles"
      class="tw-grow tw-basis-0"
      @search="onSearchList"
      @row-click="onItemClick"
    >
      <VcColumn
        id="name"
        :title="$t('IMPORT.PAGES.IMPORT_PROFILES.TABLE.HEADER.PROFILE_NAME')"
        :always-visible="true"
      />
      <VcColumn
        id="dataImporterType"
        :title="$t('IMPORT.PAGES.IMPORT_PROFILES.TABLE.HEADER.IMPORTER')"
      />
      <VcColumn
        id="longRunningStatus"
        :title="$t('IMPORT.PAGES.IMPORT_PROFILES.TABLE.HEADER.LONG_RUNNING_STATUS')"
      >
        <template #body="{ data }">
          <VcStatus
            v-if="data.inProgress"
            variant="success"
          >
            {{ $t(`IMPORT.PAGES.IMPORT_PROFILES.TABLE.CELLS.LONG_RUNNING_STATUS.RUNNING`) }}
          </VcStatus>
        </template>
      </VcColumn>
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { useBlade, usePermissions } from "@vc-shell/framework";
import { useDebounceFn } from "@vueuse/core";
import { computed, markRaw, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { UserPermissions } from "./../types";
import useImport, { ExtProfile } from "../composables/useImport";
import importNew from "./import-new.vue";

import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";

const { param, exposeToChildren } = useBlade();
defineBlade({
  url: "/import",
  name: "ImportProfileSelector",
  isWorkspace: true,
  menuItem: {
    title: "IMPORT.MENU.TITLE",
    icon: "lucide-file-input",
    priority: 4,
  },
});

const { t } = useI18n({ useScope: "global" });
const { hasAccess } = usePermissions();
const {
  openBlade
} = useBlade();

const { importProfiles, profilesLoading, fetchImportProfiles } = useImport();

const bladeWidth = ref(50);
const selectedProfileId = ref();
const title = computed(() => t("IMPORT.PAGES.IMPORT_PROFILES.TITLE"));
const searchValue = ref();
const selectedItemId = ref<string>();

watch(
  () => param.value,
  async (newParam) => {
    selectedItemId.value = newParam;
  },
  { immediate: true },
);

const bladeToolbar = computed(() => {
  return [
    {
      id: "refresh",
      title: computed(() => t("IMPORT.PAGES.IMPORT_PROFILES.TOOLBAR.REFRESH")),
      icon: "lucide-refresh-cw",
      async clickHandler() {
        await reload();
      },
    },
    {
      id: "add",
      title: computed(() => t("IMPORT.PAGES.IMPORT_PROFILES.TOOLBAR.ADD_PROFILE")),
      icon: "lucide-plus",
      async clickHandler() {
        await newProfile();
      },
      isVisible: computed(() => hasAccess(UserPermissions.SellerImportProfilesEdit)),
    },
  ];
});

const onItemClick = (event: { data: ExtProfile; index: number; originalEvent: Event }) => {
  const item = event.data;
  openBlade({
    name: "ImportNew",
    param: item.id,

    options: {
      importJobId: item && item.inProgress ? item.jobId : undefined,
    },

    onOpen() {
      selectedItemId.value = item.id;
    },

    onClose() {
      selectedItemId.value = undefined;
    }
  });
};

async function newProfile() {
  await openBlade({
    name: "ImportProfileDetails"
  });
  bladeWidth.value = 70;
}

async function reload() {
  await fetchImportProfiles();
}

const onSearchList = useDebounceFn(async (keyword: string) => {
  searchValue.value = keyword;
  await fetchImportProfiles({
    keyword,
  });
}, 1000);

async function openImporter(profileId: string) {
  const profile = importProfiles.value?.find((importProfile) => importProfile.id === profileId);

  await openBlade({
    name: "ImportNew",
    param: profileId,

    options: {
      importJobId: profile && profile.inProgress ? profile.jobId : undefined,
    },

    onOpen() {
      selectedProfileId.value = profileId;
    },

    onClose() {
      selectedProfileId.value = undefined;
    }
  });
  bladeWidth.value = 50;
}

onMounted(async () => {
  await reload();
});

exposeToChildren({
  openImporter,
  reload,
});
</script>
