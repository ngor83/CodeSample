/// <reference path="../../references.ts" />
module Controllers {
    export class RegisterCtrl {
        Save: (data) => void;
        Cancel: () => void;
        ErrorMessage: string;
        Validator: kendo.ui.Validator;

        static $inject: Array<string> = ['$scope', '$location', '$http', 'LocalizationSvc', '$timeout', '$translate'];
        constructor(
            $scope: ng.IScope,
            $location: ng.ILocationService,
            $http: ng.IHttpService,
            LocalizationSvc: Services.LocalizationSvc,
            $timeout: ng.ITimeoutService,
            $translate: ng.translate.ITranslateService) {

            $scope['RegisterCtrl'] = this;
            
            this.Validator = $('form[name=form]').kendoValidator({
                validate: (e) => {
                    if (e.valid) {
                        $("#errors").addClass('hidden');
                    } else {
                        $("#errors").empty().removeClass('hidden');
                        var errors = e.sender.errors();
                        $.each(errors, (idx, str) => {
                            $("#errors").append('<div>' + str + '</div>');
                        });
                    }
                }
            }).data('kendoValidator');

            this.Save = (data) => {
                this.ErrorMessage = "";

                if (this.Validator.validate() && $scope.form.$valid) {
                    $http.post('api/Users/RegisterUser', {
                        Name: data.name,
                        FirstName: data.firstName,
                        Surname: data.surname,
                        Pass: CryptoJS.SHA512(data.pass).toString(),
                        Language: LocalizationSvc.GetCurrentLanguage()
                    })
                        .success((data) => {
                        $timeout(() => {
                            if (data.UsernameAlreadyExists) {
                                this.ErrorMessage = $translate.instant('PAGES.REGISTER.USER_EXISTS');
                            } else {
                                $location.path('/login');
                            }
                        });
                    })
                }
            }
            this.Cancel = () => { $location.path('/login'); }
        }
    }
}