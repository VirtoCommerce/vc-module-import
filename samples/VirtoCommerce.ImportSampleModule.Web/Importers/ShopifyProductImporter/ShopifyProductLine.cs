using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.ImportModule.Core.Common;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class ShopifyProductLine
    {
        public string Handle { get; set; }
        public string Title { get; set; }
        public string BodyHtml { get; set; }
        public string Vendor { get; set; }
        public string StandardProductType { get; set; }
        public string CustomProductType { get; set; }
        public string Tags { get; set; }
        public string Published { get; set; }
        public string Option1Name { get; set; }
        public string Option1Value { get; set; }
        public string Option2Name { get; set; }
        public string Option2Value { get; set; }
        public string Option3Name { get; set; }
        public string Option3Value { get; set; }
        public string VariantSku { get; set; }
        public string VariantGrams { get; set; }
        public string VariantInventoryTracker { get; set; }
        public string VariantInventoryQty { get; set; }
        public string VariantInventoryPolicy { get; set; }
        public string VariantFulfillmentService { get; set; }
        public string VariantPrice { get; set; }
        public string VariantCompareAtPrice { get; set; }
        public string VariantRequiresShipping { get; set; }
        public string VariantTaxable { get; set; }
        public string VariantBarcode { get; set; }
        public string ImageSrc { get; set; }
        public string ImagePosition { get; set; }
        public string ImageAltText { get; set; }
        public string GiftCard { get; set; }
        public string SeoTitle { get; set; }
        public string SeoDescription { get; set; }
        public string GoogleShoppingGoogleProductCategory { get; set; }
        public string GoogleShoppingGender { get; set; }
        public string GoogleShoppingAgeGroup { get; set; }
        public string GoogleShoppingMpn { get; set; }
        public string GoogleShoppingAdWordsGrouping { get; set; }
        public string GoogleShoppingAdWordsLabels { get; set; }
        public string GoogleShoppingCondition { get; set; }
        public string GoogleShoppingCustomProduct { get; set; }
        public string GoogleShoppingCustomLabel0 { get; set; }
        public string GoogleShoppingCustomLabel1 { get; set; }
        public string GoogleShoppingCustomLabel2 { get; set; }
        public string GoogleShoppingCustomLabel3 { get; set; }
        public string GoogleShoppingCustomLabel4 { get; set; }
        public string VariantImage { get; set; }
        public string VariantWeightUnit { get; set; }
        public string VariantTaxCode { get; set; }
        public string CostPerItem { get; set; }
        public string Status { get; set; }

        public CatalogProduct ToProductDetails()
        {
            var result = ExType<CatalogProduct>.New();

            result.OuterId = Handle;
            result.Name = Title;
            result.Code = VariantSku;

            return result;
        }
    }
}
