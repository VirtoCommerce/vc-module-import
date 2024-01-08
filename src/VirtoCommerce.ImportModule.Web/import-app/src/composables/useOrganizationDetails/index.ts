import { computed, ComputedRef, ref, Ref } from "vue";
import { OrganizationInfo, OrganizationClient } from "../../api_client/import";

const organizationDetails = ref() as Ref<OrganizationInfo>;
const organizationClient = new OrganizationClient();

interface IUseOrganizationDetails {
  organizationDetails: ComputedRef<OrganizationInfo | null>;
  getOrganizationInfo: (organizationId?: string) => Promise<OrganizationInfo>;
}

export function useOrganizationDetails(): IUseOrganizationDetails {
  async function getOrganizationInfo(organizationId?: string): Promise<OrganizationInfo> {
    try {
      organizationDetails.value = await organizationClient.getOrganizationInfo(organizationId);
    } catch (e) {
      console.error(e);
      throw e;
    }

    return organizationDetails.value;
  }

  return {
    organizationDetails: computed(() => organizationDetails.value),
    getOrganizationInfo,
  };
}
