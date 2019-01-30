/// <reference path="../../references.ts" />
module Controllers {
    export interface ILoginScope extends ng.IScope {
        GridVisibility: boolean;
        Login: (data) => void;
        Logout: () => void;
        LoginAsDemo: () => void;
        LoginAsAdmin: () => void;
        LoginAsManager: () => void;

        LoginAsEngineer: () => void;
        LoginAsDesigner: () => void;
        LoginAsSiteManager: () => void;

        ShowRecoverPasswordDlg: () => void;
        SendResetLink: () => void;

        User: any;
        IsAuthError: boolean;
        AuthErrorMessage: string;
        DomainAlias: () => boolean;

        IsLockOnRequest: boolean;
        IsCompanyRegistrationAllowed: boolean;
        IsOnlyADLogin: boolean;

        RecoveryData: any;
        IsOffline: boolean;
    }

    export class LoginCtrl {
        static $inject: Array<string> = ['$scope', '$rootScope', '$location', 'AuthSvc', '$state', 'SettingsSvc', '$translate', '$http', 'LocalizationSvc'];
        constructor($scope: Controllers.ILoginScope,
            $rootScope: ng.IRootScopeService,
            $location: ng.ILocationService,
            AuthSvc: Services.AuthSvc,
            $state: any,
            settingsSvc: Services.SettingsSvc,
            $translate: ng.translate.ITranslateService,
            $http: ng.IHttpService,
            LocalizationSvc: Services.LocalizationSvc) {

            $scope.IsOffline = !navigator.onLine;

            $scope.User = AuthSvc.GetUser();

            settingsSvc.isCompaniesEnabled().then((enabled) => {
                $scope.IsCompanyRegistrationAllowed = enabled;
            });

            settingsSvc.isOnlyADLogin().then((result) => {
                $scope.IsOnlyADLogin = result;
            });

            $http.get("api/domain/alias").success((data) => $scope.DomainAlias = JSON.parse(data));

            var cleanupLoginHandler = $rootScope.$on('auth:login',(event, user) => {
                $scope.IsLockOnRequest = false;      

                $scope.User = user;
                $scope.IsAuthError = false;
              
                if ($location.path().indexOf('/login') != -1) {
                    if (AuthSvc.ReturnPath != undefined) {
                        $location.url(AuthSvc.ReturnPath);
                    } else {
                        $location.url('/');
                    }
                    delete AuthSvc.ReturnPath;                    
                }
            });

            $scope.$on('$destroy', () => {
                cleanupLoginHandler();
            });

            $rootScope.$on('auth:invalid_user',(event) => {
                $scope.IsLockOnRequest = false;
                $scope.IsAuthError = true;
                $scope.AuthErrorMessage = $translate.instant('PAGES.LOGIN.ERRORS.INVALID_USERNAME');
            });
            
            $rootScope.$on('auth:invalid_password',(event) => {
                $scope.IsLockOnRequest = false;
                $scope.IsAuthError = true;
                $scope.AuthErrorMessage = $translate.instant('PAGES.LOGIN.ERRORS.INVALID_PASSWORD');
            });           
            
            $scope.Login = (data) => {        
                $scope.IsLockOnRequest = true;        
                AuthSvc.Login(data.name, data.pass, data.save);
            }
            
            $scope.Logout = () => AuthSvc.Logout();

            $scope.LoginAsDemo = () => {
                $scope.IsLockOnRequest = true;
                AuthSvc.LoginAsDemo();
            }            

            $scope.LoginAsAdmin = () => {
                $scope.IsLockOnRequest = true;
                AuthSvc.LoginAsAdmin();
            }
            
            $scope.LoginAsManager = () => {
                $scope.IsLockOnRequest = true;
                AuthSvc.LoginAsManager();
            }

            $scope.LoginAsEngineer = () => {
                $scope.IsLockOnRequest = true;
                AuthSvc.LoginAsEngineer();
            }
            $scope.LoginAsDesigner = () => {
                $scope.IsLockOnRequest = true;
                AuthSvc.LoginAsDesigner();
            }
            $scope.LoginAsSiteManager = () => {
                $scope.IsLockOnRequest = true;
                AuthSvc.LoginAsSiteManager();
            }

            $scope.RecoveryData = {
                Email: null,
                Language: LocalizationSvc.GetCurrentLanguage()
            };

            $scope.ShowRecoverPasswordDlg = () => {
                Dialogs.showCustom({
                    title: $translate.instant('PAGES.RECOVER_PASSWORD.TITLE'), winId: 'dlgRecoverPassword' });
            }

            $scope.SendResetLink = () => {
                $http.post('api/Users/RecoverPassword', $scope.RecoveryData)
                    .success((d) => {
                        if (d.Successfully) {
                            Dialogs.showSuccess({ message: $translate.instant('PAGES.RECOVER_PASSWORD.SUCCESS') });
                        } else {
                            Dialogs.showError({ message: d.Exception });
                        }
                    })
                    .error((e) => {
                        Dialogs.showError({ message: e.Message });
                    });
            };
        }
    }
}