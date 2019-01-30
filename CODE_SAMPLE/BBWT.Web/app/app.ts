/// <reference path="references.ts" />
var version = new Date().getTime(); // Will be changed by jenkins to use build number as cache version key

angular.module('bbwt', ['HashBangURLs', 'ui.router', 'kendo.directives', 'Services', 'Directives', 'ngAnimate', 'ngSanitize', 'pascalprecht.translate', 'tmh.dynamicLocale', 'angular.filter', 'ngTouch', 'nakamura-to.angular-idb', 'LocalStorageModule', 'naif.base64'])
    .controller(Controllers)
    .config(["$provide", ($provide: ng.auto.IProvideService) => {
        //http://plnkr.co/edit/hSMzWC?p=preview
        //allow dynamic name of model, name of input name
        $provide.decorator("ngModelDirective", ["$delegate", $delegate => {
            var ngModel = $delegate[0], controller = ngModel.controller;
            ngModel.controller = ["$scope", "$element", "$attrs", "$injector", function (scope, element, attrs, $injector) {
                var $interpolate = $injector.get("$interpolate");
                attrs.$set("name", $interpolate(attrs.name || "")(scope));
                $injector.invoke(controller, this, {
                    "$scope": scope,
                    "$element": element,
                    "$attrs": attrs
                });
            }];
            return $delegate;
        }]);
        $provide.decorator("formDirective", ["$delegate", $delegate => {
            var form = $delegate[0], controller = form.controller;
            form.controller = ["$scope", "$element", "$attrs", "$injector", function (scope, element, attrs, $injector) {
                var $interpolate = $injector.get("$interpolate");
                attrs.$set("name", $interpolate(attrs.name || attrs.ngForm || "")(scope));
                $injector.invoke(controller, this, {
                    "$scope": scope,
                    "$element": element,
                    "$attrs": attrs
                });
            }];
            return $delegate;
        }]);
    }])
    .config(["$httpProvider", ($httpProvider: ng.IHttpProvider) => {
        $httpProvider.interceptors.push("requestInterceptor");
    }])
    .config(['$translateProvider', ($translateProvider: ng.translate.ITranslateProvider) => {
        $translateProvider.preferredLanguage('en-gb');
        $translateProvider.usePostCompiling(true);
        $translateProvider.useStaticFilesLoader({
            prefix: '/app/translations/translate_',
            suffix: '.json'/*?nocache=' + (new Date()).getTime()*/
        });
        // switch to json edited by developers
        $translateProvider.fallbackLanguage('default');
    }])
    .config(['tmhDynamicLocaleProvider', (tmhDynamicLocaleProvider) => {
        tmhDynamicLocaleProvider.localeLocationPattern('/Scripts/angular/i18n/angular-locale_{{locale}}.js');
    }])

    .config(["$provide", ($provide) => {
        //add logging to db
        $provide.decorator("$exceptionHandler", ["$delegate", 'LoggerSvc',
            ($delegate, loggerSvc: Services.LoggerSvc) => (exception, cause) => {
                $(document.body).removeClass("hidden");
                loggerSvc.add(exception, cause);
                $delegate(exception, cause);
            }
        ]);
    }])
    .factory('$exceptionHandler', () => {
        return (exception, cause) => {
            $(document.body).removeClass("hidden");
            Services.LogSvc.Log(exception.message + ' (caused by "' + cause + '")');
            console.error(exception.message + ' (caused by "' + cause + '")');
        };
    })
    .factory("requestInterceptor", ["$location", "$q", "$rootScope", "$injector", ($location: ng.ILocationService, $q: ng.IQService, $rootScope: ng.IRootScopeService, $injector: ng.auto.IInjectorService) => {
    return {
            'request': (config: ng.IRequestConfig) => {
                if (config.url.indexOf(".html") !== -1) {
                    config.url = config.url;/* + "?v=" + version;*/
                }
                return config || $q.when(config);
            },
            'response': (response) => {
                if (response.status === 403 || response.status === 401) {
                    $location.url("/unauthorized");
                    return $q.reject(response);
                }
                return response || $q.when(response);
            },
            'responseError': (response) => { //Intercept all http errors
                $(document.body).removeClass("hidden");

                var status = response.status;
                if (response.config.errorHandling === "webapi") { //Set this key in the invoker
                    switch (status) {

                        case 401: //Unauthorized
                            var translation = $injector.get("$translate");
                            Dialogs.showError({
                                title: translation.instant("ERRORS.DEFAULT.TITLE"),
                                message: translation.instant("ERRORS.401.MESSAGE")
                            }).then(() => {
                                    $location.url("/login");
                                    // url() here doesn't do anything until apply is called
                                    $rootScope.$apply();
                                });
                            break;

                        case 403: //Forbidden
                            var translation = $injector.get("$translate");
                            Dialogs.showError({
                                title: translation.instant("ERRORS.DEFAULT.TITLE"),
                                message: translation.instant("ERRORS.403.MESSAGE")
                            }).then(() => {
                                    $location.url("/unauthorized");
                                    // url() here doesn't do anything until apply is called
                                    $rootScope.$apply();
                                });
                            break;

                        case 404: //Not found
                            var translation = $injector.get("$translate");
                            Dialogs.showErrorConfirmation({
                                title: translation.instant("ERRORS.DEFAULT.TITLE"),
                                message: translation.instant("ERRORS.404.MESSAGE")
                            }).then(() => {
                                    $location.url("/about/reportaproblem");
                                    // url() here doesn't do anything until apply is called
                                    $rootScope.$apply();
                                });
                            break;

                        case 408: //Timeout
                            var translation = $injector.get("$translate");
                            Dialogs.showError({
                                title: translation.instant("ERRORS.DEFAULT.TITLE"),
                                message: translation.instant("ERRORS.408.MESSAGE")
                            });
                            break;

                        case 400: //Bad request
                            var translation = $injector.get('$translate');
                            Dialogs.showErrorConfirmation({
                                title: translation.instant("ERRORS.DEFAULT.TITLE"),
                                message: translation.instant("ERRORS.400.MESSAGE")
                            }).then(() => {
                                    $location.url("/about/reportaproblem");
                                    // url() here doesn't do anything until apply is called
                                    $rootScope.$apply();
                                });
                            break;

                        default: //All other errors including 500
                            var translation = $injector.get('$translate');
                            Dialogs.showErrorConfirmation({
                                title: translation.instant("ERRORS.DEFAULT.TITLE"),
                                message: translation.instant("ERRORS.DEFAULT.MESSAGE")
                            }).then(() => {
                                    $location.url("/about/reportaproblem");
                                    // url() here doesn't do anything until apply is called
                                    $rootScope.$apply();
                                });
                    }
                    return $q.reject(response);
                } else {
                    var isSyncRequest = /^Sync\//.test(response.config.url) || /NeedClearLocalDb/.test(response.config.url) || /syncLogs/.test(response.config.url);
                    if (response.status === 403 || response.status === 401) {
                        if (isSyncRequest) {
                            $location.url("/login");
                        } else {
                            $location.url("/unauthorized");
                        }
                        return $q.reject(response);
                    } else if (isSyncRequest) {
                        return $q.reject(response);
                    } else {
                        $location.url("/about/reportaproblem");
                        return $q.reject(response);
                    }
                }
            }
        }
}])
    .controller("mainAppCtrl", ["$scope", "$rootScope", "$location", "AuthSvc", "$translate", "LocalizationSvc", "$state",
        ($scope: ng.IScope, $rootScope: ng.IScope, $location: ng.ILocationService, AuthSvc: Services.AuthSvc, $translate: ng.translate.ITranslateService, LocalizationSvc: Services.LocalizationSvc,
            $state: any) => {
            $scope["AuthSvc"] = AuthSvc;

            $scope.$on('auth:pageAccessDenied', () => {
                var offStateChangeError = $rootScope.$on('$stateChangeError', () => {
                    if (AuthSvc.IsAuthenticated()) {
                        $state.go("unauthorized");
                    } else {
                        AuthSvc.ReturnPath = $location.url();
                        $state.go('login');
                    }
                    offStateChangeError();
                });
            });

            $rootScope.$on("$stateChangeSuccess", (event, current) => {
                LocalizationSvc.WaitForTranslation().then(() => {
                    if (current.hasOwnProperty('title')) {
                        $rootScope["title"] = $translate.instant(current.title);
                    }
                });
            });

            $scope.$on('locale:Set', () => {
                if ($state.current.hasOwnProperty('title')) {
                    $rootScope["title"] = $translate.instant((<any>$state.current).title);
                    console.log('Page title set to ' + $rootScope["title"]);
                }
            });
        }
    ]);
