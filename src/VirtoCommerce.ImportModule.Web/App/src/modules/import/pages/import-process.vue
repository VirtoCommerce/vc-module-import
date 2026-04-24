<template>
  <VcBlade
    :loading="bladeLoading"
    :title="title"
    width="50%"
    :toolbar-items="bladeToolbar"
  >
    <VcContainer class="import-process">
      <VcCol
        v-if="!bladeLoading"
        class="tw-flex tw-gap-3"
      >
        <VcCard :header="$t('IMPORT.PAGES.PRODUCT_IMPORTER.FILE_UPLOAD.IMPORT_RESULTS')">
          <ImportUploadedFile
            v-if="!importStatus?.inProgress"
            class="tw-m-5"
            :uploaded-file="{
              ...uploadedFile,
              name: importStatus?.fileName,
            }"
            is-uploaded
          />
          <ImportStat :import-status="importStatus" />
        </VcCard>
        <ImportErrorsCard :import-status="importStatus" />
      </VcCol>
    </VcContainer>
  </VcBlade>
</template>

<script lang="ts" setup>
import { computed, onMounted, ref, watch } from "vue";
import * as _ from "lodash-es";
import { IBladeToolbar, useBladeNotifications, notification, useBlade } from "@vc-shell/framework";
import useImport from "../composables/useImport";
import { useI18n } from "vue-i18n";
import { ImportStat, ImportErrorsCard, ImportUploadedFile } from "../components";
import { ImportPushNotification } from "../../../api_client/virtocommerce.import";

import { VcBlade, VcCard, VcCol, VcContainer } from "@vc-shell/framework/ui";

const { callParent, closeSelf, options, param, exposeToChildren } = useBlade();
defineBlade({
  name: "ImportProcess",
});

const { t } = useI18n({ useScope: "global" });

const {
  loading: importLoading,
  importHistory,
  importStatus,
  profile,
  dataImportersLoading,
  profilesLoading,
  importHistoryLoading,
  uploadedFile,
  init,
  getTasks,
} = useImport();

const { markAsRead } = useBladeNotifications({
  types: ["ImportPushNotification"],
  onMessage: (message: ImportPushNotification) => {
    console.log(message);
    const messageContent = message.profileName ? `${message.profileName} ${message.title}` : message.title;

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

const notificationId = ref();

const importStarted = computed(() => !!(importStatus.value && importStatus.value.jobId));

const title = computed(() =>
  param.value && profileDetails.value.name
    ? profileDetails.value.name + t("IMPORT.PAGES.IMPORT_PROCESS.TITLE_SUFFIX")
    : ((options.value?.title as string) ?? "") + t("IMPORT.PAGES.IMPORT_PROCESS.TITLE_SUFFIX"),
);

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "reRun",
    title: computed(() => t("IMPORT.PAGES.PRODUCT_IMPORTER.TOOLBAR.RE_RUN")),
    icon: "lucide-refresh-cw",
    async clickHandler() {
      callParent("reRunImport", options.value?.importJobId);
      closeSelf();
    },
    disabled: computed(() => {
      const historyItem =
        importHistory.value && importHistory.value.find((x) => x.jobId === options.value?.importJobId);
      return !(historyItem?.finished && historyItem.fileUrl != null);
    }),
    isVisible: computed(() => !!(importStatus.value && profile.value.name)),
  },
]);

const bladeLoading = computed(
  () => importLoading.value || dataImportersLoading.value || profilesLoading.value || importHistoryLoading.value,
);

const profileDetails = computed(() => profile.value);

onMounted(async () => {
  await init({
    profileId: param.value as string | undefined,
    importJobId: options.value?.importJobId as string | undefined,
  });
});
</script>

<style lang="scss">
:root {
  --color-error: var(--base-error-color);
  --import-process-description-color: var(--neutrals-800);
  --import-process-border-color: var(--secondary-200);
  --import-process-preview-text-color: var(--primary-500);
  --import-process-icon-color: var(--secondary-500);
  --import-process-badge-color-success: var(--success-500);
  --import-process-badge-color-info: var(--secondary-500);
  --import-process-badge-color-warning: var(--warning-500);
  --import-process-badge-color-error: var(--danger-500);
  --import-process-progress-text-color: var(--secondary-500);
  --import-process-border-top-color: var(--neutrals-200);
}

.import-process {
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
