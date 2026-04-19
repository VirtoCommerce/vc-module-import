import { computed, ComputedRef, Ref, ref } from "vue";
import {
  ImportClient,
  IDataImporter,
  ImportCancellationRequest,
  ImportDataPreview,
  ImportProfile,
  ImportPushNotification,
  ImportRunHistory,
  ISearchImportRunHistoryCriteria,
  IImportPushNotification,
  ISearchImportProfilesCriteria,
} from "@virtocommerce/import-app-api";

import { useApiClient, useAsync, useLoading, useNotifications } from "@vc-shell/framework";
import * as _ from "lodash-es";
import { useHelpers } from "../helpers";
import useImportProfiles from "../useImportProfiles";
import useImportStatus from "../useImportStatus";
import useUploadedFile from "../useUploadFile";
import useImportHistory from "../useImportHistory";

export type INotificationHistory = IImportPushNotification | ImportRunHistory;

export interface IImportStatus {
  notification?: INotificationHistory & {
    created?: Date;
    createdDate?: Date;
    errorCount?: number;
  };
  jobId?: string;
  inProgress: boolean;
  progress?: number;
  estimatingRemaining?: boolean;
  estimatedRemaining?: string;
  fileName?: string;
}

export interface IUploadedFile {
  contentType?: string;
  createdDate?: string;
  name?: string;
  relativeUrl?: string;
  size: number | string;
  type?: string;
  url?: string;
}

export type ExtProfile = ImportProfile & {
  importer?: IDataImporter;
  inProgress?: boolean;
  jobId?: string;
};

export interface ISearchProfile extends ISearchImportRunHistoryCriteria {
  results?: ExtProfile[];
}

interface IUseImport {
  readonly loading: Ref<boolean>;
  readonly profilesLoading: Ref<boolean>;
  readonly uploadedFile: Ref<IUploadedFile | undefined>;
  readonly importStatus: Ref<IImportStatus | undefined>;
  readonly isValid: Ref<boolean>;
  readonly importHistory: Ref<ImportRunHistory[] | undefined>;
  readonly dataImporters: Ref<IDataImporter[]>;
  readonly importProfiles: ComputedRef<ExtProfile[] | undefined>;
  readonly profile: Ref<ExtProfile>;
  readonly modified: Ref<boolean>;
  readonly totalHistoryCount: Ref<number | undefined>;
  readonly historyPages: Ref<number>;
  readonly currentPage: Ref<number>;
  readonly importHistoryLoading: Ref<boolean>;
  readonly dataImportersLoading: Ref<boolean>;
  readonly previewDataLoading: Ref<boolean>;
  profileDetails: Ref<ImportProfile>;
  readonly updateImportProfileLoading: ComputedRef<boolean>;
  readonly errorMessage: Ref<string>;
  setFile(file: IUploadedFile): void;
  setImporter(typeName: string): void;
  fetchDataImporters(): Promise<IDataImporter[]>;
  previewData(): Promise<ImportDataPreview>;
  startImport(extProfile?: ExtProfile): Promise<void>;
  cancelImport(): Promise<void>;
  clearImport(): void;
  fetchImportHistory(query?: ISearchImportRunHistoryCriteria): Promise<void>;
  fetchImportProfiles(args?: ISearchImportProfilesCriteria): Promise<void>;
  loadImportProfile(args: { id: string }): Promise<void>;
  createImportProfile(details: ImportProfile): Promise<void>;
  updateImportProfile(details: ImportProfile): Promise<void>;
  deleteImportProfile(args: { id: string }): Promise<void>;
  updateStatus(notification: ImportPushNotification | ImportRunHistory): void;
  getLongRunning(args: { id: string }): void;
  setErrorMessage(message: string): void;
  clearErrorMessage(): void;
  init(args: { profileId?: string; importJobId?: string }): Promise<void>;
  getTasks(args: { profileId?: string; importJobId?: string }): void;
  resume(jobId: string): Promise<ImportPushNotification | undefined>;
}

const { getApiClient } = useApiClient(ImportClient);

