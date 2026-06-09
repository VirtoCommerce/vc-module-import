<template>
  <!-- Skipped details table -->
  <VcCol
    v-if="importStarted && reversedErrors?.length"
  >
    <VcCard
      class="import-new__skipped"
      :fill="true"
      :variant="skippedColorVariant"
      :header="$t('IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.TABLE.SKIPPED_DETAILS')"
    >
      <VcDataTable
        :header="false"
        :footer="false"
        :items="(reversedErrors ?? []).map((e) => ({ errors: e }))"
        state-key="import_errors"
      >
        <VcColumn
          id="errors"
          :title="$t('IMPORT.PAGES.PRODUCT_IMPORTER.UPLOAD_STATUS.TABLE.ERROR_DESC')"
        >
          <template #body="{ data }">
            <div class="tw-flex tw-flex-col">
              <div class="tw-truncate">
                {{ data.errors }}
              </div>
            </div>
          </template>
        </VcColumn>
      </VcDataTable>
    </VcCard>
  </VcCol>
</template>

<script setup lang="ts">
import { computed } from "vue";
import * as _ from "lodash-es";
import { IImportStatus } from "../composables/useImport";
import { useI18n } from "vue-i18n";
import { VcCard, VcCol, VcColumn, VcDataTable } from "@vc-shell/framework/ui";
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
</script>
