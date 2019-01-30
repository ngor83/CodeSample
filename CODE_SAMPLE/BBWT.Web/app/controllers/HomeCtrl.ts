/// <reference path="../references.ts" />
module Controllers {
    export class HomeCtrl {
        ShowAlert: () => void;

        static $inject: Array<string> = ['$scope', '$translate'];
        constructor($scope: ng.IScope, $translate: ng.translate.ITranslateService) {
            $scope['HomeCtrl'] = this;
            this.ShowAlert = () => Dialogs.showInfo({ message: $translate.instant('PAGES.HOME.MESSAGES.OK') });

            $scope['status'] = 'ready';           
        }
    }
}