import { ref, computed, watch, Ref } from "vue";
import {
  IDataImporter,
  ImportClient,
  ImportProfile,
  ISearchImportProfilesCriteria,
  SearchImportProfilesCriteria,
} from "@virtocommerce/import-app-api";
import {
  IObjectSettingEntry,
  ObjectSettingEntry,
  useApiClient,
  useAsync,
  useLoading,
  useUser,
} from "@vc-shell/framework";
import * as _ from "lodash-es";
import { ISearchProfile, ExtProfile } from "../useImport";
import { useHelpers } from "../helpers";

const { getApiClient } = useApiClient(ImportClient);

export default function useImportProfiles() {
  const { user } = useUser();
  const { GetSellerId } = useHelpers();

  const profile = ref<ExtProfile>(new ImportProfile() as ExtProfile) as Ref<ExtProfile>;
  const profileSearchResult = ref<ISearchProfile>();

  const profileDetails = ref<ImportProfile>(new ImportProfile({ settings: [new ObjectSettingEntry()] }));
  let profileDetailsCopy: ImportProfile;
  const dataImporters = ref<IDataImporter[]>([]);
  const modified = ref(false);
  const importProfiles = ref<ExtProfile[]>([]);

  const { loading: dataImportersLoading, action: fetchDataImporters } = useAsync(async () => {
    const client = await getApiClient();

    dataImporters.value = await client.getImporters();

    return dataImporters.value;
  });

  const { loading: profilesLoading, action: fetchImportProfiles } = useAsync(
    async (args?: Omit<ISearchImportProfilesCriteria, "userId">) => {
      const client = await getApiClient();
      const importUserId = await GetSellerId();
      const profileQuery = new SearchImportProfilesCriteria({ userId: importUserId, ...args });
      profileSearchResult.value = await client.searchImportProfiles(profileQuery);

      importProfiles.value = profileSearchResult.value?.results || [];
    },
  );

  const { loading: profileLoading, action: loadImportProfile } = useAsync(async (args?: { id: string }) => {
    if (!args?.id) {
      return;
    }
    const client = await getApiClient();
    profile.value = await client.getImportProfileById(args.id);
    profile.value.importer = dataImporters.value.find((x) => x.typeName === profile.value.dataImporterType);
    Object.assign(profileDetails.value, profile.value);
    profileDetailsCopy = _.cloneDeep(profileDetails.value);
  });

  const { action: createImportProfile } = useAsync(async (newProfile?: ImportProfile) => {
    if (!newProfile) {
      return;
    }
    const importUserId = await GetSellerId();
    const client = await getApiClient();

    newProfile.userName = user.value?.userName;
    newProfile.userId = importUserId && importUserId != "" ? importUserId : user.value?.id;
    const command = new ImportProfile({
      ...newProfile,
      userId: importUserId && importUserId != "" ? importUserId : user.value?.id,
      settings: newProfile.settings?.map((setting) => new ObjectSettingEntry(setting)),
    });

    const newProfileWithId = await client.createImportProfile(command);
    await loadImportProfile({ id: newProfileWithId.id as string });
  });

  const { loading: updateImportProfileLoading, action: updateImportProfile } = useAsync(
    async (updatedProfile?: ImportProfile) => {
      if (!updatedProfile) {
        return;
      }
      const importUserId = await GetSellerId();
      const client = await getApiClient();

      const command = new ImportProfile({
        ...updatedProfile,
        userId: importUserId && importUserId != "" ? importUserId : user.value?.id,
      });

      await client.updateImportProfile(command);
      await loadImportProfile({ id: updatedProfile.id as string });
    },
  );

  const { action: deleteImportProfile } = useAsync(async (args?: { id: string }) => {
    if (!args?.id) {
      return;
    }
    const client = await getApiClient();
    await client.deleteProfile(args.id);
  });

  function setImporter(typeName: string) {
    if (typeName) {
      const importer = dataImporters.value.find((item) => item.typeName === typeName);
      if (importer) {
        profileDetails.value.settings = [
          ...(importer?.availSettings?.map((x) => {
            const entry = new ObjectSettingEntry(x as unknown as IObjectSettingEntry);
            if (entry.defaultValue) {
              entry.value = entry.defaultValue;
            }
            return entry;
          }) || []),
        ];
      }
    }
  }

  function setProfiles(extProfiles: ExtProfile[]) {
    importProfiles.value = extProfiles;
  }

  function setProfile(extProfile: ExtProfile) {
    profile.value = extProfile;
  }

  watch(
    () => profileDetails,
    (state) => {
      modified.value = !_.isEqual(profileDetailsCopy, state.value);
    },
    { deep: true },
  );

  return {
    loading: useLoading(profilesLoading, profileLoading),
    importProfiles: computed(() => importProfiles.value),
    dataImporters: computed(() => dataImporters.value),
    dataImportersLoading,
    modified: computed(() => modified.value),
    updateImportProfileLoading: computed(() => updateImportProfileLoading.value),
    profile,
    profileDetails,
    fetchDataImporters,
    fetchImportProfiles,
    loadImportProfile,
    createImportProfile,
    updateImportProfile,
    deleteImportProfile,
    setImporter,
    setProfiles,
    setProfile,
  };
}
