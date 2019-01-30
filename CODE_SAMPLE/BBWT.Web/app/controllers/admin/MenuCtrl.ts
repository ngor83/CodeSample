/// <reference path="../../references.ts" />
module Controllers {
    //asc: Judging from content code this is more like directive than control. Never do like that.
    export class MenuCtrl {
        domainAlias: string;
        MenuOptions: kendo.ui.MenuOptions;
        FilterMenuItems: (menuItems: any[], routes: any[]) => any[]

        selectMenu: (e: kendo.ui.MenuSelectEvent) => void;
        buildMenu: () => void;
        refreshMenu: () => void;
        bindMenu: () => void;

        static $inject: Array<string> = ['$scope', '$state', 'AuthSvc', 'MenuSvc', '$http', '$rootScope', 'LocalizationSvc', 'SettingsSvc'];
        // Constructor
        constructor($scope: any,
            $state: any,
            authSvc: Services.AuthSvc,
            menuSvc: Services.MenuSvc,
            $http: ng.IHttpService,
            $rootScope: ng.IScope,
            LocalizationSvc: Services.LocalizationSvc,
            SettingsSvc: Services.SettingsSvc) {
            $scope.MenuCtrl = this;

            // generate menu             
            var generateMenu = (arr, parentId) => {
                var out = [];
                for (var i in arr) {
                    if (arr[i].ParentId == parentId) {
                        var items = generateMenu(arr, arr[i].Id);
                        if (items.length > 0) {
                            arr[i].Items = items;
                        }
                        var item = { id: arr[i].Id, text: arr[i].Name, url: arr[i].Url != null ? arr[i].Url : '', items: arr[i].Items, order: arr[i].Order };
                        out.push(item);
                    }
                }                
                out.sort(function descending(a, b) { return a.order - b.order; });

                return out;
            }
            
            SettingsSvc.getDomain().then((domain) => this.domainAlias = domain);

            // build filtered menu items
            this.FilterMenuItems = (menuItems: any[], routes: any[]): any[]=> {
                var r = routes;
                return menuItems.filter((item) => {
                    var exclude = false;
                    if (item.items) {
                        item.items = this.FilterMenuItems(item.items, routes);

                        if ((!item.items || _.isEmpty(item.items)) && !item.url)
                            exclude = true;
                    } else {
                        r.filter((route) => {
                                return route.url == item.url;
                            })
                            .forEach((route) => {
                                
                                if (!authSvc.hasAnyPermission(route.permission) || route.domain != this.domainAlias && route.domain != null ) {
                                    exclude = true;
                                }
                            });
                    }

                    return !exclude;
                });
            }

            this.bindMenu = () => {
                // bind menu    
                $http.get('api/menu/GetMenu')
                    .success((data) => {
                    menuSvc.menuData = generateMenu(data, 0);
                    $scope.$broadcast('menu:Changed');
                });
            };

            var filteredLinks;            
            var restrictedRoutes;          

            // build menu
            this.buildMenu = () => {
                
                // We only use routes which have permissions set, since unrestricted routes won't filter any menu items
                restrictedRoutes = _.filter($state.get(), function (route) {
                    
                    return route['permission'] && _.isString(route['permission'])
                });

                // We send to filter copy of menuData so we can refilter it later, if permissions change
                filteredLinks = this.FilterMenuItems($.extend(true, new Array(), menuSvc.menuData), restrictedRoutes);

                this.MenuOptions = {
                    popupCollision: "",
                    dataSource: filteredLinks,
                    closeOnClick: true,
                    openOnClick: false,
                    /*please, don't re-create the whole menu from here.
                    select: function (e) {
                        $scope.MenuCtrl.selectMenu(e);
                    }*/
                    activate: function (e) {
                        $(".k-menu .k-animation-container .k-group").addClass("submenu-border-fix");
                    },
                    close: function (e) {
                        $(".k-menu .k-animation-container .k-group").removeClass("submenu-border-fix");
                    }
                };

            }

            // refresh menu control on page
            this.refreshMenu = () => {
                if ($('#mainMenu').data('kendoMenu')) {
                    $('#mainMenu').data('kendoMenu').destroy();
                }
                
                var kendoMenu = $('#mainMenu').data('kendoMenu');
                //var that = kendoMenu;
                var oldFunc = kendo.ui.Menu.fn['_click'];             
                //c+p from kendo menu sources. To keep it in synch.
                kendo.ui.Menu.fn['_click'] = function (e) {
                    var that = this;
                    oldFunc.call(that, e);
                    var groupSelector = ".k-group",
                        allItemsSelector = ":not(.k-list) > .k-item",
                        LINK = "k-link",
                        HOVERSTATE = "k-state-hover";                        
                    var options = that.options,
                        target = $(kendo['eventTarget'](e)),
                        link = target.closest("." + LINK),
                        element = target.closest(allItemsSelector),
                        childGroup = element.children(groupSelector + ",.k-animation-container"),
                        childGroupVisible = childGroup.is(":visible");
                    if ((!childGroup.length || (options.openOnClick && childGroupVisible))) {
                        element.removeClass(HOVERSTATE).css("height"); // Force refresh for Chrome
                        that._oldHoverItem = that._findRootParent(element);
                        that.close(link.parentsUntil(that.element, allItemsSelector));
                        that.clicked = false;
                        if ("MSPointerUp".indexOf(e.type) != -1) {
                            e.preventDefault();
                        }
                        return;
                    }
                }
                $('#mainMenu').kendoMenu(this.MenuOptions);
              
            }

            $scope.$on('menu:Changed', () => {
                $scope.MenuCtrl.buildMenu();
                $scope.MenuCtrl.refreshMenu();
            });

            $scope.$on('auth:permissionsChanged', () => {
                $scope.MenuCtrl.buildMenu();
                $scope.MenuCtrl.refreshMenu();
            }); 

            $scope.$on('locale:Set',() => {
                this.bindMenu();
            }); 

            $scope.$on('menu:Refresh', () => {
                $scope.MenuCtrl.refreshMenu();
            }); 

            $scope.$on('menu:breadcrumbsChanged', (breadcrumbs) => {

                $scope.breadcrumbs = menuSvc.breadcrumbs;
               // $scope.$apply();
            });   
        }
    }
}