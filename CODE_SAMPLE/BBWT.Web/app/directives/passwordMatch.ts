module Directives {

    export class Match {
        constructor() {
            var directive: ng.IDirective =
                {
                    require: "ngModel",
                    link: function (scope, elem, attrs, ctrl) {
                        var otherInput = elem.inheritedData("$formController")[attrs['match']];

                        ctrl.$parsers.push(function (value) {
                            if (value === otherInput.$viewValue) {
                                ctrl.$setValidity("match", true);
                                return value;
                            }
                            ctrl.$setValidity("match", false);
                            return undefined;
                        });

                        otherInput.$parsers.push(function (value) {
                            ctrl.$setValidity("match", value === ctrl.$viewValue);
                            return value;
                        });
                    }
                }
            return directive;
        }
    }
}

angular.module('Directives', []).directive('match', () => {
    return new Directives.Match();
});