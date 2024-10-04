<template>
  <VcBlade
    v-loading:1001="bladeLoading"
    :title="title"
    width="50%"
    :toolbar-items="bladeToolbar"
    :closable="closable"
    :expanded="expanded"
    @close="$emit('close:blade')"
    @expand="$emit('expand:blade')"
    @collapse="$emit('collapse:blade')"
  >
    <VcContainer class="import-new">
      <VcCol>
        <div class="tw-p-3">
          <VcRow>
            <VcCard
              :header="
                importStarted
                  ? $t('IMPORT.PAGES.PRODUCT_IMPORTER.FILE_UPLOAD.IMPORT_RESULTS')
                  : uploadedFile && uploadedFile.url
                    ? $t('IMPORT.PAGES.PRODUCT_IMPORTER.FILE_UPLOAD.TITLE_UPLOADED')
                    : $t('IMPORT.PAGES.PRODUCT_IMPORTER.FILE_UPLOAD.TITLE')
              "
            >
              <!-- File upload -->
              <VcCol
                v-if="!importStarted && !(uploadedFile && uploadedFile.url)"
                class="tw-p-5"
              >
                <VcRow class="tw-mb-4">
                  <a
                    class="vc-link"
                    :href="sampleTemplateUrl"
                    >{{ $t("IMPORT.PAGES.TEMPLATE.DOWNLOAD_TEMPLATE") }}</a
                  >
                  &nbsp;{{ $t("IMPORT.PAGES.TEMPLATE.FOR_REFERENCE") }}
                </VcRow>
                <VcRow>
                  <VcCol>
                    <VcRow class="tw-mb-4">
                      <VcFileUpload
                        variant="file-upload"
                        :notification="true"
                        accept="*.*"
                        :loading="fileLoading"
                        @upload="uploadCsv"
                      ></VcFileUpload>
                    </VcRow>
                    <VcRow>
                      <Field
                        v-slot="{ field, errorMessage, handleChange, errors }"
                        :model-value="profile.importFileUrl"
                        :label="$t('IMPORT.PAGES.PRODUCT_IMPORTER.EXTERNAL_URL.TITLE')"
                        rules="url"
                        name="externalUrl"
                      >
                        <VcInput
                          v-bind="field"
                          v-model="profile.importFileUrl"
                          class="tw-grow tw-basis-0"
                          :placeholder="$t('IMPORT.PAGES.PRODUCT_IMPORTER.EXTERNAL_URL.PLACEHOLDER')"
                          required
                          clearable
                          :error="!!errors.length"
                          :error-message="errorMessage"
                          @update:model-value="handleChange"
                        >
                          <template #append>
                            <slot name="button">
                              <VcButton
                                :outline="true"
                                @click="saveExternalUrl()"
                              >
                                {{ $t("IMPORT.PAGES.PRODUCT_IMPORTER.EXTERNAL_URL.SAVE") }}
                              </VcButton>
                            </slot>
                          </template>
                        </VcInput>
                      </Field>
                    </VcRow>
                  </VcCol>
                </VcRow>
              </VcCol>
              <!-- Uploaded file actions -->
              <VcCol v-else>
                <VcRow v-if="uploadedFile && uploadedFile.url">
                  <import-upload-status
                    :upload-actions="uploadActions"
                    :uploaded-file="uploadedFile"
                    :is-uploaded="isValid"
                    :is-started="importStarted"
                    class="tw-p-5"
                  >
                  </import-upload-status>
                </VcRow>
              </VcCol>
              <!-- Uploaded file import status -->
              <ImportStat :import-status="importStatus" />
            </VcCard>
          </VcRow>
        </div>
        <ImportErrorsCard :import-status="importStatus" />
        <!-- History-->
        <VcCol
          v-if="!importStarted"
          class="tw-p-3"
        >
          <VcCard
            :header="$t('IMPORT.PAGES.LAST_EXECUTIONS')"
            :fill="true"
            class="import-new__history"
          >
            <VcTable
              :columns="columns"
              :loading="importHistoryLoading"
              :items="importHistory ?? []"
              :header="false"
              :total-count="totalHistoryCount"
              :pages="historyPages"
              :selected-item-id="selectedItemId"
              :current-page="currentPage"
              state-key="import_history"
              @item-click="onItemClick"
              @pagination-click="onPaginationClick"
            >
              <!-- Override name column template -->
              <template #item_profileName="itemData">
                <div class="tw-flex tw-flex-col">
                  <div class="tw-truncate">
                    {{ itemData.item.profileName }}
                  </div>
                </div>
              </template>
              <!-- Override finished column template -->
              <template #item_finished="itemData">
                <ImportStatus :item="itemData.item" />
              </template>
            </VcTable>
          </VcCard>
        </VcCol>
      </VcCol>
    </VcContainer>
    <ImportPopup
      v-if="importPreview"
      :columns="popupColumns"
      :items="popupItems"
      :total="previewTotalNum ?? 0"
      :disabled="!!(importStatus && importStatus.jobId)"
      @close="importPreview = false"
      @start-import="initializeImporting"
    ></ImportPopup>
  </VcBlade>
