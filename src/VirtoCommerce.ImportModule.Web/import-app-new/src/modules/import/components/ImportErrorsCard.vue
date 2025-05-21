<template>
  <!-- Skipped details table -->
  <VcCol
    v-if="importStarted && reversedErrors?.length"
    class="tw-p-3"
  >
    <VcCard
      class="import-new__skipped"
      :fill="true"
      :variant="skippedColorVariant"
      :header="$t('IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.TABLE.SKIPPED_DETAILS')"
    >
      <VcTable
        :columns="skippedColumns"
        :header="false"
        :footer="false"
        :items="reversedErrors ?? []"
        state-key="import_errors"
      >
        <!-- Override errors column template -->
        <template #item_errors="itemData">
          <div class="tw-flex tw-flex-col">
            <div class="tw-truncate">
              {{ itemData.item }}
            </div>
          </div>
        </template>
      </VcTable>
    </VcCard>
  </VcCol>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import * as _ from "lodash-es";
import { IImportStatus } from "../composables/useImport";
import { ITableColumns } from "@vc-shell/framework";
import { useI18n } from "vue-i18n";
export interface Props {
  importStatus?: IImportStatus;
}

const props = defineProps<Props>();

const { t } = useI18n({ useScope: "global" });

const importStatus = computed(() => props.importStatus);
const importStarted = computed(() => !!(importStatus.value && importStatus.value.jobId));

const reversedErrors = computed(() => {
  const errors = _.cloneDeep(importStatus.value?.notification?.errors);

  return errors?.reverse();
});

const skippedColorVariant = computed(() => {
  return !(
    importStatus.value &&
    importStatus.value.notification &&
    importStatus.value.notification.errors &&
    importStatus.value.notification.errors.length
  )
    ? "success"
    : "danger";
});

const skippedColumns = ref<ITableColumns[]>([
  {
    id: "errors",
    title: computed(() => t("IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.TABLE.ERROR_DESC")),
  },
]);
</script>
