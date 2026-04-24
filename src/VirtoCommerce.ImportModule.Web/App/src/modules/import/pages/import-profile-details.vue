<template>
  <VcBlade
    :loading="bladeLoading"
    :title="param && profileDetails ? profileDetails.name : $t('IMPORT.PAGES.PROFILE_DETAILS.TITLE')"
    width="50%"
    :toolbar-items="bladeToolbar"
  >
    <VcContainer class="import-profile-details">
      <VcRow>
        <VcCol>
          <Field
            v-slot="{ field, errorMessage, handleChange, errors }"
            :label="$t('IMPORT.PAGES.PROFILE_DETAILS.IMPORT_INPUTS.PROFILE_NAME.TITLE')"
            rules="required"
            name="profile_name"
            :model-value="profileDetails.name"
          >
            <VcInput
              v-bind="field"
              v-model="profileDetails.name"
              class="tw-p-3"
              :label="$t('IMPORT.PAGES.PROFILE_DETAILS.IMPORT_INPUTS.PROFILE_NAME.TITLE')"
              :placeholder="$t('IMPORT.PAGES.PROFILE_DETAILS.IMPORT_INPUTS.PROFILE_NAME.PLACEHOLDER')"
              :clearable="true"
              :tooltip="$t('IMPORT.PAGES.PROFILE_DETAILS.IMPORT_INPUTS.PROFILE_NAME.TOOLTIP')"
              :error="!!errors.length"
              :error-message="errorMessage"
              required
              @update:model-value="handleChange"
            ></VcInput>
          </Field>
          <Field
            v-slot="{ field, handleChange }"
            :label="$t('IMPORT.PAGES.PROFILE_DETAILS.IMPORT_INPUTS.IMPORTER.TITLE')"
            rules="required"
            name="importer"
            :model-value="profileDetails.dataImporterType"
          >
            <VcSelect
              v-bind="field"
              v-model="profileDetails.dataImporterType"
              class="tw-p-3"
              :label="$t('IMPORT.PAGES.PROFILE_DETAILS.IMPORT_INPUTS.IMPORTER.TITLE')"
              :tooltip="$t('IMPORT.PAGES.PROFILE_DETAILS.IMPORT_INPUTS.IMPORTER.TOOLTIP')"
              :options="dataImporters"
              option-value="typeName"
              option-label="typeName"
              required
              searchable
              :clearable="false"
              @update:model-value="
                (e) => {
                  handleChange(e);
                  setImporter(e as string);
                }
              "
            ></VcSelect>
          </Field>
        </VcCol>
      </VcRow>
      <VcRow
        v-if="profileDetails.dataImporterType"
        class="tw-p-3"
      >
        <VcCard :header="$t('IMPORT.PAGES.PROFILE_DETAILS.PROFILE_SETTINGS.TITLE')">
          <VcRow>
            <VcCol>
              <div
                v-if="sampleTemplateUrl"
                class="tw-p-4"
              >
                <a
                  class="vc-link"
                  :href="sampleTemplateUrl"
                  >{{ $t("IMPORT.PAGES.TEMPLATE.DOWNLOAD_TEMPLATE") }}</a
                >
                {{ $t("IMPORT.PAGES.TEMPLATE.FOR_REFERENCE") }}
              </div>

              <VcDynamicProperty
                v-for="(setting, i) in profileDetails.settings"
                :key="`${setting.name}_${i}`"
                class="tw-px-4 tw-pb-4"
                :property="setting"
                :dictionary="setting.isDictionary"
                :disabled="setting.isReadOnly"
                :name="setting.name ?? ''"
                :options-getter="loadDictionaries"
                :model-value="setting.value"
                :required="setting.isRequired ?? false"
                :value-type="setting.valueType ?? ''"
                :placeholder="setting.defaultValue"
                @update:model-value="(args) => setSettingsValue({ property: setting, ...args })"
              >
              </VcDynamicProperty>
            </VcCol>
          </VcRow>
        </VcCard>
      </VcRow>
    </VcContainer>
  </VcBlade>
</template>

<script lang="ts" setup>
import { computed, onMounted, ref } from "vue";
import { IBladeToolbar, usePopup, useBladeForm, useBlade } from "@vc-shell/framework";
import useImport from "../composables/useImport";
import { IDataImporter, ObjectSettingEntry } from "../../../api_client/virtocommerce.import";
import { Field } from "vee-validate";
import { useI18n } from "vue-i18n";

