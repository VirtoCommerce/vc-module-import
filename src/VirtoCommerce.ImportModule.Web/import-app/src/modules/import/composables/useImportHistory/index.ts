import { ref, computed } from "vue";
import {
  ImportClient,
  ISearchImportRunHistoryCriteria,
  SearchImportRunHistoryCriteria,
  SearchImportRunHistoryResult,
} from "@virtocommerce/import-app-api";
import { useApiClient, useAsync } from "@vc-shell/framework";
import { useHelpers } from "../helpers";

const { getApiClient } = useApiClient(ImportClient);

export default function useImportHistory() {
  const { GetSellerId } = useHelpers();
  const historySearchResult = ref<SearchImportRunHistoryResult>();
  const currentPage = ref(1);

  const { loading, action: fetchImportHistory } = useAsync<ISearchImportRunHistoryCriteria>(
    async (query?: ISearchImportRunHistoryCriteria) => {
      const client = await getApiClient();

      const importUserId = await GetSellerId();
      const historyQuery = new SearchImportRunHistoryCriteria({
        ...(query || {}),
        take: 15,
        userId: importUserId,
      });
      historySearchResult.value = await client.searchImportRunHistory(historyQuery);
      currentPage.value = (historyQuery?.skip || 0) / Math.max(1, historyQuery?.take || 15) + 1;
    },
  );

  return {
    loading: computed(() => loading.value),
    importHistory: computed(() => historySearchResult.value?.results),
    totalHistoryCount: computed(() => historySearchResult.value?.totalCount),
    historyPages: computed(() => Math.ceil((historySearchResult.value?.totalCount ?? 1) / 15)),
    currentPage: computed(() => currentPage.value),
    fetchImportHistory,
  };
}
