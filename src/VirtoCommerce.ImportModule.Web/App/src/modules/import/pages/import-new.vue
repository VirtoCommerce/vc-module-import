<template>
  <VcBlade
    :loading="bladeLoading"
    :title="title"
    width="50%"
    :toolbar-items="bladeToolbar"
  >
    <VcContainer class="import-new">
      <VcCol class="tw-gap-4 tw-grow tw-min-h-0">
        <!-- File-based importer: file upload card -->
        <VcRow v-if="!isApiSourceImporter">
          <VcCard
            icon="lucide-upload"
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
              <!-- Template download hint -->
              <div
                v-if="sampleTemplateUrl"
                class="import-new__template-hint tw-mb-5"
              >
                <VcIcon
                  icon="lucide-file-spreadsheet"
                  size="l"
                  class="tw-text-[color:var(--primary-500)] tw-shrink-0"
                />
                <span class="tw-text-sm tw-text-[color:var(--neutrals-600)]">
                  <a
                    class="vc-link tw-font-medium"
                    :href="sampleTemplateUrl"
                  >{{ $t("IMPORT.PAGES.TEMPLATE.DOWNLOAD_TEMPLATE") }}</a>
                  {{ $t("IMPORT.PAGES.TEMPLATE.FOR_REFERENCE") }}
                </span>
              </div>

              <!-- File upload zone -->
              <VcFileUpload
                variant="file-upload"
                :notification="true"
                accept="*.*"
                :loading="fileLoading"
                @upload="uploadCsv"
              />

              <!-- OR divider -->
              <div class="import-new__divider tw-my-5">
                <span class="import-new__divider-text">{{ $t("IMPORT.PAGES.PRODUCT_IMPORTER.EXTERNAL_URL.TITLE") }}</span>
              </div>

              <!-- External URL input -->
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
                    <VcButton
                      :outline="true"
                      @click="saveExternalUrl()"
                    >
                      {{ $t("IMPORT.PAGES.PRODUCT_IMPORTER.EXTERNAL_URL.SAVE") }}
                    </VcButton>
                  </template>
                </VcInput>
              </Field>
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
                />
              </VcRow>
            </VcCol>

            <!-- Uploaded file import status -->
            <ImportStat :import-status="importStatus" />
          </VcCard>
        </VcRow>

        <!-- API-based importer: no file upload needed -->
        <VcRow v-else>
          <VcCard
            icon="lucide-cloud-download"
            :header="
              importStarted
                ? $t('IMPORT.PAGES.PRODUCT_IMPORTER.FILE_UPLOAD.IMPORT_RESULTS')
                : $t('IMPORT.PAGES.PRODUCT_IMPORTER.API_SOURCE.TITLE')
            "
          >
            <VcCol
              v-if="!importStarted"
              class="tw-p-5"
            >
              <div class="tw-flex tw-flex-row tw-items-center tw-justify-end tw-gap-3">
                <VcButton
                  :outline="true"
                  :small="true"
                  :disabled="!isValid || previewLoading"
                  @click="apiPreview"
                >
                  {{ $t("IMPORT.PAGES.ACTIONS.UPLOADER.ACTIONS.PREVIEW") }}
                </VcButton>
                <VcButton
                  :small="true"
                  :disabled="!isValid || (importStatus && importStatus.inProgress) || importLoading"
                  @click="start()"
                >
                  {{ $t("IMPORT.PAGES.ACTIONS.UPLOADER.ACTIONS.START_IMPORT") }}
                </VcButton>
              </div>
            </VcCol>
            <ImportStat :import-status="importStatus" />
          </VcCard>
        </VcRow>

        <ImportErrorsCard :import-status="importStatus" />

        <!-- History -->
        <VcCol
          v-if="!importStarted"
          class="tw-grow tw-min-h-0"
        >
          <VcCard
            icon="lucide-history"
            :header="$t('IMPORT.PAGES.LAST_EXECUTIONS')"
            :fill="true"
            class="import-new__history"
          >
            <VcDataTable
              v-model:active-item-id="selectedItemId"
              :loading="importHistoryLoading"
              :items="importHistory ?? []"
              :header="false"
              :total-count="pagination.totalCount"
              :pagination="pagination"
              state-key="import_history"
              @row-click="onItemClick"
              @pagination-click="pagination.goToPage"
            >
              <VcColumn
                id="profileName"
                :title="$t('IMPORT.PAGES.LIST.TABLE.HEADER.PROFILE_NAME')"
                :always-visible="true"
              >
                <template #body="{ data }">
                  <div class="tw-flex tw-flex-col">
                    <div class="tw-truncate tw-font-medium">
                      {{ data.profileName }}
                    </div>
                  </div>
                </template>
              </VcColumn>
              <VcColumn
                id="createdBy"
                :title="$t('IMPORT.PAGES.LIST.TABLE.HEADER.CREATED_BY')"
                :width="147"
              />
              <VcColumn
                id="finished"
                :title="$t('IMPORT.PAGES.LIST.TABLE.HEADER.STATUS')"
                :width="147"
              >
                <template #body="{ data }">
                  <ImportStatus :item="data" />
                </template>
              </VcColumn>
              <VcColumn
                id="createdDate"
                :title="$t('IMPORT.PAGES.LIST.TABLE.HEADER.STARTED_AT')"
                :width="160"
                type="date"
                format="L LT"
              />
              <VcColumn
                id="errorsCount"
                :title="$t('IMPORT.PAGES.LIST.TABLE.HEADER.ERROR_COUNT')"
                :width="118"
                :sortable="true"
              />
            </VcDataTable>
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
      :json-mode="isApiSourceImporter"
      @close="importPreview = false"
      @start-import="initializeImporting"
    />
  </VcBlade>
