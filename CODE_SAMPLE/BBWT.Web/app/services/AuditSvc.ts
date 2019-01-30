/// <reference path="../references.ts" />
module Services {
    export enum ChangeLogActionType {
        Create = 0,
        Update = 1,
        Delete = 2,
        Other = 3
    }

    export interface IAuditSvc {
        getChangeLogs: (entityType: string, entityId: number) => ng.IPromise<Array<ChangeLogModel>>;
        getAllChangeLogs: () => ng.IPromise<Array<ChangeLogModel>>;
        getAllChangeLogsTransport: (options: kendo.data.DataSourceTransportOptions) => void;
        saveChangeLog: (changeLog: ChangeLogAddModel) => any;
        getActionTypes: () => ng.IPromise<any[]>;
    }
    export class AuditSvc implements IAuditSvc {
        static $inject: Array<string> = ['$http', '$q', '$translate']
        constructor(
            private $http: ng.IHttpService,
            private $q: ng.IQService,
            private $translate: ng.translate.ITranslateService) {

        }

        getChangeLogs = (entityType: string, entityId: number) => {
            var defer = this.$q.defer();

            this.$http({
                url: "api/changelog/GetChangeLogs",
                method: "GET",
                params: { entityType: entityType, entityId: entityId }
            }).success((data) => {
                var result: Object[] = _.map(data,(changeLog: any) => {
                    return new ChangeLogModel(changeLog);
                });

                defer.resolve(result);
            });

            return defer.promise;
        }

        getAllChangeLogs = () => {
            var defer = this.$q.defer();
            this.$http.get("api/changelog/GetAllChangeLogs", {
                params: {}
            }).success((data) => {
                var result: Object[] = _.map(data,(changeLog: any) => {
                    return new ChangeLogModel(changeLog);
                });
                defer.resolve(result);
            })
                .error(reason => {
                defer.reject(reason);
            });
            return defer.promise;
        }

        getAllChangeLogsTransport = (options: kendo.data.DataSourceTransportOptions) => {
            var params = Ui.GridBase.ODataParameterMap(options.data, 'read');
            this.$http.get("odata/ChangeLogsOData", {
                params: params
            }).success((data) => {
                var result: Object[] = _.map(data.value,(changeLog: any) => {
                    return new ChangeLogModel(changeLog);
                });
                data.value = result;
                options.success(data);
            })
                .error(reason => {
                options.error(reason);
            });
        }

        saveChangeLog = (changeLog: ChangeLogAddModel): any => {

            return this.$http.post("api/changelog/SaveChangeLog", changeLog)
                .success(() => {
            }).error((response) => {
                Dialogs.showError({
                    message: response.Errors.map(elem => elem.Message).join(",")
                });
            });
        }

        getActionTypes = () => {
            var defer = this.$q.defer();

            this.$http({
                url: "api/changelog/GetActionTypes",
                method: "GET"
            }).success((data) => {
                var result: Object[] = _.map(data,(actionType: any) => {
                    actionType.Name = this.$translate.instant("COMPONENTS.LOGS_GRID.ACTIONS." + actionType.Name + ".TITLE");
                    return actionType;
                });

                defer.resolve(result);
            });

            return defer.promise;
        }
    }
}

angular.module('Services', [])
    .factory('AuditSvc',
    [
        '$http', '$q', '$translate',
        ($http: ng.IHttpService, $q: ng.IQService, $translate: ng.translate.ITranslateService) => {
            return new Services.AuditSvc($http, $q, $translate);
        }
    ]);
