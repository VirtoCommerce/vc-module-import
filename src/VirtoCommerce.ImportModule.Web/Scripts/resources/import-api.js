angular.module('Import')
    .factory('Import.webApi', ['$resource', function ($resource) {
        return $resource('api/Import');
}]);
