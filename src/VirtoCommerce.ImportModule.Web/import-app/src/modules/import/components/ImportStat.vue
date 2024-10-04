<template>
  <VcCol v-if="importStarted">
    <VcRow
      v-if="inProgress"
      class="tw-relative tw-p-5 before:tw-content-[''] before:[background:linear-gradient(180deg,var(--import-new-border-color)_0%,rgba(236,242,246,0)_100%)] before:tw-left-0 before:tw-right-0 before:tw-absolute before:h-[21px] before:tw-top-0"
    >
      <VcCol class="tw-text-[var(--import-new-preview-text-color)]">
        {{ $t("IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.IN_PROGRESS") }}
        <VcProgress
          :key="importStatus?.progress"
          class="tw-mt-3"
          :value="importStatus?.progress"
          :variant="progressbarVariant"
        ></VcProgress>
        <VcHint
          v-if="importStatus?.estimatingRemaining || importStatus?.estimatedRemaining"
          class="tw-py-3"
        >
          <template v-if="importStatus?.estimatingRemaining">
            {{ $t("IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.ESTIMATING") }}
          </template>
          <template v-else>
            {{ $t("IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.ESTIMATED") }}
            {{ estimatedRemaining }}
          </template>
        </VcHint>
      </VcCol>
    </VcRow>
    <VcRow class="tw-border-t tw-border-solid tw-border-t-[var(--import-new-border-top-color)]">
      <VcCol
        v-for="(badge, i) in importBadges"
        :key="i"
        class="tw-flex !tw-flex-row tw-items-center tw-p-5"
      >
        <VcIcon
          :icon="badge.icon"
          size="xxl"
          :style="{ color: badge.color }"
        ></VcIcon>
        <div class="tw-ml-3">
          <div class="tw-font-medium">
            {{ badge.title }}
          </div>
          <VcHint>{{ badge.description }}</VcHint>
        </div>
      </VcCol>
    </VcRow>
    <VcRow>
      <VcHint
        v-if="errorMessage"
        class="tw-p-3 import-new__error"
      >
        {{ errorMessage }}
      </VcHint>
      <div v-if="reportUrl && reportUrl != 'DefaultDataReporter'">
        <VcHint class="tw-p-3 import-new__history">
          {{ $t("IMPORT.PAGES.LIST.REPORT.DOWNLOAD") }}
          <a
            class="vc-link"
            :href="reportUrl"
            >{{ reportUrl }}</a
          >
        </VcHint>
      </div>
    </VcRow>
  </VcCol>


</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useImport } from "../composables";
import * as _ from "lodash-es";
import moment from "moment";
import { ITableColumns } from "@vc-shell/framework";
import { useI18n } from "vue-i18n";
import { IImportStatus } from "../composables/useImport";

export interface Props {
  importStatus?: IImportStatus;
}

interface IImportBadges {
  id: string;
  icon: string;
  color: string;
  title?: string | number;
  description?: string;
}

const { errorMessage } = useImport();
const { t } = useI18n({ useScope: "global" });

const props = defineProps<Props>();

const importStatus = computed(() => props.importStatus);

const locale = window.navigator.language;



const progressbarVariant = computed(() => (inProgress.value ? "striped" : "default"));

const importStarted = computed(() => !!(importStatus.value && importStatus.value.jobId));


const estimatedRemaining = computed(() => {
  return importStatus.value && importStatus.value.estimatedRemaining
    ? moment.duration(importStatus.value.estimatedRemaining).locale(locale).humanize(false, "precise")
    : null;
});

const reportUrl = computed(() => importStatus.value?.notification?.reportUrl);

const inProgress = computed(() => (importStatus.value && importStatus.value.inProgress) || false);

const importBadges = computed((): IImportBadges[] => {
  return [
    {
      id: "clock",
      icon: "far fa-clock",
      color: "var(--import-new-badge-color-info)",
      title:
        t("IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.STARTED_AT") +
        " " +
        (importStatus.value?.notification?.created
          ? moment(importStatus.value.notification.created).locale(locale).format("LTS")
          : importStatus.value?.notification?.createdDate
            ? moment(importStatus.value.notification.createdDate).locale(locale).format("LTS")
            : null),
      description: importStatus.value?.notification?.created
        ? moment(importStatus.value.notification.created).locale(locale).fromNow()
        : importStatus.value?.notification?.createdDate
          ? moment(importStatus.value.notification.createdDate).locale(locale).fromNow()
          : undefined,
    },
    {
      id: "linesRead",
      icon: "fas fa-check-circle",
      color: "var(--import-new-badge-color-success)",
      title: importStatus.value?.notification?.totalCount,
      description: t("IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.LINES_READ"),
    },
    {
      id: "linesImported",
      icon: "fas fa-check-circle",
      color: "var(--import-new-badge-color-success)",
      title:
        typeof importStatus.value?.notification?.processedCount !== "undefined" &&
        typeof importStatus.value?.notification?.errorCount !== "undefined"
          ? importStatus.value.notification.processedCount - importStatus.value.notification.errorCount >= 0
            ? importStatus.value.notification.processedCount - importStatus.value.notification.errorCount
            : 0
          : 0,
      description: t("IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.IMPORTED"),
    },
    {
      id: "skipped",
      icon: "fas fa-exclamation-circle",
      color: "var(--import-new-badge-color-warning)",
      title:
        typeof importStatus.value?.notification?.errorCount !== "undefined"
          ? importStatus.value.notification.errorCount
          : 0,
      description: t("IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.SKIPPED"),
    },
  ];
});


</script>