</template>

<script lang="ts" setup>
import { computed, onMounted, ref, watch, ComputedRef } from "vue";
import * as _ from "lodash-es";
import {
  IParentCallArgs,
  moment,
  VcContainer,
  VcCol,
  VcRow,
  VcBlade,
  VcCard,
  VcFileUpload,
  VcProgress,
  VcIcon,
  VcHint,
  VcTable,
  IBladeToolbar,
  ITableColumns,
  usePermissions,
  useNotifications,
  notification,
  useBladeNavigation,
  VcButton,
} from "@vc-shell/framework";
import { UserPermissions } from "./../types";
import useImport, { ExtProfile } from "../composables/useImport";
import { ImportDataPreview, ImportPushNotification, ImportRunHistory } from "@virtocommerce/import-app-api";
import ImportPopup from "../components/ImportPopup.vue";
import ImportUploadStatus from "../components/ImportUploadStatus.vue";
import ImportStatus from "../components/ImportStatus.vue";
import { Field } from "vee-validate";
import { useI18n } from "vue-i18n";
import { ImportStat, ImportErrorsCard, ImportUploadedFile } from "../components";

export interface Props {
  expanded: boolean;
  closable: boolean;
  param?: string;
  options?: {
    importJobId?: string;
    title?: string;
  };
}

export interface Emits {
  (event: "close:blade"): void;
  (event: "collapse:blade"): void;
  (event: "expand:blade"): void;
  (event: "parent:call", args: IParentCallArgs): void;
}

interface INotificationActions {
  name: string | ComputedRef<string>;
  clickHandler(): void;
  outline: boolean;
  variant?: InstanceType<typeof VcButton>["$props"]["variant"];
  isVisible?: boolean | ComputedRef<boolean>;
  disabled?: boolean | ComputedRef<boolean>;
}

defineOptions({
  // url: "/importer",
  name: "ImportNew",
});

const props = withDefaults(defineProps<Props>(), {
  expanded: true,
  closable: true,
});

const emit = defineEmits<Emits>();

const { openBlade, resolveBladeByName } = useBladeNavigation();

const { t } = useI18n({ useScope: "global" });
const { hasAccess } = usePermissions();

const {
  loading: importLoading,
  importHistory,
  uploadedFile,
  importStatus,
  isValid,
  profile,
  historyPages,
  totalHistoryCount,
  currentPage,
  importHistoryLoading,
  dataImportersLoading,
  previewDataLoading,
  profilesLoading,
  cancelImport,
  clearImport,
  previewData,
  setFile,
  startImport,
  fetchImportHistory,
  setErrorMessage,
  clearErrorMessage,
  init,
  getTasks,
} = useImport();
const { moduleNotifications, markAsRead } = useNotifications("ImportPushNotification");
const fileLoading = ref(false);
const preview = ref<ImportDataPreview>();
const importPreview = ref(false);
const popupColumns = ref<ITableColumns[]>([]);
const popupItems = ref<Record<string, unknown>[]>([]);
const title = computed(() =>
  props.param && profileDetails.value.name ? profileDetails.value.name : props.options?.title,
);

const cancelled = ref(false);
const notificationId = ref();
const previewLoading = ref(false);
const selectedItemId = ref();
const bladeWidth = ref(70);

