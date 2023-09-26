angular.module('virtoCommerce.importModule')
    .controller('virtoCommerce.importModule.sellerImportWidgetController', ['$scope',
        function ($scope) {
            var sellerId = $scope.widget.blade.currentEntity.id;

            $scope.openImportApp = function () {
                let url = '/apps/import-app/#/' + sellerId + '/import';
                window.open(url, '_blank');
            };
        }
    ]);
