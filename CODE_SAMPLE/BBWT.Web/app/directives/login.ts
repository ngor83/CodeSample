/// <reference path="../references.ts" />
module Directives {
    export class Login {
        constructor() {
            var directive: ng.IDirective =
                {
                    restrict: "E", 
                    transclude: true,
                    templateUrl: "app/directives/login_control.html",
                    controller: "LoginDirCtrl"
                }
            return directive;
        } 
    }
}

angular.module('Directives', []).directive('login', () => {
    return new Directives.Login();
});