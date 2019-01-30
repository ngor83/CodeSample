/// <reference path="../../references.ts" />
module Controllers {
    export interface IAddContractScope extends ng.IScope {
        UserDS: kendo.data.DataSource;
        Save(): ng.IPromise<any>;
        Back: () => void;

        StatusesDS: kendo.data.DataSource;
        statusesOption: any;

        Contract: PilingModels.Contract;

        Header: any;
    }

    export class AddContractCtrl {
        static $inject: Array<string> = ['$scope', '$location', '$http', '$translate', '$q', 'IDBCacheSvc'];
        constructor(
            $scope: Controllers.IAddContractScope,
            $location: ng.ILocationService,
            $http: ng.IHttpService,
            $translate: ng.translate.ITranslateService,
            $q: ng.IQService,
            IDBCacheSvc: Services.IDBCacheSvc) {

            $scope.Contract = <PilingModels.Contract>{};

            var loadUsers = () => {
                $scope.UserDS = h.BL.GetSrtdUsersDS();
            }

            var loadStatuses = () => {
                $scope.StatusesDS = new kendo.data.DataSource({
                    transport: {
                        read: {
                            url: "api/contracts/GetAllContractStatuses"
                        }
                    }
                });

                $scope.statusesOption = {
                    dataTextField: "StatusName",
                    dataValueField: "Id"
                };
            }
            loadStatuses();

            var loadContract = () => {
                if ($scope.Contract.Id && $scope.Contract.Id != null) {
                    $http.get('api/Contracts/GetContract/' + $scope.Contract.Id)
                        .success((data) => {
                            $scope.Header = data.ContractName;
                            $scope.Contract = data;
                            $scope.Contract.StartDate = <any>moment(data.StartDate).toDate();
                            $scope.Contract.FrequencyOfInspectionsEvery = data.FrequencyOfInspectionsEvery ? data.FrequencyOfInspectionsEvery : 7;
                            $scope.Contract.MissedInspectionEscalateAfter = data.MissedInspectionEscalateAfter ? data.MissedInspectionEscalateAfter : 1;
                        });
                }
                else {
                    $scope.Header = "Add";
                }
                $scope.Contract.FrequencyOfInspectionsEvery = 7;
                $scope.Contract.MissedInspectionEscalateAfter = 1;
            }

            var activate = () => {
                $scope.Contract.Id = parseInt(localStorage.getItem("contract"));

                loadUsers();

                var promises = [
                ];
                $q.all(promises).then(() => {
                    loadContract();
                });
            }

            IDBCacheSvc.Load().then(() => {
                activate();
            }); 

            $scope.Back = () => {
                $location.path("/ContractsOnline/manageContracts");
            }

            $scope.Save = (): ng.IPromise<any> => {
                var defer = $q.defer();
                var contract = $scope.Contract;
                contract.StartDate = moment($scope.Contract.StartDate, "DD/MM/YYYY").toDate().toLocaleDateString()
                if ($scope.Contract.Id && $scope.Contract.Id != null) {
                    $http({
                        url: "api/Contracts/UpdateContract",
                        method: "POST",
                        data: contract
                    })
                        .success((data) => {
                            $location.path("/ContractsOnline/manageContracts");
                        });
                }
                else {
                    $http({
                        url: "api/Contracts/CreateContract",
                        method: "POST",
                        data: contract
                    })
                        .success((data) => {
                            $location.path("/ContractsOnline/manageContracts");
                        });
                }
                return defer.promise;
            };
        }
    }
}