// Call this to register your module to main application
var moduleName = 'virtoCommerce.importModule';

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])

    .run(['platformWebApp.widgetService',
        function (widgetService) {
        }
    ]);
