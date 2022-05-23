namespace VirtoCommerce.ImportModule.Data.Models
{
    public class ImportDataPreview
    {
        public int TotalCount { get; set; }
        public string FileName { get; set; }
        public object[] Records { get; set; }
    }
}
