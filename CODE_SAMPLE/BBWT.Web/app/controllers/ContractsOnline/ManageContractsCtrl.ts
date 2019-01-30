/// <reference path="../../references.ts" />
module Controllers {
    export interface IManageContractsScope extends ng.IScope {
        ContractsDS: kendo.data.DataSource;
        GridOptions: kendo.ui.GridOptions;

        Add: () => void;
        Amend: (id) => void;
        Delete: (id, name) => void;
        ConstructionSchedule: (id, name, numb) => void;
        AssignUser: (id) => void;
        RemoveUser: (id) => void;

        ContractName: any;
        FilterStatuses: Array<any>;
        Filter: any;
        ApplyFilter: any;
        ResetFilter: any;

        ContractId: number;
        AddUser: (userId: number) => void;
        AddUserOptions: kendo.ui.DropDownListOptions;
        AddUserDs: kendo.data.DataSource;
        RemoveUserDs: kendo.data.DataSource;
        UsersGridOptions: kendo.ui.GridOptions;
        SaveUsers: () => void;
    }

    export class ManageContractsCtrl {
        ProjectsDS: kendo.data.DataSource;
        GridOptions: kendo.ui.GridOptions;

        static $inject: Array<string> = ['$scope', '$location', '$http', '$translate', '$idb', 'localStorageService'];
        constructor(
            $scope: Controllers.IManageContractsScope,
            $location: ng.ILocationService,
            $http: ng.IHttpService,
            $translate: ng.translate.ITranslateService,
            $idb,
            localStorageService: ng.local.storage.ILocalStorageService) {

            var selectedStatuses = [3];
            if (localStorageService.get("contractStatusFilter")) {
                selectedStatuses = <Array<any>>localStorageService.get("contractStatusFilter");
            }

            var createStatusFilter = (ids: Array<any>) => {
                if (ids.length) {
                    return {
                        logic: "or",
                        filters: $.map(ids, s => {
                            return { field: "StatusId", operator: "eq", value: s }
                        })
                    };
                } else return null;
            }

            $scope.Amend = (id) => {
                localStorage.setItem("contract", id);
                $location.path("/ContractsOnline/addContract");
            }

            $scope.Delete = (id, name) => {
                if ((<any>$scope.ContractsDS.get(id)).ScheduleCount > 0) {
                    Dialogs.showInfo({ message: "You must remove all design schedules for this contract before you can delete the contract." });
                } else {
                    Dialogs.showConfirmation({ message: "Are you sure that you want to delete contract " + name + "?" })
                        .done(() => {
                            $http.delete('api/contract/' + id)
                                .success(() => $scope.ContractsDS.read());
                        });
                }
            }

            $scope.Add = () => {
                localStorage.setItem("contract", "null");
                $location.path("/ContractsOnline/addContract");
            }

            $scope.ConstructionSchedule = (id, name, numb) => {
                localStorage.setItem("contract", JSON.stringify({ Id: id, ContractName: name, ContractNumber: numb }));
                $location.path("/ContractsOnline/designSchedules");
            }

            $scope.Filter = {};

            $http.get("api/contracts/GetAllContractStatuses").then(result => {
                $scope.FilterStatuses = $.map(result.data, s => {
                    if (selectedStatuses.indexOf(s.Id) !== -1) {
                        s.Checked = true;
                    }
                    return s;
                });
            });

            $scope.ApplyFilter = () => {
                var filterConditions = [];
                if ($scope.Filter.Name) {
                    filterConditions.push({ field: "ContractName", operator: "contains", value: $scope.Filter.Name });
                }
                if ($scope.FilterStatuses) {
                    var checked = [];
                    $scope.FilterStatuses.forEach(s => {
                        if (s.Checked)
                            checked.push(s.Id);
                    });
                    localStorageService.set("contractStatusFilter", checked);
                    filterConditions.push(createStatusFilter(checked));
                }
                $scope.ContractsDS.filter(filterConditions);
            }

            $scope.ResetFilter = () => {
                for (var i in $scope.Filter) {
                    $scope.Filter[i] = null;
                }
                $scope.FilterStatuses.forEach(s => {
                    s.Checked = false;
                });
                localStorageService.set("contractStatusFilter", []);
                $scope.ContractsDS.filter(false);
            }

            $scope.ContractsDS = Ui.GridBase.CreateDS("odata/ContractsOData", 10, {
                id: "Id",
                fields: {
                    Id: { type: "number" },
                    ContractName: { type: "string" },
                    ContractNumber: { type: "string" },
                    StatusStatusName: { type: "string" },
                    AssignedUsers: { type: "object" },
                    ScheduleCount: { type: "number" }
                }
                },
                null,
                createStatusFilter(selectedStatuses)
                );  

            $scope.GridOptions =
            {
                pageable: { refresh: true, pageSizes: [5, 10, 15, 20] },
                columns: [
                    { field: "ContractName", title: 'Contract Name', width: 140 },
                    { field: "ContractNumber", title: 'Contract No', width: 110 },
                    { field: "StatusStatusName", title: 'Status', width: 120 },
                    {
                        width: 130,
                        template: (options) => '<a ng-click="ConstructionSchedule(' + options.Id + ', \'' + options.ContractName + '\', \'' + options.ContractNumber + '\')">Design&nbsp;Schedules</a>'
                    },
                    {
                        width: 180,
                        title: 'Assigned Users',
                        template: (options) => options.AssignedUsers.join(', ')
                    },
                    {
                        width: 180,
                        template: (options) => '<a href="#" ng-click="AssignUser(\'' + options.Id + '\')">Assign&nbsp;User</a> <a href="#" ng-click="RemoveUser(\'' + options.Id + '\')"> Remove&nbsp;User</a>'
                    },
                    {
                        width: 180,
                        template: (options) => '<a ng-click="Amend(\'' + options.Id + '\')">Amend&nbsp;Details</a> <a ng-click="Delete(' + options.Id + ', \'' + options.ContractName + '\')">Delete</a>'
                    }
                ],
                sortable: true,
                selectable: false,
                editable: false,
                scrollable: true,
                resizable: true
            }

            $scope.AssignUser = (contractId: number) => {
                $scope.ContractId = contractId;
                
                $scope.AddUserDs = new kendo.data.DataSource({
                    transport: {
                        read:
                        {
                            url: "api/contract/" + contractId + "/availableusers",
                            dataType: "json"
                        }
                    }
                });
                $scope.AddUserDs.read();
                //$scope.AddUser($scope['SelectedUserId']);
                $("#addUserList").kendoDropDownList({
                    dataSource: $scope.AddUserDs,
                    dataTextField: 'FullName',
                    dataValueField: 'Id',
                    optionLabel: "Select a user..."
                });
                Dialogs.showCustom({ title: 'Add User', winId: 'dlgAddUser' });
            };

            $scope.AddUserOptions =
            {
                dataTextField: 'FullName',
                dataValueField: 'Id',
                optionLabel: "Select a user..."
            }

            $scope.AddUser = (userId: number) => {
                if (userId) {
                    $http.post("api/contract/" + $scope.ContractId + "/users", userId).then(() => {
                        $scope.ContractsDS.read();
                    });
                }
            };


            $scope.RemoveUser = (contractId: number) => {
                $scope.ContractId = contractId;

                $scope.RemoveUserDs = new kendo.data.DataSource({
                    transport: {
                        read:
                        {
                            url: "api/contract/" + contractId + "/users",
                            dataType: "json"
                        }
                    },
                    pageSize: 5,
                    schema: {
                        parse: (response) => {
                            for (var i = 0; i < response.length; i++) {
                                response[i].IsChecked = true;
                            }
                            return response;
                        }
                    }
                });
                $scope.RemoveUserDs.read();
                $("#assignedUsersGrid").data("kendoGrid").setDataSource($scope.RemoveUserDs);

                Dialogs.showCustom({ title: 'Remove Users', winId: 'dlgRemoveUser' });
            };

            $scope.UsersGridOptions =
            {
                pageable: { refresh: true, pageSize: 5 },
                columns: [
                    { title: '', width: 30, template: "<input type=\"checkbox\" ng-model=\"dataItem.IsChecked\" />" },
                    { field: "FullName", title: 'Name' }
                ],
                sortable: true,
                selectable: false,
                editable: false
            }

            $scope.SaveUsers = () => {
                var usersToRemove = $.grep<any>(<any>$scope.RemoveUserDs.data(), u => {
                    return !u.IsChecked;
                });

                var ids = $.map(usersToRemove, u => {
                    return u.Id;
                });

                $http({
                    url: "api/contract/" + $scope.ContractId + "/users",
                    method: "DELETE",
                    data: ids,
                    headers: { 'Content-Type': 'application/json' }
                }).then(() => {
                    $scope.ContractsDS.read();
                });
            };
        }
    }
}