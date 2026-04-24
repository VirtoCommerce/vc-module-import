import { ref, computed } from "vue";
import {
  ImportClient,
  SearchImportRunHistoryCriteria,
  SearchImportRunHistoryResult,
} from "../../../../api_client/virtocommerce.import";
import { useApiClient, useAsync, useDataTablePagination } from "@vc-shell/framework";
import { useHelpers } from "../helpers";

const { getApiClient } = useApiClient(ImportClient);

export default function useImportHistory() {
  const { GetSellerId } = useHelpers();
  const historySearchResult = ref<SearchImportRunHistoryResult>();
  const pageSize = 15;

  const { loading, action: fetchImportHistory } = useAsync<SearchImportRunHistoryCriteria>(
    async (query?: SearchImportRunHistoryCriteria) => {
      const client = await getApiClient();

      const importUserId = await GetSellerId();
      const historyQuery: SearchImportRunHistoryCriteria = {
        ...(query || {}),
        take: pageSize,
        userId: importUserId,
      };
      historySearchResult.value = await client.searchImportRunHistory(historyQuery);
    },
  );

  const pagination = useDataTablePagination({
    pageSize,
    totalCount: computed(() => historySearchResult.value?.totalCount ?? 0),
    onPageChange: ({ skip }) => fetchImportHistory({ skip }),
  });

  return {
    loading: computed(() => loading.value),
    importHistory: computed(() => historySearchResult.value?.results),
    pagination,
    fetchImportHistory,
  };
}