export default (): IUseImport => {
  const { notifications } = useNotifications();
  const { GetSellerId } = useHelpers();

  const {
    profile,
    profileDetails,
    dataImporters,
    importProfiles,
    dataImportersLoading,
    modified,
    updateImportProfileLoading,
    loadImportProfile,
    createImportProfile,
    updateImportProfile,
    deleteImportProfile,
    fetchDataImporters,
    setImporter,
    fetchImportProfiles,
    setProfiles,
    setProfile,
    loading: profilesLoading,
  } = useImportProfiles();
  const {
    importHistory,
    totalHistoryCount,
    historyPages,
    currentPage,
    fetchImportHistory,
    loading: importHistoryLoading,
  } = useImportHistory();
  const { uploadedFile, setFile } = useUploadedFile({
    setProfile,
    profile
  });
  const { importStatus, updateStatus, setImportStarted, startImport, clearImport } = useImportStatus({
    importProfiles,
    profile,
    setProfiles,
    setFile,
  });
  const errorMessage = ref("");
  const { loading: previewDataLoading, action: previewData } = useAsync(async () => {
    const importUserId = await GetSellerId();
    const client = await getApiClient();

    try {
      const previewDataQuery = new ImportProfile({
        ...profile.value,
        userId: importUserId,
      });
      return client.preview(previewDataQuery);
    } catch (e) {
      console.error(e);
      throw e;
    }
  });

  const { action: cancelImport } = useAsync(async () => {
    const client = await getApiClient();
    try {
      if (importStatus.value?.inProgress) {
        await client.cancelJob(new ImportCancellationRequest({ jobId: importStatus.value?.jobId }));
      }
    } catch (e) {
      console.error(e);
      throw e;
    }
  });

  async function resume(jobId: string): Promise<ImportPushNotification | undefined> {
    // TODO: replace this hand-rolled bypass with `client.resumeImport(jobId)` once the
    // `@virtocommerce/import-app-api` package is regenerated to include the resume endpoint.
    // Current code reaches through three internal NSwag properties (transformOptions,
    // http.fetch, baseUrl) to preserve Bearer-token injection; a regenerated typed client
    // removes the need for any cast.
    try {
      const client = await getApiClient();
      const internals = client as unknown as Partial<{
        http: { fetch: (url: string, init: RequestInit) => Promise<Response> };
        baseUrl: string;
        transformOptions: (options: RequestInit) => Promise<RequestInit>;
      }>;
      if (typeof internals.transformOptions !== "function" || !internals.http || typeof internals.http.fetch !== "function") {
        throw new Error("Generated ImportClient shape has changed — regenerate @virtocommerce/import-app-api to add resumeImport()");
      }
      const options = await internals.transformOptions({
        method: "POST",
        headers: { "Content-Type": "application/json" },
      });
      const response = await internals.http.fetch(
        `${internals.baseUrl ?? ""}/api/import/runs/${encodeURIComponent(jobId)}/resume`,
        options,
      );
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }
      const data = await response.json();
      return ImportPushNotification.fromJS(data);
    } catch (e) {
      console.error("Import resume failed:", e);
      return undefined;
    }
  }

  function getLongRunning(args?: { id: string }) {
    const job = notifications.value.find(
      (x) => (x as ImportPushNotification).profileId === args?.id,
    ) as ImportPushNotification | undefined;

    if (job && !job.finished) {
      updateStatus(job);
      setImportStarted(true);
    }
  }

  function clearErrorMessage() {
    errorMessage.value = "";
  }

  function setErrorMessage(message: string) {
    errorMessage.value = message;
  }

  async function init(args: { profileId?: string; importJobId?: string }) {
    if (args.profileId) {
      await fetchDataImporters();
      await loadImportProfile({ id: args.profileId });
      await fetchImportHistory({
        profileId: args.profileId,
        jobId: args.importJobId,
      });
    }

    getTasks(args);
  }

  function getTasks(args: { profileId?: string; importJobId?: string }) {
    if (args.profileId && args.importJobId) {
      const historyItem = importHistory.value && importHistory.value.find((x) => x.jobId === args.importJobId);

      getLongRunning({ id: args.profileId });

      if (!(importStatus.value && importStatus.value.jobId) && historyItem) {
        updateStatus(historyItem);
      }
    }
  }

  return {
    loading: useLoading(profilesLoading),
    importHistoryLoading: useLoading(importHistoryLoading),
    dataImportersLoading: useLoading(dataImportersLoading),
    previewDataLoading: useLoading(previewDataLoading),
    profilesLoading: computed(() => profilesLoading.value),
    uploadedFile: computed(() => uploadedFile.value),
    importStatus: computed(() => importStatus.value),
    isValid: computed(() => {
      if (!profile.value.importer) return false;
      const isApiSource = profile.value.importer.metadata?.sourceType?.toLowerCase() === "api";
      return isApiSource || !!uploadedFile.value;
    }),
    importHistory,
    importProfiles,
    dataImporters: computed(() => dataImporters.value),
    modified: computed(() => modified.value),
    profile: computed(() => profile.value),
    totalHistoryCount,
    historyPages,
    currentPage,
    profileDetails,
    errorMessage: computed(() => errorMessage.value),
    updateImportProfileLoading,
    setFile,
    fetchDataImporters,
    previewData,
    startImport,
    cancelImport,
    clearImport,
    loadImportProfile,
    fetchImportHistory,
    fetchImportProfiles,
    createImportProfile,
    updateImportProfile,
    deleteImportProfile,
    setImporter,
    updateStatus,
    getLongRunning,
    setErrorMessage,
    clearErrorMessage,
    init,
    getTasks,
    resume,
  };
};
