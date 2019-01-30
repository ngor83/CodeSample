/// <reference path="../../references.ts" />
module Controllers {
    export enum RoleType {
        Default = 0,
        CompanyAdmin = 10,
        Admin = 10
    }
    export class RolesCtrl {
        RolesDS: kendo.data.DataSource;
        GridOptions: kendo.ui.GridOptions;

        ViewDetails: (id: number) => void;
        DeleteRole: (id: number) => void;
        ApplyFilter: () => void;
        ResetFilter: () => void;
        Filter: any;

        static $inject: Array<string> = ['$scope', '$location', '$http', "$translate"];
        constructor($scope: ng.IScope, $location: ng.ILocationService, $http: ng.IHttpService, $translate: ng.translate.ITranslateService) {
            $scope['RolesCtrl'] = this;

            this.RolesDS = Ui.GridBase.CreateDS("odata/RolesOData");

            this.GridOptions =
            {
                selectable: false,
                pageable: { refresh: true, pageSize: 10 },
                columns: [
                    { field: "Name", title: $translate.instant("PAGES.ROLES.NAME.TITLE") },
                    {
                        field: "Id", title: $translate.instant("PAGES.ROLES.BUTTONS.TITLE"), width: "200px", sortable: false,
                        template: "<a ng-click=\"RolesCtrl.ViewDetails(#= Id #)\">" + $translate.instant("PAGES.ROLES.BUTTONS.EDIT.TITLE") +
                        "</a> <a permission='ManageRoles' ng-disabled=\"dataItem.IsAdmin\" ng-click=\"RolesCtrl.DeleteRole(#= Id #)\">" + $translate.instant("PAGES.ROLES.BUTTONS.DELETE.TITLE") + "</a>"
                    }],
                sortable: true
            }

            this.ViewDetails = (id: number) => $location.path('/admin/roles/' + id);
            this.DeleteRole = (id: number) => {
                var role: any = this.RolesDS.data().filter((r) => r.Id == id)[0];
                
                if (role.IsAdmin) {
                    Dialogs.showWarning({ message: $translate.instant("PAGES.ROLES.ADMIN_ERROR") });
                    return;
                } 
                Dialogs.showConfirmation({ message: $translate.instant("SHARED.ACTIONS.DELETE.CONFIRM_MESSAGE", {
                    item: $translate.instant("PAGES.ROLES.ITEM")
                }) }).done(() => {
                    $http.get('api/roles/DeleteRole/' + id)
                        .success(() => this.RolesDS.read())
                    .error((reason) => {
                        Dialogs.showError({ message: reason.ExceptionMessage });
                    });
                });
            }

            this.ApplyFilter = () => {
                var filterConditions = [];

                if (this.Filter.Name) {
                    filterConditions.push({ field: 'Name', operator: 'contains', value: this.Filter.Name });
                }

                Ui.GridBase.CreateFilters(this.RolesDS, filterConditions);
            }

            this.ResetFilter = () => {
                for (var i in this.Filter) {
                    this.Filter[i] = null;
                }
                this.RolesDS.filter([]);
            }
        }
    }
}