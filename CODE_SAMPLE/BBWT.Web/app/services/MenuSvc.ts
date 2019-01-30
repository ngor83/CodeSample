/// <reference path="../references.ts" />
module Services {
    export class MenuSvc {
                
        breadcrumbs: any;
        rebuildBreadcrumbs: (route: any) => void;

        //menuDefaultData: any;
        menuData: any;
        serializeTreeDataSource: (items: any, arr: any, rootItem: any) => any;
        prepareMenuItems: (items: any) => any;

        static $inject: Array<string> = ['$rootScope', '$http']
        constructor($rootScope: ng.IRootScopeService, $http: ng.IHttpService) {
            $rootScope['MenuSvc'] = this;

            //// helpers
            // serialize tree into data for webapi/post
            this.serializeTreeDataSource = (items, arr, rootItem) => {
                if (items) {
                    for (var i = 0; i < items.length; i++) {
                        var thisItem = items[i];

                        var parentId = rootItem != null ? rootItem.id : 0;

                        var url = thisItem.value;

                        var item = {
                            Id: thisItem.id,
                            Name: thisItem.text,
                            Url: url,
                            ParentId: parentId,
                            Order: i,
                            IsAdded: thisItem.isAdded
                        };
                        arr.push(item);

                        if (items[i].items != null) {

                            var data = thisItem.children != null ? thisItem.children.data() : thisItem.items;

                            arr = this.serializeTreeDataSource(data, arr, thisItem);
                        }
                    }
                    return arr;
                } else return null;
            }
         
            // prepare items - prepare tree items in treeview format
            this.prepareMenuItems = (items) => {
                if (items) {
                    for (var i = 0; i < items.length; i++) {
                        items[i].value = items[i].url != null ? items[i].url : '',
                        items[i].url = null;
                        this.prepareMenuItems(items[i].items);
                    }
                    return items;
                }
            }

            this.rebuildBreadcrumbs = (route) => {
                
                if (route.bcrumbsParentUrl && route.bcrumbsParentTitle) {
                    this.breadcrumbs = { url: route.bcrumbsParentUrl, title: route.bcrumbsParentTitle };
                }
                else {
                    this.breadcrumbs = null;
                }

                $rootScope.$broadcast("menu:breadcrumbsChanged", this.breadcrumbs);
            }
            
        }
    }
}

angular.module('Services', [])
    .factory('MenuSvc',
    [
        '$rootScope', '$http',
        ($rootScope: ng.IRootScopeService, $http: ng.IHttpService) => {
            return new Services.MenuSvc($rootScope, $http);
        }
    ]);
