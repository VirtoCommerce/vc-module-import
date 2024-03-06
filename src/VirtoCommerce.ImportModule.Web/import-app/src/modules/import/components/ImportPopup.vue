<template>
  <VcPopup
    :title="$t('IMPORT.PAGES.IMPORTING.POPUP.TITLE')"
    is-fullscreen
    is-mobile-fullscreen
    modal-width="tw-w-screen"
    @close="$emit('close')"
  >
    <template #content>
      <div class="tw-flex tw-flex-col">
        <div class="tw-flex tw-flex-row tw-justify-between">
          <div class="tw-p-5 tw-flex tw-items-center">
            <p class="tw-m-0 tw-text-[color:var(--basic-black-color)] tw-leading-lg">
              {{ $t("IMPORT.PAGES.IMPORTING.POPUP.DESCRIPTION") }}
            </p>
          </div>
          <div class="tw-p-5 tw-flex tw-items-center tw-border-l tw-border-solid tw-border-l-[#e3e7ec]">
            <p class="tw-text-lg tw-leading-xl tw-text-[color:var(--basic-black-color)] tw-m-0">
              {{ $t("IMPORT.PAGES.IMPORTING.POPUP.PREVIEW_COUNT") }}:
              <span class="tw-text-[color:var(--primary-color)]">{{ items.length }}</span>
              {{ $t("IMPORT.PAGES.IMPORTING.POPUP.PREVIEW_OF") }}
              <span class="tw-text-[color:var(--primary-color)]">{{ total }}</span>
            </p>
          </div>
        </div>
        <VcTable
          :columns="columns"
          :items="items"
          :header="false"
          :scrolling="true"
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
