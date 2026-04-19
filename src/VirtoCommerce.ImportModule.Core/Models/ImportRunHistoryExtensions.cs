namespace VirtoCommerce.ImportModule.Core.Models
{
    public static class ImportRunHistoryExtensions
    {
        public static bool IsResumable(this ImportRunHistory history)
        {
            return history is { Finished: not null } && history.ProcessedCount < history.TotalCount;
        }
    }
}
