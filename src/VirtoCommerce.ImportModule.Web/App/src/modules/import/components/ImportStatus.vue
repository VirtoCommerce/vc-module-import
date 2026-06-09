<template>
  <VcStatus v-bind="statusStyles">
    {{ $t(`IMPORT.PAGES.LIST.TABLE.STATUSES.${camelToSnake(statusText).toUpperCase()}`) }}</VcStatus
  >
</template>

<script lang="ts" setup>
import { computed } from "vue";
import { ImportRunHistory } from "../../../api_client/virtocommerce.import";
import { camelToSnake } from "@vc-shell/framework";

import { VcStatus } from "@vc-shell/framework/ui";

export interface Props {
  item: ImportRunHistory;
}

const props = withDefaults(defineProps<Props>(), {
  item: undefined,
});

const statusStyles = computed(
  (): {
    outline: boolean;
    variant: "warning" | "danger" | "success";
  } => {
    if (props.item.finished) {
      if (props.item.errorsCount && props.item.processedCount && props.item.errorsCount >= props.item.processedCount) {
        return {
          outline: false,
          variant: "danger",
        };
      } else if (
        props.item.errorsCount &&
        props.item.processedCount &&
        props.item.errorsCount < props.item.processedCount &&
        props.item.errorsCount > 0
      ) {
        return {
          outline: false,
          variant: "warning",
        };
      } else if (props.item.errorsCount === 0) {
        return {
          outline: false,
          variant: "success",
        };
      }
    }
    return {
      outline: true,
      variant: "warning",
    };
  },
);

const statusText = computed(() => {
  if (props.item.finished) {
    if (props.item.errorsCount && props.item.processedCount && props.item.errorsCount >= props.item.processedCount) {
      return "Failed";
    } else if (
      props.item.errorsCount &&
      props.item.processedCount &&
      props.item.errorsCount < props.item.processedCount &&
      props.item.errorsCount > 0
    ) {
      return "CompletedWithErrors";
    } else if (props.item.errorsCount === 0) {
      return "Completed";
    }
  }
  return "Cancelled";
});
</script>
