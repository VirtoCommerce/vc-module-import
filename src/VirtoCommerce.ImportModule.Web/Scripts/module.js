// Call this to register your module to main application
var moduleName = 'Import';

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])
    .config(['$stateProvider', '$urlRouterProvider',
        function ($stateProvider, $urlRouterProvider) {
            $stateProvider
                .state('workspace.ImportState', {
                    url: '/Import',
                    templateUrl: '$(Platform)/Scripts/common/templates/home.tpl.html',
                    controller: [
                        '$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
                            var newBlade = {
                                id: 'blade1',
                                controller: 'Import.helloWorldController',
                                template: 'Modules/$(VirtoCommerce.Import)/Scripts/blades/hello-world.html',
                                isClosingDisabled: true
                            };
                            bladeNavigationService.showBlade(newBlade);
                        }
                    ]
                });
        }
    ])
    .run(['platformWebApp.mainMenuService', 'platformWebApp.widgetService', '$state',
        function (mainMenuService, widgetService, $state) {
            //Register module in main menu
            var menuItem = {
                path: 'browse/Import',
                icon: 'fa fa-cube',
                title: 'Import',
                priority: 100,
                action: function () { $state.go('workspace.ImportState'); },
                permission: 'Import:access'
            };
            mainMenuService.addMenuItem(menuItem);
        }
    ]);
