using System;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CatalogModule.Core.Services;

namespace VirtoCommerce.ImportSampleModule.Web.Importers
{
    public class PropertyMetadataLoader
    {
        private readonly IPropertyDictionaryItemSearchService _propDictItemSearchService;
        private readonly IPropertyDictionaryItemService _propDictItemService;
        public PropertyMetadataLoader(
            IPropertyDictionaryItemService propDictItemService,
            IPropertyDictionaryItemSearchService propDictItemSearchService)
        {
            _propDictItemService = propDictItemService;
            _propDictItemSearchService = propDictItemSearchService;
        }
        public async Task<Property> TryLoadMetadata(PropertyValue propertyValue, Property[] allMetadata, bool createNewDictItemIfNotExists = true)
        {
            var metadata = allMetadata.FirstOrDefault(x => string.Equals(x.Name, propertyValue.PropertyName, StringComparison.InvariantCultureIgnoreCase));

            if (metadata != null)
            {
                propertyValue.ValueType = metadata.ValueType;
                propertyValue.PropertyId = metadata.Id;
                propertyValue.Property = metadata;

                if (metadata.Dictionary)
                {
                    var dictItem = (await _propDictItemSearchService.SearchAsync(new PropertyDictionaryItemSearchCriteria
                    {
                        PropertyIds = new[] { metadata.Id },
                        Keyword = propertyValue.Value?.ToString()
                    })).Results
                    .FirstOrDefault();

                    //TODO: Finding value by localized value
                    if (dictItem == null && createNewDictItemIfNotExists == true)
                    {
                        dictItem = new PropertyDictionaryItem { Alias = propertyValue.Value.ToString(), PropertyId = metadata.Id };
                        await _propDictItemService.SaveChangesAsync(new[] { dictItem });
                    }
                    if (dictItem != null)
                    {
                        propertyValue.ValueId = dictItem.Id;
                        propertyValue.Alias = dictItem.Alias;
                    }
                }
            }
            return metadata;
        }
    }
}
