using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Pursuit.Context.AD;
using Pursuit.Context;
using Pursuit.Helpers;
using Pursuit.Model;
using System.Collections;
using System.Net.Http.Headers;
using Azure;
using Microsoft.AspNetCore.Authorization;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Azure.Identity;
using static Google.Apis.Requests.BatchRequest;

namespace Pursuit.API.Controllers
{



    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]

    [ApiController]
    [Authorize]
    public class AzureController : ControllerBase
    {
        private readonly IADRepository<ADRecord> _gRepository;
        private readonly ILogger<AzureController> _logger;

        readonly string url = "";
        public AzureController(ServiceResolver serviceResolver, ILogger<AzureController> logger)
        {
            _gRepository = serviceResolver("AZ");
            _logger = logger;
        }

        [HttpPost("azureUsersFiltered")]
        public async Task<IActionResult> AzureUsersFiltered([FromBody] ICollection<gFilterRequest> filterList, [FromQuery] PaginationParameters paginationParameters)
        {
            try
            {
                var users = _gRepository.MultiFilter(filterList);

                //Console.WriteLine(users.Count());
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

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Filtering Azure AD Users" });

            }
        }

        [HttpPost("importAzureUsers")]
        public async Task<IActionResult> GetAzureUsers([FromBody] Connection cs)
        {
            try
            {

                BulkUser usr;
                dynamic varJson;
                List<ADRecord> _users = new List<ADRecord>();
                var expConverter = new ExpandoObjectConverter();


                // The client credentials flow requires that you request the
                // /.default scope, and preconfigure your permissions on the
                // app registration in Azure. An administrator must grant consent
                // to those permissions beforehand.
                var scopes = new[] { ".default" };
                string tenantId = "";
                // Multi-tenant apps can use "common",
                // single-tenant apps must use the tenant ID from the Azure portal
                if (cs.TenantName != null && cs.TenantName != "")
                {
                    tenantId = cs.TenantName;

                }
                else
                {
                    tenantId = cs.TenantId;
                }


                // Values from app registration
                var clientId = cs.ApplicationId;
                var clientSecret = cs.Client_Secret;

                // using Azure.Identity;
                var options = new TokenCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                };

                // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
                var clientSecretCredential = new ClientSecretCredential(
                    tenantId, clientId, clientSecret, options);

                var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

                var response = graphClient.Users.GetAsync((requestConfiguration) =>
                {
                    //  requestConfiguration.QueryParameters.Count = true;


                    requestConfiguration.QueryParameters.Expand = new string[] { "Manager" };
                    requestConfiguration.QueryParameters.Select = new string[] { "Id", "companyName", "department", "createdDateTime", "Mail", "MobilePhone", "GivenName", "Surname", "LastPasswordChangeDateTime" };
                    // requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                });

                foreach (var user in response.Result.Value)
                {
                    if (user.Mail != null && user.Mail != "")
                    {
                        varJson = Newtonsoft.Json.JsonConvert.SerializeObject(user);

                        ADRecord gRec = new ADRecord();

                        gRec.Email = user.Mail ?? "";
                        gRec.Phone = user.MobilePhone ?? "";
                        gRec.FirstName = user.GivenName ?? "";
                        gRec.LastName = user.Surname ?? "";

                        gRec.user_origin_code = "AZAD";
                        //Adding empty role setails
                        AccessRule rule = new AccessRule();
                        rule.Feature = "View";
                        rule.Access = true;
                        Role role1 = new Role();
                        role1.RoleName = "";
                        role1.AccessRules.Add(rule);
                        gRec.Role = role1;
                        gRec.AzureId = user.Id;
                        // gRec.UserDocument = document;
                        gRec.UserDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(varJson, expConverter);

                        _users.Add(gRec);
                    }
                }


                await _gRepository.UpsertManyAsync(_users);
                return Ok(new { ResponseCode = "200", ResponseMessege = "Azure AD Data Was Imported Successfully." });
                // return Ok(_users);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Azure AD Import Failed." });

            }
        }


        [HttpPost("verifyAzureAD")]
        public async Task<IActionResult> VerifyAzureAD([FromBody] Connection cs)
        {
            try
            {
                string tenantId = "";

                if (cs.TenantName != null && cs.TenantName != "")
                {
                    tenantId = cs.TenantName;

                }
                else
                {
                    tenantId = cs.TenantId;
                }

                IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                        .Create(cs.ApplicationId)
                        .WithTenantId(tenantId)
                        .WithClientSecret(cs.Client_Secret)
                        .Build();

                AuthenticationResult result;

                string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

                result = await confidentialClientApplication.AcquireTokenForClient(scopes).ExecuteAsync();




                if (result.AccessToken != null || result.AccessToken != "")

                    return Ok(new { ResponseCode = "200", ResponseMessege = "Azure AD Connection Tested Successfully" });

                else
                    return Ok(new { ErrorCode = "409", ErrorMessege = "Can Not Test the Azure AD Connection" });


            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Azure AD Connection Test Failed" });

            }
        }


        [HttpPost("SyncAzureUsersDaily")]
        public async Task<IActionResult> SyncAzureUsersDaily([FromBody] Connection cs)
        {


            BulkUser usr;
            dynamic varJson;
            List<ADRecord> _users = new List<ADRecord>();
            var expConverter = new ExpandoObjectConverter();


            // The client credentials flow requires that you request the
            // /.default scope, and preconfigure your permissions on the
            // app registration in Azure. An administrator must grant consent
            // to those permissions beforehand.
            var scopes = new[] { ".default" };

            // Multi-tenant apps can use "common",
            // single-tenant apps must use the tenant ID from the Azure portal
            var tenantId = cs.TenantId;

            // Values from app registration
            var clientId = cs.ApplicationId;
            var clientSecret = cs.Client_Secret;

            // using Azure.Identity;
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, options);

            var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

            var response = graphClient.Users.GetAsync((requestConfiguration) =>
            {
                //  requestConfiguration.QueryParameters.Count = true;
                //requestConfiguration.QueryParameters.Select = new string[] { "id", "displayName", "mail" };
                requestConfiguration.QueryParameters.Expand = new string[] { "Manager" };
                // requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
            });

            foreach (var user in response.Result.Value)
            {
                varJson = Newtonsoft.Json.JsonConvert.SerializeObject(user);

                //  dynamic document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<MongoDB.Bson.BsonDocument>(varJson) as ExpandoObject;
                //varHelo.Id = Guid.NewGuid();
                ADRecord gRec = new ADRecord();
                gRec.Email = user.Mail ?? "";
                //                gRec.UserDocument = document;
                gRec.UserDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(varJson, expConverter);

                _users.Add(gRec);
            }


            await _gRepository.UpsertManyAsync(_users);
            return Ok("Users Imported");

        }


    }
}
