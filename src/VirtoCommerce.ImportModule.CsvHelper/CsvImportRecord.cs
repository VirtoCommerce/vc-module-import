namespace VirtoCommerce.ImportModule.CsvHelper
{
    public class CsvImportRecord<TRecord>
    {
        public int Row { get; set; }

        public string RawHeader { get; set; }
        public string RawRecord { get; set; }

        public TRecord Record { get; set; }
    }
}