watch(
  moduleNotifications,
  (newVal: ImportPushNotification[]) => {
    newVal.forEach((message) => {
      const messageContent = message.profileName ? `${message.profileName}: ${message.title}` : message.title;

      if (!importStarted.value && message.profileId === props.param) {
        getTasks({
          profileId: message.profileId,
          importJobId: message.jobId,
        });
      }

      if (!message.finished) {
        if (!notificationId.value && messageContent) {
          notificationId.value = notification(messageContent, {
            timeout: false,
          });
        } else {
          notification.update(notificationId.value, {
            content: messageContent,
          });
        }
      } else {
        if (message.title === "Import failed") {
          notification.update(notificationId.value, {
            timeout: 5000,
            content: messageContent,
            type: "error",
            onClose() {
              markAsRead(message);
              notificationId.value = undefined;
            },
          });
        } else {
          notification.update(notificationId.value, {
            timeout: 5000,
            content: messageContent,
            type: "success",
            onClose() {
              markAsRead(message);
              notificationId.value = undefined;
            },
          });
        }
      }
    });
  },
  { deep: true },
);

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "edit",
    title: computed(() => t("IMPORT.PAGES.PRODUCT_IMPORTER.TOOLBAR.EDIT")),
    icon: "fas fa-pencil-alt",
    clickHandler() {
      openBlade({
        blade: resolveBladeByName("ImportProfileDetails"),
        options: {
          importer: profileDetails.value.importer,
        },
        param: profile.value.id,
      });
    },
    isVisible: computed(() => !!(hasAccess(UserPermissions.SellerImportProfilesEdit) && profile.value)),
    disabled: computed(() => importLoading.value || !profile.value.name || importStarted.value),
  },
  {
    id: "cancel",
    title: computed(() => t("IMPORT.PAGES.PRODUCT_IMPORTER.TOOLBAR.CANCEL")),
    icon: "fas fa-ban",
    async clickHandler() {
      if (importStatus.value?.inProgress) {
        try {
          await cancelImport();
          cancelled.value = true;
        } catch (e) {
          cancelled.value = false;
          throw e;
        }
      }
    },
    disabled: computed(() => {
      return !importStatus.value?.inProgress || cancelled.value;
    }),
    isVisible: computed(() => !!props.param),
  },
  {
    id: "newRun",
    title: computed(() => t("IMPORT.PAGES.PRODUCT_IMPORTER.TOOLBAR.NEW_RUN")),
    icon: "fas fa-plus",
    clickHandler() {
      emit("parent:call", {
        method: "openImporter",
        args: props.param,
      });
    },
    disabled: computed(() => importStatus.value?.inProgress),
    isVisible: computed(() => !!(importStatus.value && profile.value.name)),
  },
]);

async function reRunImport(importJobId?: string) {
  const jobId = props.options?.importJobId || importJobId;
  const historyItem = importHistory.value && importHistory.value.find((x) => x.jobId === jobId);

  if (historyItem?.fileUrl) {
    const correctedProfile = profile?.value;
    correctedProfile.importFileUrl = historyItem.fileUrl;
    correctedProfile.inProgress = false;
    correctedProfile.jobId = undefined;
    await start(correctedProfile);
  }
}

const columns = ref<ITableColumns[]>([
  {
    id: "profileName", // temp
    title: computed(() => t("IMPORT.PAGES.LIST.TABLE.HEADER.PROFILE_NAME")),
    alwaysVisible: true,
  },
  {
    id: "createdBy",
    title: computed(() => t("IMPORT.PAGES.LIST.TABLE.HEADER.CREATED_BY")),
    width: 147,
  },
  {
    id: "finished",
    title: computed(() => t("IMPORT.PAGES.LIST.TABLE.HEADER.STATUS")),
    width: 147,
  },
  {
    id: "createdDate",
    title: computed(() => t("IMPORT.PAGES.LIST.TABLE.HEADER.STARTED_AT")),
    width: 147,
    type: "date",
    format: "L LT",
  },
  {
    id: "errorsCount",
    title: computed(() => t("IMPORT.PAGES.LIST.TABLE.HEADER.ERROR_COUNT")),
    width: 118,
    sortable: true,
  },
]);

const uploadActions = ref<INotificationActions[]>([
  {
    name: computed(() => t("IMPORT.PAGES.ACTIONS.UPLOADER.ACTIONS.USE_ANOTHER_ONE")),
    clickHandler() {
      clearImport();
      clearErrorMessage();
    },
    outline: true,
    variant: "danger",
    isVisible: computed(() => !inProgress.value),
  },
  {
    name: computed(() => t("IMPORT.PAGES.ACTIONS.UPLOADER.ACTIONS.PREVIEW")),
    async clickHandler() {
      try {
        previewLoading.value = true;
        preview.value = await previewData();
        popupItems.value = [];
        popupColumns.value = [];
        if (preview.value && preview.value.records && preview.value.records.length) {
          for (const recordKey in preview.value.records[0]) {
            popupColumns.value.push({
              id: recordKey,
              title: recordKey,
              width: 130,
            });
          }
          preview.value.records.forEach((record) => {
            popupItems.value.push(record);
          });
          importPreview.value = true;
        }
      } catch (e: unknown) {
        setErrorMessage((e as Error).message);
        throw e;
      } finally {
        previewLoading.value = false;
      }
    },
    outline: true,
    isVisible: computed(() => isValid.value && !importStarted.value),
    disabled: computed(() => previewLoading.value),
  },
  {
    name: computed(() => t("IMPORT.PAGES.ACTIONS.UPLOADER.ACTIONS.START_IMPORT")),
    async clickHandler() {
      await start();
    },
    outline: false,
    isVisible: computed(() => isValid.value && !importStarted.value),
    disabled: computed(() => (importStatus.value && importStatus.value.inProgress) || importLoading.value),
  },
]);

