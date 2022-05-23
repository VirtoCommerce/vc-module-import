namespace VirtoCommerce.ImportModule.Data.Models
{
    public class ErrorInfo
    {
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public int? ErrorLine { get; set; }
        public string RawData { get; set; }

        public override string ToString()
        {
            return $"Line {ErrorLine}: {ErrorMessage}";
        }
    }
}
