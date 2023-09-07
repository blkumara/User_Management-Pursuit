using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Admin.Directory.directory_v1.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Newtonsoft.Json.Converters;
using OfficeOpenXml.Packaging.Ionic.Zlib;
using Pursuit.Context;
using Pursuit.Context.AD;
using Pursuit.Helpers;
using Pursuit.Model;
using System.Dynamic;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json.Nodes;
using AdminDir = Google.Apis.Admin.Directory;
using Role = Pursuit.Model.Role;

namespace Pursuit.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]

    [ApiController]
    [Authorize]

    public class GWSController : ControllerBase
    {
        private readonly IADRepository<ADRecord> _gRepository;
        private readonly ILogger<GWSController> _logger;

        readonly string url = "";
        public GWSController(ServiceResolver serviceResolver, ILogger<GWSController> logger)
        {
            _gRepository = serviceResolver("GWS");
            _logger = logger;
        }


        [HttpPost("googleUsersFiltered")]
        public async Task<IActionResult> gUsersFiltered([FromBody] ICollection<gFilterRequest> filterList, [FromQuery] PaginationParameters paginationParameters)
        {
            try
            {
                var users = _gRepository.MultiFilter(filterList);
                if (users.Count() <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Not Found" });
                }
                var pNumb = int.Parse(paginationParameters.PageNumber);
                var pSize = int.Parse(paginationParameters.PageSize);
                var totalCount = users.Count();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pSize);
                var items = users.Skip((pNumb - 1) * pSize)
                                 .Take(pSize)
                                 .ToList();

                // Return the paginated result
                var result = new
                {
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    Data = items
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Filtering Google Users" });
                throw ex;
            }
        }
        [HttpPost("importGoogleUsers")]
        public async Task<IActionResult> GetGoogleUsers([FromBody] Connection cs)
        {
            try
            {



                byte[] data = Convert.FromBase64String(cs.private_key);
                string decodedString = Encoding.UTF8.GetString(data);
                cs.private_key = decodedString;

                IList<AdminDir.directory_v1.Data.User> gUserList;

                // var userName = "admin@evolveaccess.com";

                ServiceAccountCredential credential;
                var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(cs);
                using (var stream = new MemoryStream(Encoding.Default.GetBytes(jsonString)))
                {
                    credential = GoogleCredential.FromStream(stream).CreateScoped(
                     new string[] { @"https://www.googleapis.com/auth/admin.directory.user" })
                                .CreateWithUser(cs.Connection_User_Name)
                                .UnderlyingCredential as ServiceAccountCredential;
                }


                List<ADRecord> _users = new List<ADRecord>();
                using (var directoryService = DirectoryServiceFactory.CreateDirectoryService(credential))
                {
                    //Retrieves a paginated list of either deleted users or all users in a domain.
                    var request = directoryService.Users.List();
                    var expConverter = new ExpandoObjectConverter();

                    // request.Domain = "evolveaccess.com";
                    request.Domain = cs.Domain_Name;
                    request.ViewType = UsersResource.ListRequest.ViewTypeEnum.AdminView;
                    var response = request.Execute();

                    BulkUser usr;
                    dynamic varJson;

                    foreach (var user in response.UsersValue)
                    {

                        varJson = Newtonsoft.Json.JsonConvert.SerializeObject(user);

                        //varHelo.Id = Guid.NewGuid();
                        ADRecord gRec = new ADRecord();

                        gRec.Email = user.PrimaryEmail ?? "";
                        try
                        {
                            gRec.Phone = Convert.ToString(user.Phones[0]);
                        }
                        catch { }
                        gRec.FirstName = user.Name.GivenName ?? "";
                        gRec.LastName = user.Name.FamilyName ?? "";
                        gRec.user_origin_code = "Google";
                        //Adding empty role setails
                        AccessRule rule = new AccessRule();
                        rule.Feature = "View";
                        rule.Access = true;
                        Role role1 = new Role();
                        role1.RoleName = "";
                        role1.AccessRules.Add(rule);
                        gRec.Role = role1;
                        gRec.UserDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(varJson, expConverter);

                        _users.Add(gRec);
                    }
                }

                await _gRepository.UpsertManyAsync(_users);
                return Ok(new { ResponseCode = "200", ResponseMessege = "Google WS Data Was Imported Successfully." });

                //  return Ok(_users);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Google WS Data Import Failed." });

            }

        }

        [HttpPost("verifyGoogleAD")]
        public async Task<IActionResult> VerifyGoogleAD([FromBody] Connection cs)
        {
            try
            {

                byte[] data = Convert.FromBase64String(cs.private_key);
                string decodedString = Encoding.UTF8.GetString(data);
                Console.WriteLine(decodedString);
                cs.private_key = decodedString;


                GoogleCredential credential;
                var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(cs);
                using (var stream = new MemoryStream(Encoding.Default.GetBytes(jsonString)))
                {
                    credential = GoogleCredential.FromStream(stream);
                }
                credential = credential.CreateScoped(new[] {
                "https://www.googleapis.com/auth/analytics.readonly" });


                string bearer = "";
                try
                {
                    Task<string> task = ((ITokenAccess)credential).GetAccessTokenForRequestAsync();
                    task.Wait();
                    bearer = task.Result;
                    return Ok(new { ResponseCode = "200", ResponseMessege = "Google WS Connection Tested Successfully." });

                }


                catch (AggregateException ex)
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "Can Not Test the Google WS Connection" });

                    throw ex.InnerException;
                }

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Google WS Connection Test Failed" });

            }


        }


        [HttpPost("SyncGoogleUsersDaily")]
        public async Task<IActionResult> SyncGoogleUsersDaily([FromBody] Connection cs)
        {
            try
            {
                IList<AdminDir.directory_v1.Data.User> gUserList;

                // var userName = "admin@evolveaccess.com";

                ServiceAccountCredential credential;
                var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(cs);
                using (var stream = new MemoryStream(Encoding.Default.GetBytes(jsonString)))
                {
                    credential = GoogleCredential.FromStream(stream).CreateScoped(
                     new string[] { @"https://www.googleapis.com/auth/admin.directory.user" })
                                .CreateWithUser(cs.Connection_User_Name)
                                .UnderlyingCredential as ServiceAccountCredential;
                }

                List<ADRecord> _users = new List<ADRecord>();
                using (var directoryService = DirectoryServiceFactory.CreateDirectoryService(credential))
                {
                    //Retrieves a paginated list of either deleted users or all users in a domain.
                    var request = directoryService.Users.List();
                    var expConverter = new ExpandoObjectConverter();

                    // request.Domain = "evolveaccess.com";
                    request.Domain = cs.Domain_Name;
                    request.ViewType = UsersResource.ListRequest.ViewTypeEnum.AdminView;
                    var response = request.Execute();

                    BulkUser usr;
                    dynamic varJson;

                    foreach (var user in response.UsersValue)
                    {

                        varJson = Newtonsoft.Json.JsonConvert.SerializeObject(user);

                        //varHelo.Id = Guid.NewGuid();
                        ADRecord gRec = new ADRecord();
                        gRec.Email = user.PrimaryEmail ?? "";
                        gRec.UserDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(varJson, expConverter);

                        _users.Add(gRec);
                    }
                }

                await _gRepository.UpsertManyAsync(_users);
                return Ok("Users Imported");

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }

        }

    }

}
