import { useApiClient, useAsync } from "@vc-shell/framework";
import { OrganizationClient } from "@virtocommerce/import-app-api";
import { useRoute } from "vue-router";

const { getApiClient: getOrgApiClient } = useApiClient(OrganizationClient);

export const useHelpers = () => {
  const route = useRoute();

  const { loading: getCurrentOrganizationLoading, action: getCurrentOrganization } = useAsync(async () => {
    const client = await getOrgApiClient();
    return client.getOrganizationInfo();
  });

  async function GetSellerId(): Promise<string> {
    let result = route?.params?.sellerId as string;
    if (!result || result === "") {
      result = (await getCurrentOrganization())?.organizationId as string;
    }

    return result;
  }

  return {
    GetSellerId,
  };
};
