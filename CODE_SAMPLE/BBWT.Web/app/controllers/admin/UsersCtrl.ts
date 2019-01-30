/// <reference path="../../references.ts" />
module Controllers {
    export interface IUsersScope extends ng.IScope {
        ViewDetails: (id: number) => void;
        DeleteUser: (id: number) => void;
        ApplyFilter: () => void;
        ResetFilter: () => void;
        Filter: any;

        UsersDS: kendo.data.DataSource;
        UsersGrid: kendo.ui.Grid;
        GridOptionsSimple: kendo.ui.GridOptions;
        LanguageOptions: kendo.ui.DropDownListOptions;
        RolesOptions: kendo.ui.DropDownListOptions;
        GroupsOptions: kendo.ui.DropDownListOptions;
        AllRoles: any[];
        AllGroups: any[];

        User: IUser;
        Save: (user: IUser) => void;
        ShowAddUser: () => void;
        Password: string;
        ShowLanguages: boolean;
        ValidationMessage: string;

        checkedAll: boolean;
        checkedIds: any[];
        checkOne(): void;
        checkAll(): void;
        onDataBound(e: any): void;

        showAddToRole(): void;
        showAddToGroup(): void;

        selectedRoleId: any;
        selectedGroupId: any;

        AssignToRole(): void;
        AssignToGroup(): void;

        GroupsEnabled: boolean;

        IsOnlyADLogin: boolean;
    }

