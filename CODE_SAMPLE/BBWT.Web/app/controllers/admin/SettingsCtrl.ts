/// <reference path="../../references.ts" />
module Controllers {
    export interface SettingsScope {
        Settings: any;

        Load: () => void;
        Save: () => void;
        Reset: () => void;

        UpdateFakeLocalization: (long: boolean, compound: boolean) => void;

        OriginalSettings: any;

        RolesDS: kendo.data.DataSource;
        RolesOptions: kendo.ui.DropDownListOptions;
    }

    export class SettingsCtrl {
        static $inject: Array<string> = ['$scope', '$location', '$http', '$translate', 'LocalizationSvc', '$q', 'SettingsSvc', 'AuthSvc'];
        constructor(
            $scope: Controllers.SettingsScope,
            $location: ng.ILocationService,
            $http: ng.IHttpService,
            $translate: ng.translate.ITranslateService,
            LocalizationSvc: Services.LocalizationSvc,
            $q: ng.IQService,
            settingsSvc: Services.SettingsSvc,
            authSvc: Services.AuthSvc) {

            settingsSvc.getSettings().then((settings) => {
                $scope.OriginalSettings = settings;
                $scope.Settings = angular.copy($scope.OriginalSettings);
            });

            $scope.RolesDS = Ui.GridBase.CreateDS("odata/RolesOData");

            $scope.RolesOptions = {
                dataValueField: "Id",
                dataTextField: "Name",
                dataSource: $scope.RolesDS
            };

            $scope.Save = () => {
                var confirmDefer = $q.defer();
                var changesConfirmed = confirmDefer.promise;
                
                if ($scope.Settings.GroupsAndCompanies.UsersBelongTo != $scope.OriginalSettings.GroupsAndCompanies.UsersBelongTo) {
                    Dialogs.showConfirmation({
                        message: $translate.instant('PAGES.SETTINGS.MESSAGES.CONFIRM_GC_CHANGE')
                    }).then(() => {
                        confirmDefer.resolve(true);
                    },() => {
                            confirmDefer.reject();
                        });
                } else {
                    confirmDefer.resolve(false);
                }
                changesConfirmed.then((n) => {
                    settingsSvc.saveSettings($scope.Settings).then(() => {
                        $scope.OriginalSettings = angular.copy($scope.Settings);
                        if (n) {
                            authSvc.loadPermissions();
                        }
                        LocalizationSvc.SwitchFakeLanguage($scope.Settings.Localization.UseFakeLocalization).then(() =>
                            Dialogs.showSuccess({ message: $translate.instant('PAGES.SETTINGS.MESSAGES.SAVED') }));
                    });
                });
            }

            $scope.Reset = () => { $scope.Settings = angular.copy($scope.OriginalSettings); }

            $scope.UpdateFakeLocalization = (long: boolean, compound: boolean) => {
                $http.post('api/Language/UpdateFakeLocalization', { GenerateLongerFakes: long, GenerateCompoundFakes: compound })
                    .success((result) => {
                    if (LocalizationSvc.GetCurrentLanguage().toLowerCase() === 'fake') {
                        LocalizationSvc.UpdateAfterImport(LocalizationSvc.GetCurrentLanguage()).then(() => {
                            Dialogs.showSuccess({ message: $translate.instant('PAGES.SETTINGS.MESSAGES.FAKE_UPDATED') });
                        });
                    } else {
                        Dialogs.showSuccess({ message: $translate.instant('PAGES.SETTINGS.MESSAGES.FAKE_UPDATED') });
                    }
                });
            }
        }
    }
}
