using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Pursuit.Context;
using Pursuit.Helpers;
using Pursuit.Model;
using Pursuit.Utilities;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using System.Runtime;
using System.Security.Claims;
using System.Security.Policy;
using System.Text;
using static System.Net.WebRequestMethods;
/* =========================================================
    Item Name: API's Related to User - UsersController
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.API.Controllers.v1
{

    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiController]

    public class UsersController : ControllerBase
    {
        private readonly IPursuitRepository<User> _userRepository;
        private readonly IPursuitRepository<Role> _roleRepository;
        private static readonly FormOptions _defaultFormOptions = new FormOptions();
        private readonly ILogger<UsersController> _logger;
        private readonly IConfiguration _config;
        private readonly long _fileSizeLimit;
        private readonly string[] _permittedExtensions = { ".xls", ".xlsx", ".csv", ".txt" };


        public UsersController(IPursuitRepository<User> userRepository,
            IPursuitRepository<Role> roleRepository,
            ILogger<UsersController> logger, IConfiguration config)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _logger = logger;
            _config = config;
            _fileSizeLimit = long.Parse(_config["FileSizeLimit"]);
        }
        //API used to register user by themselve
        [HttpPost("AnotherFromV1API")]

        public async Task<IActionResult> ThisisAV1Method()
        {
            return Ok("Calling API version 1.0, coming from here api/v1");
        }
        [HttpPost("GetBuildDetails")]
        public async Task<IActionResult> GetBuildDetails()
        {
            var aspversion = "6.0v";
            var version = "1.0";

            Assembly execAssembly = Assembly.GetEntryAssembly();
            var creationTime = new FileInfo(execAssembly.Location).CreationTime;

            var obj = new
            {
                AspDotNetVersion = aspversion,
                APIVersion = version,
                APIBuildTimeStamp = creationTime
            };
            return Ok(obj);
        }
    }
}
