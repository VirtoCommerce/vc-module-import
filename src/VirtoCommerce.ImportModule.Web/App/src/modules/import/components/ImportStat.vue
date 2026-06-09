<template>
  <VcCol v-if="importStarted">
    <!-- Progress bar section -->
    <VcRow
      v-if="inProgress"
      class="tw-relative tw-px-5 tw-pt-5 tw-pb-4 before:tw-content-[''] before:[background:linear-gradient(180deg,var(--import-new-border-color)_0%,rgba(236,242,246,0)_100%)] before:tw-left-0 before:tw-right-0 before:tw-absolute before:h-[21px] before:tw-top-0"
    >
      <VcCol>
        <div class="tw-flex tw-items-center tw-gap-2 tw-mb-3">
          <VcIcon
            icon="lucide-loader-2"
            size="s"
            class="tw-text-[color:var(--import-new-preview-text-color)] tw-animate-spin"
          />
          <span class="tw-text-sm tw-font-medium tw-text-[color:var(--import-new-preview-text-color)]">
            {{ $t("IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.IN_PROGRESS") }}
          </span>
        </div>
        <VcProgress
          :key="importStatus?.progress"
          :value="importStatus?.progress"
          :variant="progressbarVariant"
        />
        <VcHint
          v-if="importStatus?.estimatingRemaining || importStatus?.estimatedRemaining"
          class="tw-pt-3"
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

    <!-- Stats badges -->
    <div class="import-stat__badges tw-border-t tw-border-solid tw-border-t-[color:var(--import-new-border-top-color)]">
      <div
        v-for="(badge, i) in importBadges"
        :key="i"
        class="tw-flex tw-flex-row tw-items-center tw-px-5 tw-py-4 tw-min-w-0"
      >
        <div
          class="import-stat__badge-icon tw-flex tw-items-center tw-justify-center tw-w-10 tw-h-10 tw-rounded-lg tw-shrink-0"
          :style="{ '--badge-color': badge.color }"
        >
          <VcIcon
            :icon="badge.icon"
            size="xl"
            :style="{ color: badge.color }"
          />
        </div>
        <div class="tw-ml-3 tw-min-w-0">
          <div class="tw-text-sm tw-font-semibold tw-leading-tight tw-truncate">
            {{ badge.title }}
          </div>
          <VcHint class="tw-mt-0.5">{{ badge.description }}</VcHint>
        </div>
      </div>
    </div>

    <!-- Error / report links -->
    <VcRow v-if="errorMessage || (reportUrl && reportUrl !== 'DefaultDataReporter')">
      <VcCol class="tw-px-5 tw-pb-4">
        <VcHint
          v-if="errorMessage"
          class="import-new__error"
        >
          {{ errorMessage }}
        </VcHint>
        <div
          v-if="reportUrl && reportUrl !== 'DefaultDataReporter'"
          class="tw-flex tw-items-center tw-gap-2 tw-mt-2"
        >
          <VcIcon
            icon="lucide-download"
            size="s"
            class="tw-text-[color:var(--primary-500)]"
          />
          <VcHint>
            {{ $t("IMPORT.PAGES.LIST.REPORT.DOWNLOAD") }}
            <a
              class="vc-link"
              :href="reportUrl"
            >{{ reportUrl }}</a>
          </VcHint>
        </div>
      </VcCol>
    </VcRow>
  </VcCol>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useImport } from "../composables";
import { formatDateWithPattern, formatDateRelative } from "@vc-shell/framework";
import { useI18n } from "vue-i18n";
import { IImportStatus } from "../composables/useImport";

import { VcCol, VcHint, VcIcon, VcProgress, VcRow } from "@vc-shell/framework/ui";

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

function humanizeDuration(isoDuration: string): string {
  const match = isoDuration.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?/);
  if (!match) return isoDuration;
  const hours = parseInt(match[1] || "0", 10);
  const minutes = parseInt(match[2] || "0", 10);
  const seconds = parseInt(match[3] || "0", 10);
  const parts: string[] = [];
  if (hours > 0) parts.push(`${hours}h`);
  if (minutes > 0) parts.push(`${minutes}m`);
  if (seconds > 0 || parts.length === 0) parts.push(`${seconds}s`);
  return parts.join(" ");
}

const estimatedRemaining = computed(() => {
  return importStatus.value && importStatus.value.estimatedRemaining
    ? humanizeDuration(importStatus.value.estimatedRemaining)
    : null;
});

const reportUrl = computed(() => importStatus.value?.notification?.reportUrl);

const inProgress = computed(() => (importStatus.value && importStatus.value.inProgress) || false);

const importBadges = computed((): IImportBadges[] => {
  const clockTitleTime = (() => {
    if (importStatus.value?.notification?.created) {
      return formatDateWithPattern(importStatus.value.notification.created, "LTS", locale);
    } else if (importStatus.value?.notification?.createdDate) {
      return formatDateWithPattern(importStatus.value.notification.createdDate, "LTS", locale);
    }
    return null;
  })();

  const linesImported = (() => {
    if (
      typeof importStatus.value?.notification?.processedCount !== "undefined" &&
      typeof importStatus.value?.notification?.errorCount !== "undefined"
    ) {
      const value = importStatus.value.notification.processedCount - importStatus.value.notification.errorCount;
      return value >= 0 ? value : 0;
    }
    return 0;
  })();

  const clockDescription = (() => {
    if (importStatus.value?.notification?.created) {
      return formatDateRelative(importStatus.value.notification.created, locale);
    } else if (importStatus.value?.notification?.createdDate) {
      return formatDateRelative(importStatus.value.notification.createdDate, locale);
    }
    return undefined;
  })();

  return [
    {
      id: "clock",
      icon: "lucide-clock",
      color: "var(--import-new-badge-color-info)",
      title: t("IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.STARTED_AT") + " " + clockTitleTime,
      description: clockDescription,
    },
    {
      id: "linesRead",
      icon: "lucide-check-circle",
      color: "var(--import-new-badge-color-success)",
      title: importStatus.value?.notification?.totalCount,
      description: t("IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.LINES_READ"),
    },
    {
      id: "linesImported",
      icon: "lucide-check-circle",
      color: "var(--import-new-badge-color-success)",
      title: linesImported,
      description: t("IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.IMPORTED"),
    },
    {
      id: "skipped",
      icon: "lucide-alert-circle",
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

<style lang="scss">
.import-stat__badges {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
}

.import-stat__badge-icon {
  background-color: color-mix(in srgb, var(--badge-color) 10%, transparent);
}
</style>