import {
  VcBlade,
  VcCard,
  VcCol,
  VcContainer,
  VcDynamicProperty,
  VcInput,
  VcRow,
  VcSelect,
} from "@vc-shell/framework/ui";

const { callParent, closeSelf, options, param, exposeToChildren } = useBlade();
defineBlade({
  url: "/import-profile-details",
  name: "ImportProfileDetails",
  routable: false,
});

const { showConfirmation } = usePopup();
const { t } = useI18n({ useScope: "global" });
const {
  dataImporters,
  profileDetails,
  loading,
  profile,
  modified,
  updateImportProfileLoading,
  dataImportersLoading,
  createImportProfile,
  loadImportProfile,
  deleteImportProfile,
  updateImportProfile,
  fetchDataImporters,
  setImporter,
} = useImport();

const { canSave, isModified, setBaseline, setFieldError, errorBag } = useBladeForm({
  data: profileDetails,
  closeConfirmMessage: () => t("IMPORT.PAGES.PROFILE_DETAILS.ALERTS.CLOSE_CONFIRMATION"),
});

const bladeLoading = computed(() => loading.value || updateImportProfileLoading.value || dataImportersLoading.value);

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "save",
    title: computed(() => t("IMPORT.PAGES.PROFILE_DETAILS.TOOLBAR.SAVE")),
    icon: "lucide-save",
    async clickHandler() {
      if (canSave.value) {
        if (param.value) {
          await updateImportProfile(profileDetails.value);
          setBaseline();
          callParent("reloadParent");
        } else {
          await createImportProfile(profileDetails.value);
          setBaseline();
          callParent("reload");
        }
        closeSelf();
      }
    },
    disabled: computed(() => !canSave.value),
  },
  {
    id: "cancel",
    title: computed(() => t("IMPORT.PAGES.PROFILE_DETAILS.TOOLBAR.CANCEL")),
    icon: "lucide-x",
    clickHandler() {
      closeSelf();
    },
    isVisible: computed(() => !param.value),
  },
  {
    id: "delete",
    title: computed(() => t("IMPORT.PAGES.PROFILE_DETAILS.TOOLBAR.DELETE")),
    icon: "lucide-trash-2",
    isVisible: computed(() => !!param.value),
    async clickHandler() {
      if (
        await showConfirmation(
          computed(() => t("IMPORT.PAGES.PROFILE_DETAILS.CONFIRM_POPUP.DELETE_IMPORTER.DESCRIPTION")),
        )
      ) {
        deleteProfile();
      }
    },
  },
]);

const sampleTemplateUrl = computed(() => {
  const importer = dataImporters.value.find((x) => x.typeName === profileDetails.value.dataImporterType);

  if (profile.value.importer) {
    return profile.value.importer.metadata?.sampleCsvUrl;
  } else if (importer) {
    return importer.metadata?.sampleCsvUrl;
  }

  return undefined;
});

const title = computed(() =>
  options.value?.importer
    ? (options.value.importer as IDataImporter).typeName
    : t("IMPORT.PAGES.PROFILE_DETAILS.TITLE"),
);

onMounted(async () => {
  await fetchDataImporters();
  if (param.value) {
    await loadImportProfile({ id: param.value });
  }
  setBaseline();
});

function setSettingsValue(data: { property: ObjectSettingEntry; value: string | boolean }) {
  const { property, value } = data;

  const mutatedSetting: ObjectSettingEntry = { ...property, value };

  profileDetails.value.settings?.forEach((x) => {
    if ((x.id && property.id && x.id === property.id) || x.name === property.name) {
      Object.assign(x, mutatedSetting);
    }
  });
}

function loadDictionaries(settingId: string) {
  const setting = profileDetails.value.settings?.find((x) => x.id === settingId);
  if (setting?.allowedValues && setting?.allowedValues.length) {
    return setting.allowedValues.map((val) => ({
      id: val,
      alias: val,
    }));
  }
}

async function deleteProfile() {
  if (param.value) {
    await deleteImportProfile({ id: param.value });

    callParent("reloadParent");
    closeSelf();
  }
}
</script>

<style lang="scss">
.import-profile-details {
  & .vc-container__inner {
    @apply tw-flex tw-flex-col;
  }
}
</style>
