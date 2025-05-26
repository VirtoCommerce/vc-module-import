<template>
  <div class="tw-flex tw-flex-row tw-items-center">
    <div
      v-if="isUploaded"
      class="tw-w-[30px] tw-h-[41px] tw-bg-center tw-bg-no-repeat tw-bg-[image:var(--file-upload-success-img)]"
    ></div>
    <div
      v-else
      class="tw-w-[30px] tw-h-[41px] tw-bg-center tw-bg-no-repeat tw-bg-[image:var(--file-upload-error-img)]"
    ></div>
    <div class="tw-flex tw-flex-col tw-grow tw-basis-0 tw-ml-3">
      <div class="tw-font-normal tw-text-lg">
        {{ uploadedFile.name }}
      </div>
      <VcHint v-if="typeof uploadedFile.size === 'number'"> {{ uploadedFile.size }} Mb </VcHint>
    </div>
  </div>
</template>

<script setup lang="ts">
export interface Props {
  uploadedFile: {
    name?: string;
    size?: number | string;
  };
  isUploaded: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  uploadedFile: () => ({}),
  isUploaded: false,
});
</script>
