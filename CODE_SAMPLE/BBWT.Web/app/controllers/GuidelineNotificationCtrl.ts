/// <reference path="../references.ts" />
module Controllers {
    export class GuidelineNotificationCtrl {
        ShowInfo: () => void;
        ShowSuccess: () => void;
        ShowError: () => void;
        HideAll: () => void;

        static $inject: Array<string> = ['$scope', '$location', '$http'];
        constructor($scope: ng.IScope, $location: ng.ILocationService, $http: ng.IHttpService) {
            $scope['GuidelineNotificationCtrl'] = this;

            this.ShowInfo = () => {
                Dialogs.showInfoNotification({ title: 'Welcome', message: 'This is Info notification.' });
            }

            this.ShowSuccess = () => {
                Dialogs.showSuccessNotification({ title: 'Welcome', message: 'This is Success notification.' });
            }

            this.ShowError = () => {
                Dialogs.showErrorNotification({ title: 'Error', message: 'This is Error notification.' });
            }

            this.HideAll = () => {
                Dialogs.hideAllNotifications();
            }
        }
    }
}
