using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Pursuit.Context;
using Pursuit.Model;
/* =========================================================
    Item Name: API's Related to Role - RoleController
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]

    [ApiController]
    [Authorize]
    public class RoleController : ControllerBase
    {

        private readonly IPursuitRepository<Role> _roleRepository;
        private readonly ILogger<RoleController> _logger;
        public RoleController(IPursuitRepository<Role> roleRepository, ILogger<RoleController> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }
        [HttpGet("getListOfRoles")]
        public async Task<IActionResult> GetListOfRoles()
        {
            _logger.LogInformation("Queried Users Data");
            try
            {
                Guid guid;
                //Getting all users from the database
                var role = await Task.Run(() => _roleRepository
                .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable());
                return Ok(role);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex.Message, ex);
            }
            return NoContent();
        }
    }
}
