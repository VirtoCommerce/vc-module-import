import { ref, computed, watch, Ref } from "vue";
import {
  type ImportCancellationRequest,
  ImportClient,
  type ImportProfile,
  type ImportPushNotification,
} from "../../../../api_client/virtocommerce.import";
import { useBladeNotifications, useApiClient, useAsync } from "@vc-shell/framework";
import { IImportStatus, INotificationHistory, ExtProfile, IUploadedFile } from "../useImport";

const { getApiClient } = useApiClient(ImportClient);

export default function useImportStatus({
  setProfiles,
  setFile,
  importProfiles,
  profile,
}: {
  setProfiles: (extProfile: ExtProfile[]) => void;
  setFile: (file: IUploadedFile) => void;
  importProfiles: Ref<ExtProfile[]>;
  profile: Ref<ExtProfile>;
}) {
  const { messages } = useBladeNotifications({
    types: ["ImportPushNotification"],
  });

  const importStatus = ref<IImportStatus | undefined>({ inProgress: false });
  const importStarted = ref(false);

  function updateStatus(notification: INotificationHistory) {
    const pushNotification = notification as ImportPushNotification & {
      processedCount: number;
      totalCount: number;
      errorsCount: number;
    };
    importStatus.value = {
      notification: { ...notification, errorCount: pushNotification.errorsCount ?? pushNotification.errorCount },
      jobId: notification.jobId,
      inProgress: !notification.finished,
      progress: ((notification.processedCount as number) / (notification.totalCount as number)) * 100 || 0,
      estimatingRemaining: pushNotification.estimatingRemaining,
      estimatedRemaining: pushNotification.estimatedRemaining,
      fileName:
        ("fileUrl" in pushNotification &&
          typeof pushNotification.fileUrl === "string" &&
          pushNotification.fileUrl?.split("/").pop()) ||
        undefined,
    };
  }

  watch(
    [() => messages, () => importStarted],
    ([newNotifications, isStarted]) => {
      if (isStarted.value && importStatus.value) {
        const notification = newNotifications.value.find(
          (x) => x.id === importStatus.value?.notification?.id,
        ) as ImportPushNotification;

        if (notification) {
          updateStatus(notification);
        }
      }

      if (importProfiles.value && importProfiles.value?.length) {
        const mappedProfiles = importProfiles.value.map((item) => {
          const notification = newNotifications.value.find(
            (x) => (x as ImportPushNotification).profileId === item.id,
          ) as ImportPushNotification | undefined;

          if (notification) {
            item.inProgress = !notification.finished;
            item.jobId = notification.jobId;
          }

          return item;
        });

        setProfiles(mappedProfiles);
      }
    },
    { deep: true, immediate: true },
  );

  const { loading, action: startImport } = useAsync(async (extProfile?: ExtProfile) => {
    const client = await getApiClient();

    const importProfile: ImportProfile =
      extProfile && Object.keys(extProfile).length > 0 ? { ...extProfile } : { ...profile.value };

    const notification = await client.runImport(importProfile);

    importStarted.value = true;
    updateStatus(notification);
  });

  const { action: cancelImport } = useAsync(async () => {
    const client = await getApiClient();

    if (importStatus.value?.inProgress) {
      await client.cancelJob({ jobId: importStatus.value?.jobId } as ImportCancellationRequest);
    }
  });

  function clearImport() {
    setFile({
      url: undefined,
      name: undefined,
      size: 0,
    });
    importStatus.value = undefined;
  }

  function setImportStarted(value: boolean) {
    importStarted.value = value;
  }

  return {
    loading,
    importStatus: computed(() => importStatus.value),
    importStarted: computed(() => importStarted.value),
    setImportStarted,
    startImport,
    cancelImport,
    clearImport,
    updateStatus,
  };
}