    export class UsersCtrl {
        static $inject: Array<string> = ['$scope', '$location', '$http', "$translate", "LocalizationSvc", '$timeout', 'DictSvc', 'SettingsSvc'];
        constructor($scope: Controllers.IUsersScope,
            $location: ng.ILocationService,
            $http: ng.IHttpService,
            $translate: ng.translate.ITranslateService,
            LocalizationSvc: Services.LocalizationSvc,
            $timeout: ng.ITimeoutService,
            dictSvc: Services.DictSvc,
            settingsSvc: Services.SettingsSvc) {

            settingsSvc.isOnlyADLogin().then((result) => {
                $scope.IsOnlyADLogin = result;
            });

            $scope.GroupsEnabled = false;
            settingsSvc.isGroupsEnabled().then((enabled) => {
                $scope.GroupsEnabled = enabled;
                if (!enabled) {
                    $scope.UsersGrid.hideColumn(4);
                }
            });

            $scope.ShowLanguages = true;
            $scope.Password = null;
            $scope.UsersDS = Ui.GridBase.CreateDS("odata/UsersOData");
            $scope.checkedIds = [];
            angular.extend($scope.checkedIds, {
                hasAny: () => {
                    var hasAny = false;
                    $scope.checkedIds.forEach(x => hasAny = hasAny || (x === true));
                    return hasAny;
                }
            });
            $scope.checkedAll = false;

            var rolesLimit = 32;
            var groupsLimit = 32;
            $scope.GridOptionsSimple = {
                selectable: false,
                pageable: { refresh: true, pageSize: 10 },
                columns: [
                    {
                        field: "", type: "boolean", width: "31px",
                        template: (model) => "<input ng-model=\"checkedIds[" + model.Id + "]\" ng-change=\"checkOne($event)\" type=\"checkbox\" />",
                        headerTemplate: "<input ng-model=\"checkedAll\" ng-change=\"checkAll($event)\" type=\"checkbox\" />"
                    },
                    { field: "Name", title: $translate.instant("PAGES.USERS.USERNAME.GRID_TITLE") },
                    { field: "FullName", title: $translate.instant("PAGES.USERS.FULLNAME.GRID_TITLE") },
                    {
                        title: $translate.instant("PAGES.USERS.ROLES.GRID_TITLE"),
                        template: item => {
                            var roles = item.Roles.join($translate.instant("SHARED.LIST_SEPARATOR"));
                            return roles.length > rolesLimit ? roles.substring(0, rolesLimit - 3) + '...' : roles;
                        }
                    },
                    {
                        title: $translate.instant("PAGES.USERS.GROUPS.GRID_TITLE"),
                        template: item => {
                            var groups = item.Groups.join($translate.instant("SHARED.LIST_SEPARATOR"));
                            return groups.length > groupsLimit ? groups.substring(0, groupsLimit - 3) + '...' : groups;
                        }
                    },
                    {
                        field: "Id", title: $translate.instant("PAGES.USERS.BUTTONS.TITLE"), width: "200px", sortable: false,
                        template: "<a ng-click=\"ViewDetails(#= Id #)\">" + $translate.instant("PAGES.USERS.BUTTONS.EDIT.TITLE") + "</a>  <a ng-click=\"DeleteUser(#= Id #)\">" + $translate.instant("PAGES.USERS.BUTTONS.DELETE.TITLE") + "</a>"
                    }],
                sortable: true
            }

            $scope.checkOne = () => {
                var Ids = $scope.UsersGrid.dataSource.data().map(x => $scope.checkedIds[x.Id]);
                var checked = true;
                Ids.forEach(x => checked = checked && (x === true));
                $scope.checkedAll = checked;
            };

            $scope.checkAll = () => {
                var Ids = $scope.UsersGrid.dataSource.data().map(item => item.Id);
                Ids.forEach(x => $scope.checkedIds[x] = $scope.checkedAll);
            };

            $scope.onDataBound = (e) => {
                var data = e.sender.dataSource.data().map(x => $scope.checkedIds[x.Id]);
                var checked = true;
                data.forEach(x => checked = checked && (x === true));
                $scope.checkedAll = checked;
            }

            $scope.Save = (user: IUser) => {
                user.Password = CryptoJS.SHA512($scope.Password).toString();
                $http.post('api/users/SaveUser', user)
                    .success((data) => {
                    $timeout(() => {
                        if (data.UsernameAlreadyExists) {
                            $scope.ValidationMessage = $translate.instant('PAGES.USERS.USERNAME.ALREADY_EXISTS');
                        } else {
                            $('#dlgAddUser').data("kendoWindow").close();
                            $scope.UsersDS.read();
                        }
                    });
                });
            }

            $scope.ViewDetails = (id: number) => $location.path('/admin/users/' + id);

            $scope.DeleteUser = (id: number) => {
                Dialogs.showConfirmation({
                    message: $translate.instant("SHARED.ACTIONS.DELETE.CONFIRM_MESSAGE", {
                        item: $translate.instant("PAGES.USERS.ITEM")
                    })
                }).done(() => {
                    $http.get('api/users/DeleteUser/' + id)
                        .success(() => $scope.UsersDS.read());
                });
            }

            $scope.ApplyFilter = () => {
                var filterConditions = [];

                if ($scope.Filter.Name) {
                    filterConditions.push({ field: 'Name', operator: 'contains', value: $scope.Filter.Name });
                }

                if ($scope.Filter.FullName) {
                    filterConditions.push({ field: 'FullName', operator: 'contains', value: $scope.Filter.FullName });
                }

                if ($scope.Filter.Roles) {
                    filterConditions.push({ field: 'Roles', operator: 'any', value: $scope.Filter.Roles });
                }

                if ($scope.Filter.Groups) {
                    filterConditions.push({ field: 'Groups', operator: 'any', value: $scope.Filter.Groups });
                }

                Ui.GridBase.CreateFilters($scope.UsersDS, filterConditions);
            }

            $scope.ResetFilter = () => {
                for (var i in $scope.Filter) {
                    $scope.Filter[i] = null;
                }
                $scope.UsersDS.filter([]);
            }

            $scope.ShowAddUser = () => {
                $scope.User = {
                    Id: 0,
                    Name: '',
                    Email: '',
                    Password: '',
                    Language: LocalizationSvc.GetCurrentLanguage(),
                    Roles: [],
                    Groups: []
                };
                $scope.Password = '';
                $scope.ValidationMessage = null;
                Dialogs.showCustom({ title: $translate.instant("PAGES.USERS.DIALOGS.ADD.TITLE"), winId: 'dlgAddUser' });
            };

            $scope.LanguageOptions = {
                dataSource: LocalizationSvc.GetDropDownLanguageDataSource(),
                dataTextField: 'Name',
                dataValueField: 'Id',
                dataBound: (e) => {
                    $scope.ShowLanguages = e.sender.dataSource.view().length > 1;
                }
            };

            $scope.AllRoles = [];

            $scope.RolesOptions = {
                dataSource: new kendo.data.DataSource({
                    transport: {
                        read: options => {
                            dictSvc.GetAllRoles().then(data => {
                                var roles = [];
                                $scope.AllRoles = data;
                                data.forEach(r => {
                                    roles.push(r);
                                });
                                options.success(roles);
                            });
                        }
                    }
                }),
                dataTextField: 'Name',
                dataValueField: 'Id'
            };

            $scope.AllGroups = [];

            $scope.GroupsOptions = {
                dataSource: new kendo.data.DataSource({
                    transport: {
                        read: options => {
                            dictSvc.GetAllGroups().then(data => {
                                var groups = [];
                                $scope.AllGroups = data;
                                data.forEach(g => {
                                    if (!g.IsQueryGroup) {
                                        groups.push(g);
                                    }
                                });
                                options.success(groups);
                            });
                        }
                    }
                }),
                dataTextField: 'Name',
                dataValueField: 'Id'
            };

            $scope.showAddToRole = () => {
                Dialogs.showCustom({ title: $translate.instant("PAGES.USERS.DIALOGS.ADD_TO_ROLE.TITLE"), winId: 'dlgAddToRole' })
                    .then($scope.AssignToRole);
            }

            $scope.showAddToGroup = () => {
                Dialogs.showCustom({ title: $translate.instant("PAGES.USERS.DIALOGS.ADD_TO_GROUP.TITLE"), winId: 'dlgAddToGroup' })
                    .then($scope.AssignToGroup);
            }

            $scope.AssignToRole = () => {
                var userIds = [];
                $scope.checkedIds.forEach((checked, id) => {
                    if (checked) {
                        userIds.push(id);
                    }
                });
                var roleId = parseInt($scope.selectedRoleId);
                $http.post('api/users/AssignUsersToRole',
                    {
                        RoleId: roleId,
                        UserIds: userIds
                    })
                    .success(() => {
                        Dialogs.showSuccess({
                            message: $translate.instant(
                                userIds.length > 1 ?
                                "PAGES.USERS.DIALOGS.ADD_TO_ROLE.SUCCESS.MESSAGE_MANY" :
                                "PAGES.USERS.DIALOGS.ADD_TO_ROLE.SUCCESS.MESSAGE",
                                {
                                    count: userIds.length,
                                    roleName: $scope.AllRoles[roleId].Name
                                })
                    });
                    $scope.UsersDS.read();
                });
            }

            $scope.AssignToGroup = () => {
                var userIds = [];
                $scope.checkedIds.forEach((checked, id) => {
                    if (checked) {
                        userIds.push(id);
                    }
                });
                var groupId = parseInt($scope.selectedGroupId);
                $http.post('api/users/AssignUsersToGroup',
                    {
                        GroupId: groupId,
                        UserIds: userIds
                    })
                    .success(() => {
                    Dialogs.showSuccess({
                        message: $translate.instant(
                            userIds.length > 1 ?
                                "PAGES.USERS.DIALOGS.ADD_TO_GROUP.SUCCESS.MESSAGE_MANY" :
                                "PAGES.USERS.DIALOGS.ADD_TO_GROUP.SUCCESS.MESSAGE",
                            {
                                count: userIds.length,
                                groupName: $scope.AllGroups[groupId].Name
                            })
                    });
                    $scope.UsersDS.read();
                });
            }
        }
    }
}