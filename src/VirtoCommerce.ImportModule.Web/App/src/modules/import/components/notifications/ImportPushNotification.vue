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
import { useBladeNavigation, useNotificationContext, NotificationTemplate } from "@vc-shell/framework";
import { ImportPushNotification } from "@virtocommerce/import-app-api";
import { computed } from "vue";

const notification = useNotificationContext<ImportPushNotification>();

const { openBlade, resolveBladeByName } = useBladeNavigation();

const notificationStyle = computed(() => {
  const n = notification.value;
  if (n.finished && !(n.errors && n.errors.length)) {
    return {
      color: 'var(--import-notification-success-color)',
      icon: "material-check_circle",
    };
  } else if (!(n.errors && n.errors.length) && !n.finished) {
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
  if (notification.value.notifyType === "ImportPushNotification") {
    await openBlade(
      {
        blade: resolveBladeByName("ImportProfileSelector"),
        param: notification.value.profileId,
      },
      true,
    );
    await openBlade({
      blade: resolveBladeByName("ImportProcess"),
      param: notification.value.profileId,
      options: {
        importJobId: notification.value.jobId,
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
