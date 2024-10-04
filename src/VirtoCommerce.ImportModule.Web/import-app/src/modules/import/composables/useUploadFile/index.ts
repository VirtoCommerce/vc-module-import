import { ref, computed, Ref, reactive } from "vue";
import useImportProfiles from "../useImportProfiles";
import { ExtProfile } from "../useImport";
import { ImportProfile } from "@virtocommerce/import-app-api";

export interface IUploadedFile {
  contentType?: string;
  createdDate?: string;
  name?: string;
  relativeUrl?: string;
  size: number | string;
  type?: string;
  url?: string;
}

export default function useUploadedFile({
  profile,
  setProfile,
}: {
  profile: Ref<ExtProfile>;
  setProfile: (extProfile: ExtProfile) => void;
}) {
  const uploadedFile = ref<IUploadedFile>();

  function setFile(file: IUploadedFile) {
    setProfile(
      reactive(
        new ImportProfile({
          ...profile.value,
          importFileUrl: file.url,
        }),
      ),
    );
    uploadedFile.value = file;
  }

  return {
    uploadedFile: computed(() => uploadedFile.value),
    setFile,
  };
}
