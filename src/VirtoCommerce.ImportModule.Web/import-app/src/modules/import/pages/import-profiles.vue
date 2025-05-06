<template>
  <VcBlade
    :title="$t('IMPORT.PAGES.IMPORT_PROFILES.TITLE')"
    width="50%"
    :toolbar-items="bladeToolbar"
    :closable="closable"
    :expanded="expanded"
    @expand="$emit('expand:blade')"
    @collapse="$emit('collapse:blade')"
  >
  <!-- @vue-generic {ExtProfile} -->
    <VcTable
      :loading="profilesLoading"
      :columns="columns"
      :items="importProfiles ?? []"
      :footer="false"
      :selected-item-id="unref(selectedItemId)"
      state-key="importProfiles"
      @search:change="onSearchList"
      @item-click="onItemClick"
    >
      <template #item_longRunningStatus="{ item }">
        <VcStatus
          v-if="item.inProgress"
          variant="success"
        >
          {{ $t(`IMPORT.PAGES.IMPORT_PROFILES.TABLE.CELLS.LONG_RUNNING_STATUS.RUNNING`) }}
        </VcStatus>
      </template>
    </VcTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { ITableColumns, useBladeNavigation, usePermissions, useFunctions } from "@vc-shell/framework";
import { computed, markRaw, onMounted, ref, unref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { UserPermissions } from "./../types";
import useImport, { ExtProfile } from "../composables/useImport";
import importNew from "./import-new.vue";

export interface Props {
  expanded?: boolean;
  closable?: boolean;
  param?: string;
  options?: {
    importJobId: string;
  };
}

export interface Emits {
  (event: "collapse:blade"): void;
  (event: "expand:blade"): void;
}

defineOptions({
  url: "/import",
  name: "ImportProfileSelector",
  isWorkspace: true,
  menuItem: {
    title: "IMPORT.MENU.TITLE",
    icon: "fas fa-file-import",
    priority: 4,
  },
});

const props = defineProps<Props>();

defineEmits<Emits>();

const { t } = useI18n({ useScope: "global" });
const { hasAccess } = usePermissions();
const { openBlade, resolveBladeByName } = useBladeNavigation();
const { debounce } = useFunctions();

const { importProfiles, profilesLoading, fetchImportProfiles } = useImport();

const bladeWidth = ref(50);
const selectedProfileId = ref();
const title = computed(() => t("IMPORT.PAGES.IMPORT_PROFILES.TITLE"));
const searchValue = ref();
const selectedItemId = ref<string>();

watch(
  () => props.param,
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
      icon: "fas fa-sync-alt",
      async clickHandler() {
        await reload();
      },
    },
    {
      id: "add",
      title: computed(() => t("IMPORT.PAGES.IMPORT_PROFILES.TOOLBAR.ADD_PROFILE")),
      icon: "fas fa-plus",
      async clickHandler() {
        await newProfile();
      },
      isVisible: computed(() => hasAccess(UserPermissions.SellerImportProfilesEdit)),
    },
  ];
});

const columns = ref<ITableColumns[]>([
  {
    id: "name",
    title: computed(() => t("IMPORT.PAGES.IMPORT_PROFILES.TABLE.HEADER.PROFILE_NAME")),
    alwaysVisible: true,
  },
  {
    id: "dataImporterType",
    title: computed(() => t("IMPORT.PAGES.IMPORT_PROFILES.TABLE.HEADER.IMPORTER")),
  },
  {
    id: "longRunningStatus",
    title: computed(() => t("IMPORT.PAGES.IMPORT_PROFILES.TABLE.HEADER.LONG_RUNNING_STATUS")),
  },
  // {
  //   id: "lastRun",
  //   title: computed(() => t("IMPORT.PAGES.IMPORT_PROFILES.TABLE.HEADER.LAST_RUN")),
  //   type: "date-time",
  // },
]);

const onItemClick = (item: ExtProfile) => {
  openBlade({
    blade: markRaw(importNew),
    param: item.id,
    options: {
      importJobId: item && item.inProgress ? item.jobId : undefined,
    },
    onOpen() {
      selectedItemId.value = item.id;
    },
    onClose() {
      selectedItemId.value = undefined;
    },
  });
};

async function newProfile() {
  await openBlade({
    blade: resolveBladeByName("ImportProfileDetails"),
  });
  bladeWidth.value = 70;
}

async function reload() {
  await fetchImportProfiles();
}

const onSearchList = debounce(async (keyword: string) => {
  searchValue.value = keyword;
  await fetchImportProfiles({
    keyword,
  });
}, 1000);

async function openImporter(profileId: string) {
  const profile = importProfiles.value?.find((profile) => profile.id === profileId);

  await openBlade({
    blade: resolveBladeByName("ImportNew"),
    param: profileId,
    options: {
      importJobId: profile && profile.inProgress ? profile.jobId : undefined,
    },
    onOpen() {
      selectedProfileId.value = profileId;
    },
    onClose() {
      selectedProfileId.value = undefined;
    },
  });
  bladeWidth.value = 50;
}

onMounted(async () => {
  await reload();
});

defineExpose({
  openImporter,
  reload,
  title,
});
</script>
