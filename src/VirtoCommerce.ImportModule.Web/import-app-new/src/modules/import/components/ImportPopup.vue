<template>
  <VcPopup
    :title="$t('IMPORT.PAGES.IMPORTING.POPUP.TITLE')"
    is-fullscreen
    is-mobile-fullscreen
    modal-width="tw-w-screen"
    class="import-popup"
    @close="$emit('close')"
  >
    <template #content>
      <div class="tw-flex tw-flex-col">
        <div class="tw-flex tw-flex-row tw-justify-between">
          <div class="tw-p-5 tw-flex tw-items-center">
            <p class="tw-m-0 tw-text-[color:var(--import-popup-description-color)] tw-leading-lg">
              {{ $t("IMPORT.PAGES.IMPORTING.POPUP.DESCRIPTION") }}
            </p>
          </div>
          <div
            class="tw-p-5 tw-flex tw-items-center tw-border-l tw-border-solid tw-border-l-[var(--import-popup-border-color)]"
          >
            <p class="tw-text-lg tw-leading-xl tw-text-[color:var(--import-popup-description-color)] tw-m-0">
              {{ $t("IMPORT.PAGES.IMPORTING.POPUP.PREVIEW_COUNT") }}:
              <span class="tw-text-[color:var(--import-popup-preview-text-color)]">{{ items.length }}</span>
              {{ $t("IMPORT.PAGES.IMPORTING.POPUP.PREVIEW_OF") }}
              <span class="tw-text-[color:var(--import-popup-preview-text-color)]">{{ total }}</span>
            </p>
          </div>
        </div>
        <VcTable
          class="tw-flex-auto"
          :columns="columns"
          :items="items"
          :header="false"
          :footer="false"
          state-key="import_popup"
        ></VcTable>
      </div>
    </template>
    <template #footer="{ close }">
      <div class="tw-p-5 tw-flex tw-flex-auto tw-justify-between">
        <VcButton
          :outline="true"
          @click="close"
          >{{ $t("IMPORT.PAGES.IMPORTING.POPUP.CANCEL") }}</VcButton
        >
        <VcButton
          :disabled="disabled"
          @click="$emit('startImport')"
          >{{ $t("IMPORT.PAGES.IMPORTING.POPUP.IMPORT") }}</VcButton
        >
      </div>
    </template>
  </VcPopup>
</template>

<script lang="ts" setup>
import { VcTable, VcButton, VcPopup, ITableColumns } from "@vc-shell/framework";

export interface Props {
  columns: ITableColumns[];
  items: Record<string, unknown>[];
  total: number;
  disabled: boolean;
}

interface Emits {
  (event: "close"): void;
  (event: "startImport"): void;
}

withDefaults(defineProps<Props>(), {
  columns: () => [],
  items: () => [],
  total: 0,
  disabled: false,
});

defineEmits<Emits>();
</script>

<style lang="scss">
:root {
  --import-popup-description-color: var(--neutrals-800);
  --import-popup-preview-text-color: var(--primary-500);
  --import-popup-border-color: var(--secondary-200);
}

.import-popup {
  .vc-popup__content-wrapper {
    @apply tw-w-full tw-flex-col;
  }
}
</style>
