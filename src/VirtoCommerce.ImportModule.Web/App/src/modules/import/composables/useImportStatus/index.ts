import { ref, computed, watch, Ref } from "vue";
import {
  ImportCancellationRequest,
  ImportClient,
  ImportProfile,
  ImportPushNotification,
} from "@virtocommerce/import-app-api";
import { useNotifications, useApiClient, useAsync } from "@vc-shell/framework";
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
  const { notifications } = useNotifications();

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
    [() => notifications, () => importStarted],
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
        const mappedProfiles = importProfiles.value.map((profile) => {
          const notification = newNotifications.value.find(
            (x: ImportPushNotification) => x.profileId === profile.id,
          ) as ImportPushNotification;

          if (notification) {
            profile.inProgress = !notification.finished;
            profile.jobId = notification.jobId;
          }

          return profile;
        });

        setProfiles(mappedProfiles);
      }
    },
    { deep: true, immediate: true },
  );

  const { loading, action: startImport } = useAsync(async (extProfile?: ExtProfile) => {
    const client = await getApiClient();

    const importProfile = new ImportProfile(
      extProfile && Object.keys(extProfile).length > 0 ? extProfile : profile.value,
    );

    const notification = await client.runImport(importProfile);

    importStarted.value = true;
    updateStatus(notification);
  });

  const { action: cancelImport } = useAsync(async () => {
    const client = await getApiClient();

    if (importStatus.value?.inProgress) {
      await client.cancelJob(new ImportCancellationRequest({ jobId: importStatus.value?.jobId }));
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
