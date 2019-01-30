/// <reference path="../references.ts" />
module Services {
    export class AuthSvc {
        ReturnPath: string;
        CheckPageAccess: (permission: string, domain: string) => ng.IPromise<any>;
        permissionsList: Array<string>;
        setPermissions: (permissions: Array<string>) => void;
        hasPermission: (permission: string) => boolean;
        hasAnyPermission: (permissionString: string) => boolean;
        loadPermissions: () => ng.IPromise<any>;
        domainAlias: string;

        OfflineLogin: (userId: number, pin: string) => void;
        Login: (user: string, password: string, save: boolean) => void;
        Logout: () => void;
        GetUser: () => any;
        IsAuthenticated: () => boolean;

        LoginAsDemo: () => void;
        LoginAsAdmin: () => void;
        LoginAsManager: () => void;

        LoginAsEngineer: () => void;
        LoginAsDesigner: () => void;
        LoginAsSiteManager: () => void;


        progress: boolean;

        static $inject: Array<string> = ['$http', '$rootScope', '$q', 'IDBHelpSvc', '$idb', 'localStorageService']
        constructor($http: ng.IHttpService,
            $rootScope: ng.IRootScopeService,
            $q: ng.IQService,
            IDBHelpSvc: Services.IDBHelpSvc,
            $idb: any,
            localStorageService: ng.local.storage.ILocalStorageService) {

            $http.get("api/domain/alias").success((data) => this.domainAlias = JSON.parse(data));

            var currentUser;

            this.progress = false;
            this.loadPermissions = () => {
                return $http.post('api/auth/permissions', {})
                    .success((perm) => {
                        this.setPermissions(perm);
                        localStorageService.set('currentPermissions', JSON.stringify(perm));
                    })
                    .error((err) => {
                        Dialogs.showError({ message: "Permission request fault" });
                        // JL().warn('permission request fault ' + data);
                    });
            }

            var loadCurrentUser = () => {
                return $http.get('api/auth/CurrentUser', {})
                    .success((user) => {
                    setUser(user);
                    localStorageService.set('currentUser', JSON.stringify(user));
                })
                    .error((err) => {
                    Dialogs.showError({ message: "User data request fault" });
                });
            }

            this.setPermissions = (permissions: Array<string>) => {
                this.permissionsList = permissions;
                $rootScope.$broadcast('auth:permissionsChanged');
            }

            var setUser = (user?) => {
                currentUser = user ? user : null;
            }

            this.GetUser = () => {
                return currentUser;
            }

            this.IsAuthenticated = () => {
                return currentUser !== null;
            }

            this.CheckPageAccess = (permission, domain) => {
                var deferred = $q.defer();
               
                if (this.hasAnyPermission(permission) && (domain == null || this.domainAlias == domain)) {
                    deferred.resolve();
                    console.log('Page access granted');
                } else {
                    $rootScope.$broadcast('auth:pageAccessDenied');
                    deferred.reject();
                }
                return deferred.promise;
            };

            this.hasAnyPermission = (permissionString: string) => {
                var permissions = permissionString.split(',');
                var hasAny = false;
                for (var i = 0; i < permissions.length; i++) {
                    hasAny = hasAny || this.hasPermission(permissions[i].trim());
                    if (hasAny) break;
                }
                return hasAny;
            }

            // each permission check has to be duplicated on server!
            this.hasPermission = (permissionName: string, param?: string) => {
                if (! _.isString(permissionName)) {
                    return false;
                }

                var permission = permissionName.trim();

                if (permission == 'everybody') {
                    return true;
                }

                if (param != undefined) {
                    var permission1 = permission + "(" + param + ")";
                    var permission2 = permission + "()";

                    return _.any(this.permissionsList, (item: string) => {
                        if (_.isString(item))
                            return item.trim() === permission1 || item.trim() === permission2;
                    });
                }

                return _.any(this.permissionsList, (item: string) => {
                    if (_.isString(item))
                        return item.trim().indexOf(permission) >= 0;
                });
            }

            this.OfflineLogin = (userId: number, pin: string) => {
                $idb('pilingUsers').get(userId).then((pilingUser: PilingModels.PilingUser) => {
                    var expiredDate = h._date.WCFStrToDate(pilingUser.ExpiredOn);
                    if (expiredDate.getTime() > new Date().getTime()) {
                        var hash = CryptoJS.SHA512(pin + pilingUser.Salt).toString();
                        if (hash == pilingUser.HashedPIN) {
                            $idb('users').get(userId).then((u) => {
                                u.FullName = u.FirstName + ' ' + u.Surname;
                                setUser(u);
                                var perm = JSON.parse(pilingUser.Permissions);
                                this.setPermissions(perm);
                                localStorageService.set('currentUser', JSON.stringify(u));
                                localStorageService.set('currentPermissions', JSON.stringify(perm));
                                localStorageService.set('currentUserExpired', pilingUser.ExpiredOn);
                                $rootScope.$broadcast("auth:login", this.GetUser());
                            });
                        }
                        else {
                            $rootScope.$broadcast("auth:pin_invalid");
                        }
                    }
                    else {
                        $rootScope.$broadcast("auth:pin_expiredOn", h._date.ToLocalDateTimeString(expiredDate));
                    }
                });
            }

            this.Login = (user: string, password: string, save: boolean) => {  
                return $http.post('api/auth/login', {
                    'User': user, 'Pass': CryptoJS.SHA512(password).toString(), 'Save': save
                })
                    .success((data) => {
                    if (data.UserNotFound) {
                        setUser();
                        $rootScope.$broadcast("auth:invalid_user");
                    } else if (data.InvalidPassword) {
                        setUser();
                        $rootScope.$broadcast("auth:invalid_password");
                    } else {
                        setUser(data);
                        localStorageService.set('currentUser', JSON.stringify(data));
                        this.loadPermissions()
                            .then(() => {
                            $rootScope.$broadcast("auth:login", this.GetUser());
                        });
                    }
                });
            }

            this.Logout = () => {
                if (!navigator.onLine) {
                    resetUser();
                }
                else {
                    $http.post('api/auth/logout', {})
                        .success((data) => {
                            resetUser();
                        }).error((data) => {
                            Dialogs.showError({ message: 'logout fault ' + data });
                            resetUser();
                        });
                }
            }

            this.LoginAsDemo = () => this.Login('demo@bbconsult.co.uk', 'demo@bbconsult.co.uk', true); 
            this.LoginAsAdmin = () => this.Login('admin@bbconsult.co.uk', 'admin@bbconsult.co.uk', true);
            this.LoginAsManager = () => this.Login('manager@bbconsult.co.uk', 'manager@bbconsult.co.uk', true);

            this.LoginAsEngineer = () => this.Login('engineer@bbconsult.co.uk', 'engineer', true); 
            this.LoginAsDesigner = () => this.Login('designer@bbconsult.co.uk', 'designer', true); 
            this.LoginAsSiteManager = () => this.Login('sitemanager@bbconsult.co.uk', 'sitemanager', true); 

            $rootScope.$on('auth:pin_expiredOn',(event, expiredDate) => {
                Dialogs.showWarning({ message: 'Your PIN was expired on ' + expiredDate });
            });

            var loadFromStorage = () => {
                var expiredOn = <string>localStorageService.get('currentUserExpired');
                if (expiredOn) {
                    if (moment(expiredOn) <= moment()) {
                        resetUser();
                        $rootScope.$broadcast("auth:pin_expiredOn", moment(expiredOn).format('HH:mm DD/MM/YYYY'));
                    }
                } 
                var storedUser = <string>localStorageService.get('currentUser');
                if (storedUser) {
                    setUser(JSON.parse(storedUser));
                } else {
                    localStorageService.set('currentUser', JSON.stringify(window['currentUser']));
                    setUser(window['currentUser']);
                }
                
                var permissions = <string>localStorageService.get('currentPermissions');
                if (permissions) {
                    this.setPermissions(JSON.parse(permissions));
                } else {
                    localStorageService.set('currentPermissions', JSON.stringify(window['currentPermissions']));
                    this.setPermissions(window['currentPermissions']);
                }

                if (navigator.onLine) {
                    this.loadPermissions();
                    loadCurrentUser();
                }
                
                console.log(this.ReturnPath);
            }

            var resetUser = () => {
                setUser();
                this.setPermissions([]);
                localStorageService.remove('currentUser');
                localStorageService.remove('currentPermissions');
                localStorageService.remove('currentUserExpired');
                $rootScope.$broadcast("auth:logout", null);
            }

            loadFromStorage();
        }
    }
}

angular.module('Services', [])
    .factory('AuthSvc',
    [
        '$http', '$rootScope', '$q', 'IDBHelpSvc', '$idb', 'localStorageService',
        ($http: ng.IHttpService, $rootScope: ng.IRootScopeService, $q: ng.IQService, IDBHelpSvc: Services.IDBHelpSvc, $idb: any, localStorageService: ng.local.storage.ILocalStorageService) => {
            return new Services.AuthSvc($http, $rootScope, $q, IDBHelpSvc, $idb, localStorageService);
        }
    ]);
