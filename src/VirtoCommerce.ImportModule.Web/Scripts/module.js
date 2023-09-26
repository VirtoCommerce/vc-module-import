// Call this to register your module to main application
var moduleName = 'virtoCommerce.importModule';

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])

    .run(['platformWebApp.widgetService',
        function (widgetService) {

            // Vendor details: Import widget
            var sellerImportWidget = {
                controller: 'virtoCommerce.importModule.sellerImportWidgetController',
                template: 'Modules/$(VirtoCommerce.Import)/Scripts/widgets/seller-import-widget.tpl.html'
            };
            widgetService.registerWidget(sellerImportWidget, 'sellerDetails');

        }
    ]);
