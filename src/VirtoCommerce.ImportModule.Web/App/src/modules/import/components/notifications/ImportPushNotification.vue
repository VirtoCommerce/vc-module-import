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
import { useBlade, useNotificationContext, NotificationTemplate } from "@vc-shell/framework";
import { ImportPushNotification } from "../../../../api_client/virtocommerce.import";
import { computed } from "vue";

import { VcHint } from "@vc-shell/framework/ui";

const notification = useNotificationContext<ImportPushNotification>();

const {
  openBlade
} = useBlade();

const notificationStyle = computed(() => {
  const n = notification.value;
  if (n.finished && !(n.errors && n.errors.length)) {
    return {
      color: "var(--import-notification-success-color)",
      icon: "lucide-check-circle",
    };
  } else if (!(n.errors && n.errors.length) && !n.finished) {
    return {
      color: "var(--import-notification-info-color)",
      icon: "lucide-info",
    };
  } else {
    return {
      color: "var(--import-notification-error-color)",
      icon: "lucide-alert-circle",
    };
  }
});

async function onClick() {
  if (notification.value.notifyType === "ImportPushNotification") {
    await openBlade({
      name: "ImportProfileSelector",
      param: notification.value.profileId
    });
    await openBlade({
      name: "ImportProcess",
      param: notification.value.profileId,

      options: {
        importJobId: notification.value.jobId,
      }
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
