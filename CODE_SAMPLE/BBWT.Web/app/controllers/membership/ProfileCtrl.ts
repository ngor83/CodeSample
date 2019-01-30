/// <reference path="../../references.ts" />
module Controllers {
    export interface IProfileScope extends ng.IScope {
        data: any;
        passwordData: any;
        save: () => void;
        changePassword: () => void;
        showChangePassowrdDialog: () => void;
        loadUserSettings: () => void;
        languageOptions: kendo.ui.DropDownListOptions;
        IsOffline: boolean;
    }

    export class ProfileCtrl {

        static $inject: Array<string> = ['$scope', '$http', 'AuthSvc', 'LocalizationSvc', '$translate', '$idb'];
        constructor(
            $scope: IProfileScope,
            $http: ng.IHttpService,
            AuthSvc: Services.AuthSvc,
            LocalizationSvc: Services.LocalizationSvc,
            $translate: ng.translate.ITranslateService,
            $idb) {
            console.log('Profile controller entered');
            var user = AuthSvc.GetUser();
            var fullName = user.FullName.split(" ");
            $scope.IsOffline = !navigator.onLine;

            function showError(error: any) {
                Dialogs.showError({ message: error.ExceptionMessage });
            };

            $scope.data = {
                id: user.Id,
                firstName: user.FirstName,//fullName[0],
                surname: user.Surname,//fullName[1],
                name: user.Name,
                languageId: null,
            };

            $scope.passwordData = {
                currentPassword: null,
                newPassword: null,
                confirmNewPassword: null,
            };

            $scope.loadUserSettings = () => {
                $idb('userSettings').get(user.Id).then((us: PilingModels.UserSettings) => {
                    if(us)
                        $scope.data.languageId = us.LanguageId;
                    else
                        $scope.data.languageId = 'en-gb';
                });
            };

            $scope.save = function () {
                $http.post('api/Users/UpdateUser', $scope.data)
                    .success(() => {
                    LocalizationSvc.SetLanguagePreference($scope.data.languageId).then(() => {
                        $("#tabstrip").kendoTabStrip();
                        Dialogs.showSuccess({
                            message: $translate.instant("PAGES.PROFILE.SAVE.SUCCESS")
                        });
                    });
                })
                    .error(showError);
            };

            $scope.changePassword = () => {
                $http.post("api/Users/ChangePassword", {
                    Name: $scope.data.name,
                    CurrentPassword: CryptoJS.SHA512($scope.passwordData.currentPassword).toString(),
                    NewPassword: CryptoJS.SHA512($scope.passwordData.newPassword).toString()
                })
                    .success(() => Dialogs.showSuccess({ message: $translate.instant("PAGES.PROFILE.CHANGE_PASSWORD.SUCCESS") }))
                    .error(showError);
            };

            $scope.showChangePassowrdDialog = () => {
                Dialogs.showCustom({ title: $translate.instant("PAGES.PROFILE.SAVE.TITLE"), winId: "changePasswordInputDlg", width: "650px" });
            };

            $scope.languageOptions = {
                dataSource: LocalizationSvc.GetDropDownLanguageDataSource(),
                dataTextField: 'Name',
                dataValueField: 'Id',
                optionLabel: {
                    Id: null,
                    Name: $translate.instant('PAGES.PROFILE.LANGUAGE.DEFAULT_OPTION')
                }
            };

            $scope.loadUserSettings();
        }
    }
}