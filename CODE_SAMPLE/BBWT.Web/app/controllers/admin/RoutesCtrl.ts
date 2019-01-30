/// <reference path="../../references.ts" />
module Controllers {
    export class RouteItem {
        Path: string;
        Title: string;
        Permission: string;
        Roles: string[];
    }

    export interface IRoutesScope {
        RoutesDS: kendo.data.DataSource;
        RoutesGrid: kendo.ui.GridOptions;
        ApplyFilter: () => void;
        ResetFilter: () => void;
        Filter: any;
    }

    export class RoutesCtrl {
        routes: RouteItem[];
        roles: string[];

        static $inject: Array<string> = ['$scope', '$state', '$http', '$translate'];
        constructor($scope: IRoutesScope, $state: any, $http: ng.IHttpService, $translate: ng.translate.ITranslateService) {
            this.routes = [];
            this.roles = [];

            angular.forEach($state.get(), (config, route) => {
                if (!config.abstract && config.redirectTo == undefined) {
                    this.routes.push({
                        Path: config.url,
                        Title: $translate.instant(config.title),
                        Permission: config.permission,
                        Roles: []
                    });
                }
            });

            $http.post('/api/roles/GetRolePermissions', null).success(data => {
                angular.forEach(data, (val, key) => {
                    this.roles.push(val.Role);
                    angular.forEach(this.routes, (route, rk) => {
                        angular.forEach(val.Permissions, (perm, pk) => {
                            if (route.Permission == perm) {
                                route.Roles.push(val.Role);
                            }
                        });
                    });
                });
                $scope.RoutesGrid.dataSource = $scope.RoutesDS;
                $scope.RoutesDS.read(this.routes);
            });

            $scope.RoutesDS = new kendo.data.DataSource({
                pageSize: 15,
                transport: {
                    read: (options) => { options.success(this.routes); }
                },
                schema: {
                    model: {
                        id: "Path",
                        fields: {
                            Path: {
                                editable: false,
                                type: 'string'
                            },
                            Title: {
                                editable: false,
                                type: 'string'
                            },
                            Permission: {
                                editable: false,
                                type: 'string'
                            }
                        }
                    }
                }
            });

            $scope.RoutesGrid = {
                selectable: false,
                pageable: { refresh: true, pageSize: 10 },
                columns: [
                    { field: "Path", title: $translate.instant("PAGES.ROUTES.PATH") },
                    { field: "Title", title: $translate.instant("PAGES.ROUTES.ROUTE_TITLE") },
                    { field: "Permission", title: $translate.instant("PAGES.ROUTES.PERMISSION") },
                    {
                        field: "Roles",
                        title: $translate.instant("PAGES.ROUTES.ROLES"),
                        template: "#=Roles.join(', ')#"
                    }
                ],
                sortable: true
            };
     
        
        
            $scope.ApplyFilter = () => {
                var filterConditions = [];

                if ($scope.Filter.Path) {
                    filterConditions.push({ field: 'Path', operator: 'contains', value: $scope.Filter.Path });
                }

                if ($scope.Filter.Title) {
                    filterConditions.push({ field: 'Title', operator: 'contains', value: $scope.Filter.Title });
                }

                if ($scope.Filter.Permission) {
                    filterConditions.push({ field: 'Permission', operator: 'contains', value: $scope.Filter.Permission });
                }

                Ui.GridBase.CreateFilters($scope.RoutesDS, filterConditions);
            }

            $scope.ResetFilter = () => {
                $scope.Filter = {};
                $scope.RoutesDS.filter([]);
            }  
        }
    }
}
