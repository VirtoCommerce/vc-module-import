import { ref, computed, watch, Ref } from "vue";
import {
  IDataImporter,
  ImportClient,
  ImportProfile,
  ObjectSettingEntry,
  SearchImportProfilesCriteria,
} from "../../../../api_client/virtocommerce.import";
import { useApiClient, useAsync, useLoading, useUser } from "@vc-shell/framework";
import * as _ from "lodash-es";
import { ISearchProfile, ExtProfile } from "../useImport";
import { useHelpers } from "../helpers";

const { getApiClient } = useApiClient(ImportClient);

export default function useImportProfiles() {
  const { user } = useUser();
  const { GetSellerId } = useHelpers();

  const profile = ref<ExtProfile>({} as ExtProfile) as Ref<ExtProfile>;
  const profileSearchResult = ref<ISearchProfile>();

  const profileDetails = ref<ImportProfile>({ settings: [{} as ObjectSettingEntry] } as ImportProfile);
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
    async (args?: Omit<SearchImportProfilesCriteria, "userId">) => {
      const client = await getApiClient();
      const importUserId = await GetSellerId();
      const profileQuery: SearchImportProfilesCriteria = { userId: importUserId, ...args };
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
    const command: ImportProfile = {
      ...newProfile,
      userId: importUserId && importUserId != "" ? importUserId : user.value?.id,
      settings: newProfile.settings?.map((setting) => ({ ...setting }) as ObjectSettingEntry),
    };

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

      const command: ImportProfile = {
        ...updatedProfile,
        userId: importUserId && importUserId != "" ? importUserId : user.value?.id,
      };

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
            const entry: ObjectSettingEntry = { ...x } as ObjectSettingEntry;
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
