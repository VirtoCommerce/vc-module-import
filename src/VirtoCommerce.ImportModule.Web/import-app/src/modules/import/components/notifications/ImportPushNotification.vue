<template>
  <NotificationTemplate
    :color="notificationStyle.color"
    :title="notification.title ?? ''"
    :icon="notificationStyle.icon"
    :notification="notification"
    @click="onClick"
  >
    <VcHint
      v-if="notification.profileName"
      class="tw-mb-1"
      >{{ $t("IMPORT.PUSH.PROFILE") }} <b>{{ notification.profileName }}</b></VcHint
    >
    <div v-if="notification.errors && notification.errors.length">
      <VcHint> {{ $t("IMPORT.PUSH.ERRORS") }}: {{ notification.errors && notification.errors.length }}</VcHint>
    </div>
  </NotificationTemplate>
</template>

<script lang="ts" setup>
import { useBladeNavigation, NotificationTemplate } from "@vc-shell/framework";
import { ImportPushNotification } from "@virtocommerce/import-app-api";
import { computed } from "vue";

export interface Props {
  notification: ImportPushNotification;
}

export interface Emits {
  (event: "notificationClick"): void;
}

const props = defineProps<Props>();

const emit = defineEmits<Emits>();

defineOptions({
  inheritAttrs: false,
  notifyType: "ImportPushNotification",
});

const { openBlade, resolveBladeByName } = useBladeNavigation();

const notificationStyle = computed(() => {
  const notification = props.notification;
  if (notification.finished && !(notification.errors && notification.errors.length)) {
    return {
      color: 'var(--import-notification-success-color)',
      icon: "material-check_circle",
    };
  } else if (!(notification.errors && notification.errors.length) && !notification.finished) {
    return {
      color: 'var(--import-notification-info-color)',
      icon: "material-info",
    };
  } else {
    return {
      color: 'var(--import-notification-error-color)',
      icon: "material-error",
    };
  }
});

async function onClick() {
  if (props.notification.notifyType === "ImportPushNotification") {
    emit("notificationClick");
    await openBlade(
      {
        blade: resolveBladeByName("ImportProfileSelector"),
        param: props.notification.profileId,
      },
      true,
    );
    await openBlade({
      blade: resolveBladeByName("ImportProcess"),
      param: props.notification.profileId,
      options: {
        importJobId: props.notification.jobId,
      },
    });
  }
}
</script>

<style lang="scss">
:root {
  --import-notification-success-color: var(--success-500);
  --import-notification-info-color: var(--secondary-500);
  --import-notification-error-color: var(--danger-500);
}
</style>
