angular.module('Import')
    .controller('Import.helloWorldController', ['$scope', 'Import.webApi', function ($scope, api) {
        var blade = $scope.blade;
        blade.title = 'Import';

        blade.refresh = function () {
            api.get(function (data) {
                blade.title = 'Import.blades.hello-world.title';
                blade.data = data.result;
                blade.isLoading = false;
            });
        };

        blade.refresh();
    }]);
