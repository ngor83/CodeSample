namespace BBWT.Web.WebAPI
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Http;

    using AutoMapper;
    using AutoMapper.QueryableExtensions;

    using BBWT.Data.Menu;
    using BBWT.DTO;
    using BBWT.DTO.Menu;
    using BBWT.Services.Classes;
    using BBWT.Web.Attributes;

    /// <summary>
    /// Web API Menu Controller
    /// </summary>
    public class MenuController : ApiController
    {
        private readonly MenuService menuService;
        private readonly IMappingEngine mapper;

        /// <summary>Constructs MenuController class</summary>
        /// <param name="ms">Menu Service instance</param>    
        /// <param name="mapper">Mapping engine instance</param>
        public MenuController(MenuService ms, IMappingEngine mapper)
        {
            this.menuService = ms;
            this.mapper = mapper;
        }

        /// <summary>
        /// Get effective menu elements
        /// </summary>
        /// <param name="language">language id</param>
        /// <returns>List of menu elements</returns>
        public IQueryable<MenuItemDTO> GetMenu()
        {
            return this.menuService.GetAllMenuItems("en-gb").Project().To<MenuItemDTO>();
        }

        /// <summary>        
        /// Save menu        
        /// </summary>
        /// <param name="updateMenu">UpdateMenuItemsDTO</param>
        /// <returns>result</returns>
        [HttpPost]
        [HasPermission(Permissions = "ManageMenu")]
        public bool SaveMenu(UpdateMenuItemsDTO updateMenu)
        {
            return this.menuService.SaveMenu(this.mapper.Map<List<MenuItemPresentation>>(updateMenu.MenuItems), updateMenu.Language);         
        }

        /// <summary>
        /// The reset menu.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        [HttpPost]
        [HasPermission(Permissions = "ManageMenu")]
        public bool ResetMenu()
        {
            return this.menuService.ResetMenu();
        }
    }
}