</template>

<script lang="ts" setup>
import { computed, onMounted, ref, watch, ComputedRef } from "vue";
import * as _ from "lodash-es";
import {
  IBladeToolbar,
  ITableColumns,
  usePermissions,
  useBladeNotifications,
  notification,
  useBlade,
} from "@vc-shell/framework";
import { UserPermissions } from "./../types";
import useImport, { ExtProfile } from "../composables/useImport";
import { ImportDataPreview, ImportPushNotification, ImportRunHistory } from "../../../api_client/virtocommerce.import";
import ImportPopup from "../components/ImportPopup.vue";
import ImportUploadStatus from "../components/ImportUploadStatus.vue";
import ImportStatus from "../components/ImportStatus.vue";
import { Field } from "vee-validate";
import { useI18n } from "vue-i18n";
import { ImportStat, ImportErrorsCard } from "../components";

import {
  VcBlade,
  VcButton,
  VcCard,
  VcCol,
  VcColumn,
  VcContainer,
  VcDataTable,
  VcFileUpload,
  VcIcon,
  VcInput,
  VcRow,
} from "@vc-shell/framework/ui";

const { callParent, closeSelf, options, param, exposeToChildren } = useBlade();
interface INotificationActions {
  name: string | ComputedRef<string>;
  clickHandler(): void;
  outline: boolean;
  variant?: InstanceType<typeof VcButton>["$props"]["variant"];
  isVisible?: boolean | ComputedRef<boolean>;
  disabled?: boolean | ComputedRef<boolean>;
}

defineBlade({
  name: "ImportNew",
});

const { openBlade } = useBlade();

const { t } = useI18n({ useScope: "global" });
const { hasAccess } = usePermissions();

