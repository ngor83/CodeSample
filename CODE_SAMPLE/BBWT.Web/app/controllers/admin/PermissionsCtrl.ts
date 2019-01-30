/// <reference path="../../references.ts" />
module Controllers {
    export class PermissionsCtrl {
        PermissionsDS: kendo.data.DataSource;
        GridOptions: kendo.ui.GridOptions;

        ViewDetails: (id: number) => void;
        DeletePermission: (id: number) => void;
        ApplyFilter: () => void;
        ResetFilter: () => void;
        Filter: any;

        static $inject: Array<string> = ['$scope', '$location', '$http', "$translate"];
        constructor($scope: ng.IScope, $location: ng.ILocationService, $http: ng.IHttpService, $translate: ng.translate.ITranslateService) {
            $scope['PermissionsCtrl'] = this;

            this.PermissionsDS = Ui.GridBase.CreateDS("odata/PermissionsOData");             

            this.GridOptions =
            {
                selectable: false,
                pageable: { refresh: true, pageSize: 10 },
                columns: [{ field: "Id", title: $translate.instant("PAGES.PERMISSIONS.ID.TITLE"), width: "50px" },
                    { field: "Code", title: $translate.instant("PAGES.PERMISSIONS.CODE.TITLE") },
                    { field: "Name", title: $translate.instant("PAGES.PERMISSIONS.NAME.TITLE") },
                    {
                        field: "Id", title: $translate.instant("PAGES.PERMISSIONS.BUTTONS.TITLE"), width: "200px", sortable: false,
                        template: "<a ng-click=\"PermissionsCtrl.ViewDetails(#= Id #)\">" + $translate.instant("PAGES.PERMISSIONS.BUTTONS.EDIT.TITLE") + "</a> <a ng-hide=\"#= Id<1000 #\" ng-click=\"PermissionsCtrl.DeletePermission(#= Id #)\">" + $translate.instant("PAGES.PERMISSIONS.BUTTONS.DELETE.TITLE") + "</a>"
                    }],
                sortable: true
            }

            this.ViewDetails = (id: number) => $location.path('/admin/permissions/' + id);
            this.DeletePermission = (id: number) => {
                Dialogs.showConfirmation({ message: $translate.instant("SHARED.ACTIONS.DELETE.CONFIRM_MESSAGE", {
                    item: $translate.instant("PAGES.PERMISSIONS.ITEM")
                }) }).done(() => {
                    $http.get('api/permissions/DeletePermission/' + id)
                        .success(() => this.PermissionsDS.read());
                });
            }

            this.ApplyFilter = () => {
                var filterConditions = [];

                if (this.Filter.Name) {
                    filterConditions.push({ field: 'Name', operator: 'contains', value: this.Filter.Name });
                }

                Ui.GridBase.CreateFilters(this.PermissionsDS, filterConditions);
            }

            this.ResetFilter = () => {
                for (var i in this.Filter) {
                    this.Filter[i] = null;
                }
                this.PermissionsDS.filter([]);
            }
        }
    }
}