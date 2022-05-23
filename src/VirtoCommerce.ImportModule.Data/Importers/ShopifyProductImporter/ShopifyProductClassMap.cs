using CsvHelper.Configuration;

namespace VirtoCommerce.ImportModule.Data.Importers.ShopifyProductImporter
{
    public class ShopifyProductClassMap : ClassMap<ShopifyProductLine>
    {
        public ShopifyProductClassMap()
        {
            Map(m => m.Handle);
            Map(m => m.Title);
            Map(m => m.BodyHtml).Name("Body (HTML)");
            Map(m => m.Vendor);
            Map(m => m.StandardProductType).Name("Standard Product Type");
            Map(m => m.CustomProductType).Name("Custom Product Type");
            Map(m => m.Tags);
            Map(m => m.Published);
            Map(m => m.Option1Name).Name("Option1 Name");
            Map(m => m.Option1Value).Name("Option1 Value");
            Map(m => m.Option2Name).Name("Option2 Name");
            Map(m => m.Option2Value).Name("Option2 Value");
            Map(m => m.Option3Name).Name("Option3 Name");
            Map(m => m.Option3Value).Name("Option3 Value");
            Map(m => m.VariantSku).Name("Variant SKU");
            Map(m => m.VariantGrams).Name("Variant Grams");
            Map(m => m.VariantInventoryTracker).Name("Variant Inventory Tracker");
            Map(m => m.VariantInventoryQty).Name("Variant Inventory Qty");
            Map(m => m.VariantInventoryPolicy).Name("Variant Inventory Policy");
            Map(m => m.VariantFulfillmentService).Name("Variant Fulfillment Service");
            Map(m => m.VariantPrice).Name("Variant Price");
            Map(m => m.VariantCompareAtPrice).Name("Variant Compare At Price");
            Map(m => m.VariantRequiresShipping).Name("Variant Requires Shipping");
            Map(m => m.VariantTaxable).Name("Variant Taxable");
            Map(m => m.VariantBarcode).Name("Variant Barcode");
            Map(m => m.ImageSrc).Name("Image Src");
            Map(m => m.ImagePosition).Name("Image Position");
            Map(m => m.ImageAltText).Name("Image Alt Text");
            Map(m => m.GiftCard).Name("Gift Card");
            Map(m => m.SeoTitle).Name("SEO Title");
            Map(m => m.SeoDescription).Name("SEO Description");
            Map(m => m.GoogleShoppingGoogleProductCategory).Name("Google Shopping / Google Product Category");
            Map(m => m.GoogleShoppingGender).Name("Google Shopping / Gender");
            Map(m => m.GoogleShoppingAgeGroup).Name("Google Shopping / Age Group");
            Map(m => m.GoogleShoppingMpn).Name("Google Shopping / MPN");
            Map(m => m.GoogleShoppingAdWordsGrouping).Name("Google Shopping / AdWords Grouping");
            Map(m => m.GoogleShoppingAdWordsLabels).Name("Google Shopping / AdWords Labels");
            Map(m => m.GoogleShoppingCondition).Name("Google Shopping / Condition");
            Map(m => m.GoogleShoppingCustomProduct).Name("Google Shopping / Custom Product");
            Map(m => m.GoogleShoppingCustomLabel0).Name("Google Shopping / Custom Label 0");
            Map(m => m.GoogleShoppingCustomLabel1).Name("Google Shopping / Custom Label 1");
            Map(m => m.GoogleShoppingCustomLabel2).Name("Google Shopping / Custom Label 2");
            Map(m => m.GoogleShoppingCustomLabel3).Name("Google Shopping / Custom Label 3");
            Map(m => m.GoogleShoppingCustomLabel4).Name("Google Shopping / Custom Label 4");
            Map(m => m.VariantImage).Name("Variant Image");
            Map(m => m.VariantWeightUnit).Name("Variant Weight Unit");
            Map(m => m.VariantTaxCode).Name("Variant Tax Code");
            Map(m => m.CostPerItem).Name("Cost per item");
            Map(m => m.Status);
        }
    }
}