const {
  loading: importLoading,
  importHistory,
  uploadedFile,
  importStatus,
  isValid,
  profile,
  pagination,
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
const { messages, markAsRead } = useBladeNotifications({
  types: ["ImportPushNotification"],
  onMessage: (message: ImportPushNotification) => {
    const messageContent = message.profileName ? `${message.profileName}: ${message.title}` : message.title;

    if (!importStarted.value && message.profileId === param.value) {
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
      if (message.errorCount && message.errorCount > 0) {
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
  },
});
const fileLoading = ref(false);
const preview = ref<ImportDataPreview>();
const importPreview = ref(false);
const popupColumns = ref<ITableColumns[]>([]);
const popupItems = ref<Record<string, unknown>[]>([]);
const title = computed(() =>
  param.value && profileDetails.value.name ? profileDetails.value.name : (options.value?.title as string | undefined),
);

const cancelled = ref(false);
const notificationId = ref();
const previewLoading = ref(false);
const selectedItemId = ref();
const bladeWidth = ref(70);

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "edit",
    title: computed(() => t("IMPORT.PAGES.PRODUCT_IMPORTER.TOOLBAR.EDIT")),
    icon: "lucide-pencil",
    clickHandler() {
      openBlade({
        name: "ImportProfileDetails",

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
    icon: "lucide-x",
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
    isVisible: computed(() => !!param.value),
  },
  {
    id: "newRun",
    title: computed(() => t("IMPORT.PAGES.PRODUCT_IMPORTER.TOOLBAR.NEW_RUN")),
    icon: "lucide-plus",
    clickHandler() {
      callParent("openImporter", param.value);
    },
    disabled: computed(() => importStatus.value?.inProgress),
    isVisible: computed(() => !!(importStatus.value && profile.value.name)),
  },
]);

async function reRunImport(importJobId?: string) {
  const jobId = options.value?.importJobId || importJobId;
  const historyItem = importHistory.value && importHistory.value.find((x) => x.jobId === jobId);

  if (historyItem?.fileUrl) {
    const correctedProfile = profile?.value;
    correctedProfile.importFileUrl = historyItem.fileUrl;
    correctedProfile.inProgress = false;
    correctedProfile.jobId = undefined;
    await start(correctedProfile);
  }
}

const uploadActions = ref<INotificationActions[]>([
  {
    name: computed(() => t("IMPORT.PAGES.ACTIONS.UPLOADER.ACTIONS.USE_ANOTHER_ONE")),
    clickHandler() {
      clearImport();
      clearErrorMessage();
    },
    outline: true,
    variant: "secondary",
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

const isApiSourceImporter = computed(() => profile.value.importer?.metadata?.sourceType?.toLowerCase() === "api");

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

async function onItemClick(event: { data: ImportRunHistory; index: number; originalEvent: Event }) {
  const item = event.data;
  if (item?.jobId && item.profileId) {
    openBlade({
      name: "ImportProcess",

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
  await init({
    profileId: param.value as string | undefined,
    importJobId: options.value?.importJobId as string | undefined,
  });
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

async function apiPreview() {
  try {
    previewLoading.value = true;
    preview.value = await previewData();
    popupItems.value = [];
    popupColumns.value = [];
    if (preview.value && preview.value.records && preview.value.records.length) {
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
}

async function start(importProfile?: ExtProfile) {
  try {
    clearErrorMessage();
    await startImport(importProfile);
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
  callParent("reload");
  closeSelf();
}

const sampleTemplateUrl = computed(() => {
  return profile.value?.importer?.metadata?.sampleCsvUrl;
});

exposeToChildren({
  reloadParent,
  reRunImport,
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

  &__template-hint {
    @apply tw-flex tw-flex-row tw-items-center tw-gap-2
      tw-rounded tw-bg-[color:var(--secondary-50)]
      tw-border tw-border-solid tw-border-[color:var(--secondary-200)]
      tw-px-4 tw-py-3;
  }

  &__divider {
    @apply tw-flex tw-items-center tw-gap-3;

    &::before,
    &::after {
      content: "";
      @apply tw-flex-1 tw-h-px tw-bg-[color:var(--neutrals-200)];
    }
  }

  &__divider-text {
    @apply tw-text-xs tw-font-medium tw-uppercase tw-tracking-wider
      tw-text-[color:var(--neutrals-400)] tw-whitespace-nowrap;
  }

  &__error {
    --hint-color: var(--color-error);
  }

  &__skipped {
    & .vc-card__body {
      @apply tw-flex tw-flex-col;
    }
  }
}
</style>
