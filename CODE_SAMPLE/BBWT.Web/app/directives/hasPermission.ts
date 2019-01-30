
module Directives {
    export class HasPermission {
        static $inject = ['AuthSvc'];
        constructor(AuthSvc: Services.AuthSvc) {
            var directive: ng.IDirective =
                {
                    restrict: "A",
                    link: ($scope, element, attrs) => {
                        if (!_.isString(attrs['permission'])) {
                            throw "hasPermission value must be a string";
                        }
                        
                        var values = attrs['permission'].trim();
                        var permissions = [];
    
                        values.split(',').forEach((value) => {
                            var permission = {
                                notFlag: value.trim()[0] === '!',
                                value: value
                            }

                            if (permission.notFlag) {
                                permission.value = value.slice(1).trim();
                            }
                            permissions.push(permission);
                        });


                        function toggleVisibilityBasedOnPermission() {
                            // checks multiple permissions using 'OR' logical operation
                            // breaks once gets true
                           
                            var allowAccess = false;
                            for (var i = 0; i < permissions.length; i++) {
                                var hasPermission = AuthSvc.hasPermission(permissions[i].value);
                                allowAccess = allowAccess ||
                                (hasPermission && !permissions[i].notFlag ||
                                    !hasPermission && permissions[i].notFlag);
                                if (allowAccess) break;
                            }

                            if (allowAccess)
                                element.show();
                            else
                                element.hide();
                        }
                        toggleVisibilityBasedOnPermission();
                        $scope.$on('auth:permissionsChanged', toggleVisibilityBasedOnPermission);
                    }
                }
            return directive;
        }
    }
}

angular.module('Directives', []).directive('permission', ['AuthSvc', (AuthSvc) => {
    return new Directives.HasPermission(AuthSvc);
}]);
