using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Pursuit.Context;
using Pursuit.Helpers;
using Pursuit.Model;

namespace Pursuit.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]

    [ApiController]
    public class DatabaseController : ControllerBase
    {
        private readonly IADRepository<ADRecord> _azureRepository;
        private readonly IADRepository<ADRecord> _adRepository;
        private readonly IADRepository<ADRecord> _googleRepository;
        private readonly IPursuitRepository<Role> _roleRepository;
        private readonly IPursuitRepository<User> _userRepository;
        private readonly IPursuitRepository<Admin_Configuration> _adminconfigRepository;
        private readonly ILogger<DatabaseController> _logger;

        public DatabaseController(ILogger<DatabaseController>? logger,ServiceResolver serviceResolver, IPursuitRepository<Role> roleRepository,
            IPursuitRepository<User> userRepository, IPursuitRepository<Admin_Configuration> adminconfigRepository)
        {
            _logger = logger;
            
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _adminconfigRepository = adminconfigRepository;
            _azureRepository = serviceResolver("AZ");
            _adRepository = serviceResolver("MS");
            _googleRepository = serviceResolver("GWS");

        }
        [HttpGet("DeleteRecords")]
        public async Task<IActionResult> DeleteRecords(string Password)
        {
            try
            {
                if (Password == "Pursuit")
                {
                    _userRepository.RemoveManyAsync(x => x.Id != ObjectId.Empty);
                    _adminconfigRepository.RemoveManyAsync(x => x.Id != ObjectId.Empty);
                    _roleRepository.RemoveManyAsync(x => x.Id != ObjectId.Empty);
                    _azureRepository.RemoveManyAsync(x => x.Id != ObjectId.Empty);
                    _googleRepository.RemoveManyAsync(x => x.Id != ObjectId.Empty);
                    _adRepository.RemoveManyAsync(x => x.Id != ObjectId.Empty);
                    return Ok("Data removed from DB");
                }
                return Ok("Password Not Matched");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }

    }
}
