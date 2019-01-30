namespace BBWT.Web.WebAPI
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Http;

    using AutoMapper;
    using AutoMapper.QueryableExtensions;

    using BBWT.Data.Membership;
    using BBWT.DTO.Membership;
    using BBWT.Services.Interfaces;
    using BBWT.Web.Attributes;

    /// <summary>
    /// Permissions controller
    /// </summary>
    public class PermissionsController : ApiController
    {
        private readonly IMembershipService service;

        private readonly IMappingEngine mapper;

        private readonly ICommonDataService dataService;

        /// <summary>Constructs permissions controller class</summary>
        /// <param name="svc">Membership service injection</param>
        /// <param name="mapper">Mapper instance</param>
        /// <param name="dataService">Data Service</param>
        public PermissionsController(IMembershipService svc, IMappingEngine mapper, ICommonDataService dataService)
        {
            this.service = svc;
            this.mapper = mapper;
            this.dataService = dataService;
        }

        /// <summary>Delete permission by id</summary>
        /// <param name="id">permission id</param>
        [HttpGet]
        [HasPermission(Permissions = "ManagePermissions")]
        public void DeletePermission(int id)
        {
            this.service.DeletePermission(id);
        }

        /// <summary>
        /// Get list of permissions
        /// </summary>
        /// <returns>List of permissions</returns>
        [HttpGet]
        [Authorize]
        public IQueryable<PermissionDTO> GetAllPermissions()
        {            
            return this.dataService.GetAll<Permission>().Project().To<PermissionDTO>();
        }

        /// <summary>Get single permission by id</summary>
        /// <param name="id">permission id</param>
        /// <returns>permission</returns>
        [HttpGet]
        [HasPermission(Permissions = "ManagePermissions")]
        public PermissionDTO GetPermissionById(int id)
        {            
            var permission = this.dataService.Find<Permission>(id);
            return this.mapper.Map<PermissionDetailsDTO>(permission);
        }

        /// <summary>Create or update permission</summary>
        /// <param name="dto">Permission DTO</param>
        [HttpPost]
        [HasPermission(Permissions = "ManagePermissions")]
        public void SavePermission(PermissionDetailsDTO dto)
        {
            if (dto.Id == 0)
            {
                this.service.CreatePermission(this.mapper.Map<Permission>(dto));
            }
            else
            {
                this.dataService.Update<Permission>(dto.Id, (permission) => this.mapper.Map(dto, permission));
            }
        }
    }
}