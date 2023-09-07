using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Pursuit.Context;
using Pursuit.Model;
using System;
using System.Runtime;
/* =========================================================
    Item Name: API's Related to Roles - RolesController
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
    public class RolesController : ControllerBase
    {
        private readonly IPursuitRepository<Role> _roleRepository;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IPursuitRepository<Role> roleRepository, ILogger<RolesController> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }

        [HttpPost("addRole")]
        public async Task AddRole(Role role)
        {
            await _roleRepository.InsertOneAsync(role);
        }

        [HttpGet("allRoleDetails")]
        public async Task<IActionResult> allRoleDetails()
        {
            _logger.LogInformation("Queried All the details of a Roles");
            try
            {
                var role = await Task.Run(() => _roleRepository
                    .FilterBy(f => f.Id != ObjectId.Empty));

                return Ok(role);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex.Message, ex);
            }
            return NoContent();
        }


        [HttpGet("getRoleData")]

        public async Task<IActionResult> GetRoleData()
        {
            _logger.LogInformation("Queried Roles Data");
            try
            {
                var role = await Task.Run(() => _roleRepository
                    .FilterBy(
                        f => f.Id != ObjectId.Empty,
                        p => new
                        {
                            p.RoleName,
                            AccessRules = p.AccessRules != null ? p.AccessRules.Select(x => new { x.Feature, x.Access }) : null
                        }
                    ));

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