const inProgress = computed(() => (importStatus.value && importStatus.value.inProgress) || false);

const bladeLoading = computed(
  () =>
    importLoading.value ||
    dataImportersLoading.value ||
    profilesLoading.value ||
    previewDataLoading.value ||
    importHistoryLoading.value,
);

const profileDetails = computed(() => profile.value);

const importStarted = computed(() => !!(importStatus.value && importStatus.value.jobId));

const previewTotalNum = computed(() => preview.value?.totalCount);

async function onItemClick(item: ImportRunHistory) {
  if (item?.jobId && item.profileId) {
    // const historyItem = importHistory.value && importHistory.value.find((x) => x.jobId === item?.jobId);

    // if (historyItem) {
    //   updateStatus(historyItem);
    // } else {
    //   getLongRunning({ id: item.profileId });
    // }

    openBlade({
      blade: resolveBladeByName("ImportProcess"),
      options: {
        importJobId: item?.jobId,
        title: item?.profileName,
      },
      param: item?.profileId,
      onOpen() {
        selectedItemId.value = item?.id;
      },
      onClose() {
        selectedItemId.value = undefined;
      },
    });

    bladeWidth.value = 50;
  }
}

onMounted(async () => {
  clearImport();
  await init({ profileId: props.param, importJobId: props.options?.importJobId });
});

async function uploadCsv(files: FileList | null) {
  if (files && files.length) {
    try {
      fileLoading.value = true;
      const formData = new FormData();
      formData.append("file", files[0]);
      const result = await fetch(`/api/assets?folderUrl=/tmp`, {
        method: "POST",
        body: formData,
      });
      const response = await result.json();
      if (response?.length) {
        setFile(response[0]);
      }
      files = null;
    } catch (e: unknown) {
      setErrorMessage((e as Error).message);
      if (files)
        setFile({
          name: files[0].name,
          size: files[0].size / (1024 * 1024),
        });
      throw e;
    } finally {
      fileLoading.value = false;
    }
  }
}

async function saveExternalUrl() {
  setFile({
    name: profile.value.importFileUrl?.substring(profile.value.importFileUrl.lastIndexOf("/") + 1),
    url: profile.value.importFileUrl,
    size: Number(0),
  });
}

async function start(profile?: ExtProfile) {
  try {
    clearErrorMessage();
    await startImport(profile);
  } catch (e: unknown) {
    setErrorMessage((e as Error).message);
    throw e;
  }
}

function initializeImporting() {
  importPreview.value = false;
  start();
}

function reloadParent() {
  emit("parent:call", {
    method: "reload",
  });
  emit("close:blade");
}

const sampleTemplateUrl = computed(() => {
  return profile.value && profile.value.importer && profile.value.importer.metadata
    ? profile.value.importer.metadata.sampleCsvUrl
    : "#";
});

async function onPaginationClick(page: number) {
  await fetchImportHistory({
    skip: (page - 1) * 15,
    profileId: props.param,
  });
}

defineExpose({
  reloadParent,
  reRunImport,
  title,
});
</script>

<style lang="scss">
:root {
  --color-error: var(--base-error-color);
  --import-new-description-color: var(--neutrals-800);
  --import-new-border-color: var(--secondary-200);
  --import-new-preview-text-color: var(--primary-500);
  --import-new-icon-color: var(--secondary-500);
  --import-new-badge-color-success: var(--success-500);
  --import-new-badge-color-info: var(--secondary-500);
  --import-new-badge-color-warning: var(--warning-500);
  --import-new-badge-color-error: var(--danger-500);
  --import-new-progress-text-color: var(--secondary-500);
  --import-new-border-top-color: var(--neutrals-200);
}

.import-new {
  & .vc-container__inner {
    @apply tw-flex tw-flex-col;
  }

  &__error {
    --hint-color: var(--color-error);
  }

  &__skipped {
    & .vc-card__body {
      @apply tw-flex tw-flex-col;
    }
  }

  &__history {
    & .vc-card__body {
      @apply tw-flex tw-flex-col;
    }
  }
}
</style>
