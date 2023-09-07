using Microsoft.AspNetCore.Mvc;
using Pursuit.Context;
using Pursuit.Model;
using System.Dynamic;
using Pursuit.Helpers;
using Newtonsoft.Json.Converters;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Primitives;
using Microsoft.Kiota.Abstractions.Authentication;
using static System.Net.WebRequestMethods;

using Microsoft.Kiota.Abstractions;
using Microsoft.Graph.Models;
using System;

using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using System.Text;
using Pursuit.Context.AD;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Diagnostics;
using System.Globalization;
using ObjectsComparer;
using Pursuit.Utilities;
using Azure;
using Google.Apis.Admin.Directory.directory_v1.Data;
using Tavis.UriTemplates;
using MongoDB.Bson;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Drawing.Printing;
using static Google.Apis.Requests.BatchRequest;
using Newtonsoft.Json.Linq;
using User = Pursuit.Model.User;
using static Pursuit.Utilities.Enums;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.DirectoryServices.AccountManagement;

/* =========================================================
Item Name: API's Related to AD - ADController
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

    public class DeltaManagerController : ControllerBase
    {
        private readonly IADRepository<ADRecord> _adRepository;
        private readonly ILogger<MSController> _logger;
        private readonly IADRepository<ADRecord> _azureRepository;
        private readonly IPursuitRepository<Admin_Configuration> _adminconfigRepository;
        private readonly IADRepository<ADRecord> _gRepository;
        private ADRecordComparersFactory _factory;
        private ObjectsComparer.IComparer<ADRecord> _comparer;
        private readonly IDeltaRepository<DeltaModel> _addeltaRepository;
        private readonly IDeltaRepository<DeltaModel> _azuredelatRepository;
        private readonly IDeltaRepository<DeltaModel> _gdeltaRepository;

        private readonly IPursuitRepository<User> _userRepository;

        public DeltaManagerController(ServiceResolver serviceResolver, DeltaServiceResolver deltaServiceResolver, IPursuitRepository<User> userRepository,
           ILogger<MSController> logger, IConfiguration config, IPursuitRepository<Admin_Configuration> adminconfigRepository)
        {
            _adRepository = serviceResolver("MS");
            _gRepository = serviceResolver("GWS");
            _azureRepository= serviceResolver("AZ");
            _addeltaRepository = deltaServiceResolver("MSDelta");
            _gdeltaRepository = deltaServiceResolver("GWSDelta");
            _azuredelatRepository = deltaServiceResolver("AZDelta");
            _adminconfigRepository = adminconfigRepository;
            _logger = logger;
            _userRepository = userRepository;
        }

        [HttpPost(@"Google/getDelta")]
        public async Task<IActionResult> getDeltaGoogle([FromBody] Connection cs)
        {
            try
            {
                /*
                                byte[] data = Convert.FromBase64String(cs.private_key);
                                string decodedString = Encoding.UTF8.GetString(data);
                                cs.private_key = decodedString;*/

                IList<Google.Apis.Admin.Directory.directory_v1.Data.User> gUserList;
                IList<object> delta = new List<object>();
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
                DeltaModel _deltamodel = new DeltaModel();
                List<ADRecord> _users = new List<ADRecord>();
                dynamic varJson;

                var expConverter = new ExpandoObjectConverter();
                Users response = new Users();
                using (var directoryService = DirectoryServiceFactory.CreateDirectoryService(credential))
                {
                    //Retrieves a paginated list of either deleted users or all users in a domain.
                    var request = directoryService.Users.List();
                   // request.Domain = "evolveaccess.com";
                    request.Domain = cs.Domain_Name;
                    request.ViewType = UsersResource.ListRequest.ViewTypeEnum.AdminView;
                    //request.ShowDeleted = "true";
                     response = request.Execute();

                    BulkUser usr;
                   
                    var dbUsers = _gRepository.gwsgetallemp();

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

                        gRec.UserDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(varJson, expConverter);

                        var matchedUser = dbUsers.FirstOrDefault(x => x.Email == user.PrimaryEmail);

                        if (matchedUser != null)
                        {
                            _factory = new ADRecordComparersFactory();
                            _comparer = _factory.GetObjectsComparer<ADRecord>();
                            IEnumerable<Difference> differences;

                            _comparer.Compare(gRec, matchedUser, out differences);

                            if (differences.Count() > 0)
                            {
                                // string diffJson = Newtonsoft.Json.JsonConvert.SerializeObject(differences);
                                // gRec.Differences = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(diffJson, expConverter);
                                delta.Add(gRec);
                            }
                        }
                        else
                        { delta.Add(gRec); }

                        //Adding empty role setails
                        AccessRule rule = new AccessRule();
                        rule.Feature = "View";
                        rule.Access = true;
                        Model.Role role1 = new Model.Role();
                        role1.RoleName = "";
                        role1.AccessRules.Add(rule);
                        gRec.Role = role1;
                        _users.Add(gRec);

                    }
                }
                await _gRepository.UpsertManyAsync(_users);

                using (var directoryService = DirectoryServiceFactory.CreateDirectoryService(credential))
                {
                    var request2 = directoryService.Users.List();
                 
                    request2.Domain = cs.Domain_Name;
                    request2.ViewType = UsersResource.ListRequest.ViewTypeEnum.AdminView;
                    request2.ShowDeleted = "true";
                    var response2 = request2.Execute();
                    string id,email;
                    if (response2.UsersValue != null)
                    {
                        foreach (var user in response2.UsersValue)
                        {
                            id = user.Id;
                            email = user.PrimaryEmail;
                            var appuser = await Task.Run(() => _userRepository.FilterBy(a => a.Email == email).AsQueryable().FirstOrDefault());
                            if (appuser != null)
                            {
                                await _userRepository.UpdateStatusAsync(appuser.Id.ToString(), "DEACTIVE");
                            }
                            if (_gRepository.RemoveAsync(id))
                            {
                                delta.Add(getRecFromAD(user));
                            }
                        }
                    }
                }


                // Here store delta to the new Collection GWS_Delta
                varJson = Newtonsoft.Json.JsonConvert.SerializeObject(delta);
                _deltamodel.Value = Newtonsoft.Json.JsonConvert.DeserializeObject<ICollection<ExpandoObject>>(varJson, expConverter);
                _deltamodel.PrevUpdateDate=DateTime.Now;
               

                await _gdeltaRepository.InsertOneAsync(_deltamodel);

                

                return Ok(_users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });
            }
        }

        [HttpPost(@"Azure/deltaInitiate")]
        public async Task<IActionResult> getInitialAzure([FromBody] Connection cs)
        {

            try
            {


                dynamic varJson;

                var expConverter = new ExpandoObjectConverter();

                List<ADRecord> _users = new List<ADRecord>();
                DeltaModel _delta = new DeltaModel();
                   try
                {
                    var scopes = new[] { @"https://graph.microsoft.com/.default" };
                    // Multi-tenant apps can use "common",
                    // single-tenant apps must use the tenant ID from the Azure portal
                    string tenantId = "";
                    if (cs.TenantName != null && cs.TenantName != "")
                    {
                         tenantId = cs.TenantName;

                    }
                    else
                    {
                        tenantId = cs.TenantId;
                    }
                    var clientSecret = cs.Client_Secret;
                    // Value from app registration
                    var clientId = cs.ApplicationId; 

                    // using Azure.Identity;
                    var options = new UsernamePasswordCredentialOptions
                    {
                        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                    };

                    var clientSecretCredential = new ClientSecretCredential(
                    tenantId, clientId, clientSecret, options);

                    var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

                    var result = await graphClient.Users.Delta.GetAsync((requestConfiguration) =>
                    {
                        requestConfiguration.Headers.Add("Prefer", "return=minimal");

                    });

                    //Store result in ADData > collection Azure_AD
                    foreach (var user in result.Value)
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
                        Model.Role role1 = new Model.Role();
                        role1.RoleName = "";
                        role1.AccessRules.Add(rule);
                        gRec.Role = role1;
                        gRec.AzureId = user.Id;
                        gRec.UserDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(varJson, expConverter);
                        
                        _users.Add(gRec);
                    }
                    

                    await _azureRepository.UpsertManyAsync(_users);

                    //Create dummy record for delta with nextLink,PrevUpdateDate and IsNextLinkCalled flag in the result
                    varJson = Newtonsoft.Json.JsonConvert.SerializeObject(result.Value);
                    _delta.Value= Newtonsoft.Json.JsonConvert.DeserializeObject<ICollection<ExpandoObject>>(varJson, expConverter);
                    _delta.NextLink = result.OdataNextLink;
                    _delta.IsNextLinkCalled = false;
                    _delta.PrevUpdateDate = DateTime.Now;

                    _delta.ConnectionId = cs.ApplicationId;


                    //Store dummy record in ADData> collection Azure_Delta

                    await _azuredelatRepository.InsertOneAsync(_delta);

                    return Ok(result);
                }
                catch (Exception ex)
                {
                    throw;
                }



            }
            catch (Exception ex)
            {
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });
                throw ex;
            }
        }


        [HttpPost("Azure/getDelta")]
        public async Task<IActionResult> getDeltaAzure([FromBody] Connection cs)
        {
            try
            {

                dynamic varJson;

                var expConverter = new ExpandoObjectConverter();


                DeltaModel _delta = new DeltaModel();

                List<ADRecord> _users = new List<ADRecord>();
                try
                {

                    var scopes = new[] { @"https://graph.microsoft.com/.default" };
                    // Multi-tenant apps can use "common",
                    // single-tenant apps must use the tenant ID from the Azure portal
                    string tenantId = "";
                    if (cs.TenantName != null && cs.TenantName != "")
                    {
                        tenantId = cs.TenantName;

                    }
                    else
                    {
                        tenantId = cs.TenantId;
                    }
                    var clientSecret = cs.Client_Secret;
                    // Value from app registration
                    var clientId = cs.ApplicationId;

                    // using Azure.Identity;
                    var options = new UsernamePasswordCredentialOptions
                    {
                        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                    };



                    var clientSecretCredential = new ClientSecretCredential(
                    tenantId, clientId, clientSecret, options);

                    var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

                    var delta1 = _azuredelatRepository.FilterBy(x => x.Id != ObjectId.Empty);

                    //fetch nextlink from recent(decnding sorts by PrevUpdateDate) Azure_delta record where the flag is false
                    var nextLink = _azuredelatRepository.FilterBy(x => x.Id != ObjectId.Empty).OrderByDescending(a => a.PrevUpdateDate)
                        .FirstOrDefault(x => x.IsNextLinkCalled == false && x.ConnectionId==cs.ApplicationId)?.NextLink;

                    var deltaId = _azuredelatRepository.FilterBy(x => x.Id != ObjectId.Empty).OrderByDescending(a => a.PrevUpdateDate)
                        .FirstOrDefault(x => x.IsNextLinkCalled == false && x.ConnectionId == cs.ApplicationId)?.Id;
                   

                    string skipToken = nextLink[(nextLink.IndexOf("$skiptoken=") + "$skiptoken=".Length)..];

                    var requestInformation = graphClient.Users.Delta.ToGetRequestInformation((requestConfiguration) =>
                    {
                        requestConfiguration.QueryParameters.Count = true;
                    });
                    requestInformation.UrlTemplate = requestInformation.UrlTemplate.Insert(requestInformation.UrlTemplate.Length - 1, ",%24skiptoken");

                    requestInformation.QueryParameters.Add("%24skiptoken", skipToken);

                    var result2 = await graphClient.RequestAdapter.SendAsync(requestInformation, UserCollectionResponse.CreateFromDiscriminatorValue);
                    string azureid = "";
                    foreach (var user in result2.Value)
                    {
                        if (user.AdditionalData.Count()>0)
                        {
                            
                            azureid = user.Id;
                            _azureRepository.RemoveAsync(azureid);
                            var azureuser= await Task.Run(() => _azureRepository.FilterBy(a => a.AzureId == azureid).AsQueryable().FirstOrDefault());

                            if (azureuser!=null)
                            {
                                var appuser = await Task.Run(() => _userRepository.FilterBy(a => a.Email == azureuser.Email).AsQueryable().FirstOrDefault());
                                if (appuser != null)
                                {
                                    await _userRepository.UpdateStatusAsync(appuser.Id.ToString(), "DEACTIVE");
                                }  
                            }
                        }
                    }

                        //Store result2 into ADData> Azure_Delta collection
                        var Id = ObjectId.GenerateNewId();
                    _delta.Id = Id;

                    varJson = Newtonsoft.Json.JsonConvert.SerializeObject(result2.Value);
                    _delta.Value = Newtonsoft.Json.JsonConvert.DeserializeObject<ICollection<ExpandoObject>>(varJson, expConverter);

                    _delta.DeltaLink = result2.AdditionalData.Values.ElementAt(1).ToString();

                    _delta.IsNextLinkCalled = false;
                    _delta.PrevUpdateDate = DateTime.Now;
                    _delta.ConnectionId = cs.ApplicationId;


                    //Store dummy record in ADData> collection Azure_Delta

                    await _azuredelatRepository.InsertOneAsync(_delta);

                    //Changing old data flag to true as we used nextlink here
                    await _azuredelatRepository.UpdateFlagAsync(deltaId.ToString(), true);

                    //Call getFinalAzure() using the  deltaLink from result2
                    string DeltaLink = "";
                    DeltaLink= result2.AdditionalData.Values.ElementAt(1).ToString();
                    getFinalAzure(cs, Id.ToString(), DeltaLink);

                    return Ok(result2);

                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
                throw ex;
            }



        }

        private async void getFinalAzure(Connection cs,string id,string DeltaLink)
        {
                 dynamic varJson;

                 var expConverter = new ExpandoObjectConverter();


                 DeltaModel _delta = new DeltaModel();

                 List<ADRecord> _users = new List<ADRecord>();

                  var scopes = new[] { @"https://graph.microsoft.com/.default" };
                     // Multi-tenant apps can use "common",
                     // single-tenant apps must use the tenant ID from the Azure portal
                     string tenantId = "";
                     if (cs.TenantName != null && cs.TenantName != "")
                     {
                         tenantId = cs.TenantName;

                     }
                     else
                     {
                         tenantId = cs.TenantId;
                     }
                     var clientSecret = cs.Client_Secret;
                     // Value from app registration
                     var clientId = cs.ApplicationId;
                     // using Azure.Identity;
                     var options = new UsernamePasswordCredentialOptions
                     {
                         AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                     };



                     var clientSecretCredential = new ClientSecretCredential(
                     tenantId, clientId, clientSecret, options);

                     var graphClient = new GraphServiceClient(clientSecretCredential, scopes);
                
            string deltaToken = DeltaLink[(DeltaLink.IndexOf("$deltatoken=") + "$deltatoken=".Length)..];

            /*var requestInformationDelta = graphClient.Users.Delta.ToGetRequestInformation((requestConfiguration) =>
            {
                requestConfiguration.QueryParameters.Count = true;
            });*/
            var requestInformationDelta = graphClient.Users.Delta.ToGetRequestInformation((requestConfiguration) =>
                     {
                        
                         requestConfiguration.QueryParameters.Select = new string[] { "Id", "companyName", "department", "createdDateTime", "Mail", "MobilePhone", "GivenName", "Surname", "LastPasswordChangeDateTime" };

                     });
                     requestInformationDelta.UrlTemplate = requestInformationDelta.UrlTemplate.Insert(requestInformationDelta.UrlTemplate.Length - 1, "%24deltatoken");

                     requestInformationDelta.QueryParameters.Add("%24deltatoken", deltaToken);
            


            //Using to get the nextLink
            var result3 = graphClient.RequestAdapter.SendAsync(requestInformationDelta, UserCollectionResponse.CreateFromDiscriminatorValue);
            //Using to store in Azure_data
            var result4 = graphClient.Users.GetAsync((requestConfiguration) =>
            {
                requestConfiguration.QueryParameters.Expand = new string[] { "Manager" };
                requestConfiguration.QueryParameters.Select = new string[] { "Id", "companyName", "department", "createdDateTime", "Mail", "MobilePhone", "GivenName", "Surname", "LastPasswordChangeDateTime" };
                
            });
            //Store result4.value in ADData> Azure_AD collection
            foreach (var user in result4.Result.Value)
            {
                if (user.Mail!=null && user.Mail!="")
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
                    Model.Role role1 = new Model.Role();
                    role1.RoleName = "";
                    role1.AccessRules.Add(rule);
                    gRec.Role = role1;
                    gRec.AzureId = user.Id;
                    gRec.UserDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(varJson, expConverter);

                    _users.Add(gRec); 
                }
              }


                      await _azureRepository.UpsertManyAsync(_users);

                     //Update current delta(result2 of gettdelta api) with the nextLink from result3 with PrevUpdateDate=todaysdate,IsNextLinkCalled=false
                     var objectId = new ObjectId(id);
                     var delta = _azuredelatRepository.FilterBy(x => x.Id == objectId).FirstOrDefault();
                     _delta.Value = delta.Value;
                     _delta.DeltaLink = delta.DeltaLink;
                     _delta.PrevUpdateDate = DateTime.Now;
                     _delta.NextLink = result3.Result.OdataNextLink;
                     _delta.IsNextLinkCalled = false;

                     await _azuredelatRepository.ReplaceOneAsync(id, _delta);


                   
        }

        [HttpPost(@"MSAD/deltaInitiate")]
        public async Task<IActionResult> getInitialMSAD([FromBody] Connection cs)
        {
            try
            {
                string domainName = cs.Domain_Name;
                string userId = cs.Connection_User_Id;

                string password = cs.Connection_User_Pwd;
                //Hostname contains IP
                string hostName = cs.HostName;
                List<ADRecord> _users = new List<ADRecord>();
                DeltaModel _delta = new DeltaModel();
                dynamic varJson;

                var expConverter = new ExpandoObjectConverter();

                List<SearchResultEntry> srList=new List<SearchResultEntry>();
                try
                {
                    
                    NetworkCredential credentials = new NetworkCredential(userId, password);
                    LdapDirectoryIdentifier directoryIdentifier = new LdapDirectoryIdentifier(hostName);
                   
                    using (LdapConnection connection = new LdapConnection(directoryIdentifier, credentials, AuthType.Basic))

                    {
                        DateTime yesterDay = DateTime.Now.AddYears(-100);
                        string filter = "(&(objectCategory=person)(objectClass=user)(memberOf=*)(whenCreated>=" + yesterDay.ToString("yyyyMMddHHmmss.sZ") + "))";
                        string[] components = domainName.Split('.');
                        string dcString = "DC=" + string.Join(",DC=", components);
                        string baseDN = dcString;


                        string[] attribArray = { "*" };

                      srList = PerformPagedSearch(connection, baseDN, filter, attribArray);


                       // dynamic varJson;



                        foreach (SearchResultEntry result in srList)
                        {


                            varJson = Newtonsoft.Json.JsonConvert.SerializeObject(result);


                            ADRecord aDRec = new ADRecord();
                            try
                            {
                                aDRec.Email = Convert.ToString(result.Attributes["mail"][0]);
                                aDRec.Phone = Convert.ToString(result.Attributes["telephonenumber"][0]);
                                aDRec.FirstName = Convert.ToString(result.Attributes["givenname"][0]);
                                aDRec.LastName = Convert.ToString(result.Attributes["name"][0]);
                                aDRec.user_origin_code = "MSAD";
                                //Adding empty role setails
                                AccessRule rule = new AccessRule();
                                rule.Feature = "View";
                                rule.Access = true;
                                Model.Role role1 = new Model.Role();
                                role1.RoleName = "";
                                role1.AccessRules.Add(rule);
                                aDRec.Role = role1;
                                aDRec.description = Convert.ToString(result.Attributes["description"][0]);

                                aDRec.useraccountcontrol = Convert.ToString(result.Attributes["useraccountcontrol"][0]);
                                aDRec.countrycode = Convert.ToString(result.Attributes["countrycode"][0]);
                                aDRec.accountexpires = Convert.ToString(result.Attributes["accountexpires"][0]);
                                aDRec.employeeid = Convert.ToString(result.Attributes["employeeid"][0]);
                                aDRec.company = Convert.ToString(result.Attributes["company"][0]);
                                aDRec.samaccountname = Convert.ToString(result.Attributes["samaccountname"][0]);
                                aDRec.instancetype = Convert.ToString(result.Attributes["instancetype"][0]);
                                aDRec.l = Convert.ToString(result.Attributes["l"][0]);
                                aDRec.cn = Convert.ToString(result.Attributes["cn"][0]);
                                aDRec.codepage = Convert.ToString(result.Attributes["codepage"][0]);
                                aDRec.manager = Convert.ToString(result.Attributes["manager"][0]);
                                aDRec.lastlogoff = Convert.ToString(result.Attributes["lastlogoff"][0]);
                                aDRec.department = Convert.ToString(result.Attributes["department"][0]);
                                aDRec.initials = Convert.ToString(result.Attributes["initials"][0]);
                                aDRec.objectguid = Convert.ToString(result.Attributes["objectguid"][0]);
                                aDRec.logoncount = Convert.ToString(result.Attributes["logoncount"][0]);
                                aDRec.c = Convert.ToString(result.Attributes["c"][0]);
                                aDRec.badpwdcount = Convert.ToString(result.Attributes["badpwdcount"][0]);
                                aDRec.pwdlastset = Convert.ToString(result.Attributes["pwdlastset"][0]);
                                aDRec.memberof = Convert.ToString(result.Attributes["memberof"][0]);
                                aDRec.badpasswordtime = Convert.ToString(result.Attributes["badpasswordtime"][0]);
                                aDRec.objectsid = Convert.ToString(result.Attributes["objectsid"][0]);
                                aDRec.usnchanged = Convert.ToString(result.Attributes["usnchanged"][0]);
                                aDRec.st = Convert.ToString(result.Attributes["st"][0]);
                                aDRec.primarygroupid = Convert.ToString(result.Attributes["primarygroupid"][0]);
                                aDRec.objectcategory = Convert.ToString(result.Attributes["objectcategory"][0]);
                                aDRec.userprincipalname = Convert.ToString(result.Attributes["userprincipalname"][0]);
                                aDRec.streetaddress = Convert.ToString(result.Attributes["streetaddress"][0]);
                                aDRec.samaccounttype = Convert.ToString(result.Attributes["samaccounttype"][0]);
                                aDRec.lastlogon = Convert.ToString(result.Attributes["lastlogon"][0]);
                                aDRec.whencreated = Convert.ToString(result.Attributes["whencreated"][0]);
                                aDRec.distinguishedname = Convert.ToString(result.Attributes["distinguishedname"][0]);
                                aDRec.dscorepropagationdata = Convert.ToString(result.Attributes["dscorepropagationdata"][0]);
                                aDRec.displayname = Convert.ToString(result.Attributes["displayname"][0]);
                                aDRec.sn = Convert.ToString(result.Attributes["sn"][0]);
                                aDRec.usncreated = Convert.ToString(result.Attributes["usncreated"][0]);
                                aDRec.postalcode = Convert.ToString(result.Attributes["postalcode"][0]);
                                aDRec.objectclass = null;


                            }
                            catch { }

                            aDRec.UserDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(varJson, expConverter);

                            _users.Add(aDRec);
                        }
                    };
                }
                catch (Exception ex)
                {
                    throw;
                }

                //Changes this and store into MS_AD_Delta with PrevUpdateDate=today's date,flag=false

                varJson = Newtonsoft.Json.JsonConvert.SerializeObject(_users);
                _delta.Value = Newtonsoft.Json.JsonConvert.DeserializeObject<ICollection<ExpandoObject>>(varJson, expConverter);
                _delta.PrevUpdateDate = DateTime.Now;
                _delta.IsRecordUsed = false;
                await _addeltaRepository.InsertOneAsync(_delta);

                

                //Call msad Import API and store the data to MS_AD collection

                importMSAD(cs);

                return Ok(new { ResponseCode = "200", ResponseMessege = "Microsoft AD Delta Was Imported Successfully." });

            }
            catch (Exception ex)
            {
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });
                throw ex;
            }
        }
        
        [HttpPost(@"MSAD/getDelta")]
        public async Task<IActionResult> getDeltaMSAD([FromBody] Connection cs)
        {
            try
            {
                string domainName = cs.Domain_Name;
                string userId = cs.Connection_User_Id;

                string password = cs.Connection_User_Pwd;
                //Hostname contains IP
                string hostName = cs.HostName;
                List<ADRecord> _users = new List<ADRecord>();
                DeltaModel _delta = new DeltaModel();
                dynamic varJson;

                var expConverter = new ExpandoObjectConverter();

                List<SearchResultEntry> srList = new List<SearchResultEntry>();

                List<SearchResultEntry> deletedList = new List<SearchResultEntry>();
                try
                {

                    NetworkCredential credentials = new NetworkCredential(userId, password);
                    LdapDirectoryIdentifier directoryIdentifier = new LdapDirectoryIdentifier(hostName);

                    using (LdapConnection connection = new LdapConnection(directoryIdentifier, credentials, AuthType.Basic))

                    {
                        DateTime yesterDay = DateTime.Now.AddDays(-1);//Should be replaced from recent(decnding sorts by PrevUpdateDate) record's PrevUpdateDate of MS_AD_Delta

                        string filter = "(&(objectCategory=person)(objectClass=user)(memberOf=*)(whenChanged>=" + yesterDay.ToString("yyyyMMddHHmmss.sZ") + "))";
                        string[] components = domainName.Split('.');
                        string dcString = "DC=" + string.Join(",DC=", components);
                        string baseDN = dcString;


                        string[] attribArray = { "*" };

                        srList = PerformPagedSearch(connection, baseDN, filter, attribArray);


                        // dynamic varJson;



                        foreach (SearchResultEntry result in srList)
                        {
                            if (result.Attributes["mail"][0] != null && result.Attributes["mail"][0] != "")
                            {

                                varJson = Newtonsoft.Json.JsonConvert.SerializeObject(result);


                                ADRecord aDRec = new ADRecord();
                                try
                                {


                                    aDRec.Email = Convert.ToString(result.Attributes["mail"][0]);
                                    aDRec.Phone = Convert.ToString(result.Attributes["telephonenumber"][0]);
                                    aDRec.FirstName = Convert.ToString(result.Attributes["givenname"][0]);
                                    aDRec.LastName = Convert.ToString(result.Attributes["name"][0]);
                                    aDRec.user_origin_code = "MSAD";
                                    //Adding empty role setails
                                    AccessRule rule = new AccessRule();
                                    rule.Feature = "View";
                                    rule.Access = true;
                                    Model.Role role1 = new Model.Role();
                                    role1.RoleName = "";
                                    role1.AccessRules.Add(rule);
                                    aDRec.Role = role1;
                                    aDRec.description = Convert.ToString(result.Attributes["description"][0]);

                                    aDRec.useraccountcontrol = Convert.ToString(result.Attributes["useraccountcontrol"][0]);
                                    aDRec.countrycode = Convert.ToString(result.Attributes["countrycode"][0]);
                                    aDRec.accountexpires = Convert.ToString(result.Attributes["accountexpires"][0]);
                                    aDRec.employeeid = Convert.ToString(result.Attributes["employeeid"][0]);
                                    aDRec.company = Convert.ToString(result.Attributes["company"][0]);
                                    aDRec.samaccountname = Convert.ToString(result.Attributes["samaccountname"][0]);
                                    aDRec.instancetype = Convert.ToString(result.Attributes["instancetype"][0]);
                                    aDRec.l = Convert.ToString(result.Attributes["l"][0]);
                                    aDRec.cn = Convert.ToString(result.Attributes["cn"][0]);
                                    aDRec.codepage = Convert.ToString(result.Attributes["codepage"][0]);
                                    aDRec.manager = Convert.ToString(result.Attributes["manager"][0]);
                                    aDRec.lastlogoff = Convert.ToString(result.Attributes["lastlogoff"][0]);
                                    aDRec.department = Convert.ToString(result.Attributes["department"][0]);
                                    aDRec.initials = Convert.ToString(result.Attributes["initials"][0]);
                                    aDRec.objectguid = Convert.ToString(result.Attributes["objectguid"][0]);
                                    aDRec.logoncount = Convert.ToString(result.Attributes["logoncount"][0]);
                                    aDRec.c = Convert.ToString(result.Attributes["c"][0]);
                                    aDRec.badpwdcount = Convert.ToString(result.Attributes["badpwdcount"][0]);
                                    aDRec.pwdlastset = Convert.ToString(result.Attributes["pwdlastset"][0]);
                                    aDRec.memberof = Convert.ToString(result.Attributes["memberof"][0]);
                                    aDRec.badpasswordtime = Convert.ToString(result.Attributes["badpasswordtime"][0]);
                                    aDRec.objectsid = Convert.ToString(result.Attributes["objectsid"][0]);
                                    aDRec.usnchanged = Convert.ToString(result.Attributes["usnchanged"][0]);
                                    aDRec.st = Convert.ToString(result.Attributes["st"][0]);
                                    aDRec.primarygroupid = Convert.ToString(result.Attributes["primarygroupid"][0]);
                                    aDRec.objectcategory = Convert.ToString(result.Attributes["objectcategory"][0]);
                                    aDRec.userprincipalname = Convert.ToString(result.Attributes["userprincipalname"][0]);
                                    aDRec.streetaddress = Convert.ToString(result.Attributes["streetaddress"][0]);
                                    aDRec.samaccounttype = Convert.ToString(result.Attributes["samaccounttype"][0]);
                                    aDRec.lastlogon = Convert.ToString(result.Attributes["lastlogon"][0]);
                                    aDRec.whencreated = Convert.ToString(result.Attributes["whencreated"][0]);
                                    aDRec.distinguishedname = Convert.ToString(result.Attributes["distinguishedname"][0]);
                                    aDRec.dscorepropagationdata = Convert.ToString(result.Attributes["dscorepropagationdata"][0]);
                                    aDRec.displayname = Convert.ToString(result.Attributes["displayname"][0]);
                                    aDRec.sn = Convert.ToString(result.Attributes["sn"][0]);
                                    aDRec.usncreated = Convert.ToString(result.Attributes["usncreated"][0]);
                                    aDRec.postalcode = Convert.ToString(result.Attributes["postalcode"][0]);
                                    aDRec.objectclass = null;


                                }
                                catch { }

                                aDRec.UserDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(varJson, expConverter);

                                _users.Add(aDRec);
                            }

                        }
                        //find deleted records
                        string deletefilter = "(&(isDeleted=TRUE)(objectclass=user))";

                        deletedList = PerformPagedSearch(connection, baseDN, deletefilter, attribArray);
                        string email = "";
                        foreach (SearchResultEntry result in deletedList)
                        {
                            email = Convert.ToString(result.Attributes["mail"][0]);
                            _adRepository.RemoveAsync(email);
                            var appuser = await Task.Run(() => _userRepository.FilterBy(a => a.Email == email).AsQueryable().FirstOrDefault());
                            if (appuser != null)
                            {
                                await _userRepository.UpdateStatusAsync(appuser.Id.ToString(), "DEACTIVE");
                            }
                           
                        }
                    };
                }
                catch (Exception ex)
                {
                    throw;
                }

                
               //Changes this and store into MS_AD_Delta with PrevUpdateDate=today's date,flag=false

                    varJson = Newtonsoft.Json.JsonConvert.SerializeObject(_users);
                _delta.Value = Newtonsoft.Json.JsonConvert.DeserializeObject<ICollection<ExpandoObject>>(varJson, expConverter);
                _delta.PrevUpdateDate = DateTime.Now;
                _delta.IsRecordUsed = false;
                await _addeltaRepository.InsertOneAsync(_delta);

                //Should update recordUsed flag of recent(decnding sorts by PrevUpdateDate) record to true of MS_AD_Delta
                var deltaId = _addeltaRepository.FilterBy(x => x.Id != ObjectId.Empty).OrderByDescending(a => a.PrevUpdateDate)
                      .FirstOrDefault(x => x.IsRecordUsed == false)?.Id;

                await _addeltaRepository.UpdateFlagAsync(deltaId.ToString(), true);


                //Call msad Import API and store the data to MS_AD collection

                importMSAD(cs);

                return Ok(new { ResponseCode = "200", ResponseMessege = "Microsoft AD Delta Was Imported Successfully." });

            }
            catch (Exception ex)
            {
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });
                throw ex;
            }
        }
        private async void importMSAD(Connection cs)
        {

            try
            {
                string domainName = cs.Domain_Name;
                string userId = cs.Connection_User_Id;

                string password = cs.Connection_User_Pwd;
                //Hostname contains IP
                string hostName = cs.HostName;
                List<ADRecord> _users = new List<ADRecord>();
                DateTime dNow = DateTime.Now.AddDays(-1);

                string strYest = dNow.Year.ToString() + dNow.Month.ToString("00") + dNow.Day.ToString("00") + "00000.0-0500";

                try
                {
                    NetworkCredential credentials = new NetworkCredential(userId, password);
                    LdapDirectoryIdentifier directoryIdentifier = new LdapDirectoryIdentifier(hostName);
                    //LdapDirectoryIdentifier directoryIdentifier = new LdapDirectoryIdentifier(domainName);
                    using (LdapConnection connection = new LdapConnection(directoryIdentifier, credentials, AuthType.Basic))

                    {



                        string filter = "(&(objectCategory=person)(objectClass=user)(memberOf=*))";
                        string[] components = domainName.Split('.');
                        string dcString = "DC=" + string.Join(",DC=", components);
                        string baseDN = dcString;


                        string[] attribArray = { "*" };

                        List<SearchResultEntry> srList = PerformPagedSearch(connection, baseDN, filter, attribArray);


                        var expConverter = new ExpandoObjectConverter();
                        dynamic varJson;



                        foreach (SearchResultEntry result in srList)
                        {


                            varJson = Newtonsoft.Json.JsonConvert.SerializeObject(result);


                            ADRecord aDRec = new ADRecord();
                            try
                            {
                                aDRec.Email = Convert.ToString(result.Attributes["mail"][0]);
                                aDRec.Phone = Convert.ToString(result.Attributes["telephonenumber"][0]);
                                aDRec.FirstName = Convert.ToString(result.Attributes["givenname"][0]);
                                aDRec.LastName = Convert.ToString(result.Attributes["name"][0]);
                                aDRec.user_origin_code = "MSAD";
                                //Adding empty role setails
                                AccessRule rule = new AccessRule();
                                rule.Feature = "View";
                                rule.Access = true;
                                Model.Role role1 = new Model.Role();
                                role1.RoleName = "";
                                role1.AccessRules.Add(rule);
                                aDRec.Role = role1;
                                aDRec.description = Convert.ToString(result.Attributes["description"][0]);

                                aDRec.useraccountcontrol = Convert.ToString(result.Attributes["useraccountcontrol"][0]);
                                aDRec.countrycode = Convert.ToString(result.Attributes["countrycode"][0]);
                                aDRec.accountexpires = Convert.ToString(result.Attributes["accountexpires"][0]);
                                aDRec.employeeid = Convert.ToString(result.Attributes["employeeid"][0]);
                                aDRec.company = Convert.ToString(result.Attributes["company"][0]);
                                aDRec.samaccountname = Convert.ToString(result.Attributes["samaccountname"][0]);
                                aDRec.instancetype = Convert.ToString(result.Attributes["instancetype"][0]);
                                aDRec.l = Convert.ToString(result.Attributes["l"][0]);
                                aDRec.cn = Convert.ToString(result.Attributes["cn"][0]);
                                aDRec.codepage = Convert.ToString(result.Attributes["codepage"][0]);
                                aDRec.manager = Convert.ToString(result.Attributes["manager"][0]);
                                aDRec.lastlogoff = Convert.ToString(result.Attributes["lastlogoff"][0]);
                                aDRec.department = Convert.ToString(result.Attributes["department"][0]);
                                aDRec.initials = Convert.ToString(result.Attributes["initials"][0]);
                                aDRec.objectguid = Convert.ToString(result.Attributes["objectguid"][0]);
                                aDRec.logoncount = Convert.ToString(result.Attributes["logoncount"][0]);
                                aDRec.c = Convert.ToString(result.Attributes["c"][0]);
                                aDRec.badpwdcount = Convert.ToString(result.Attributes["badpwdcount"][0]);
                                aDRec.pwdlastset = Convert.ToString(result.Attributes["pwdlastset"][0]);
                                aDRec.memberof = Convert.ToString(result.Attributes["memberof"][0]);
                                aDRec.badpasswordtime = Convert.ToString(result.Attributes["badpasswordtime"][0]);
                                aDRec.objectsid = Convert.ToString(result.Attributes["objectsid"][0]);
                                aDRec.usnchanged = Convert.ToString(result.Attributes["usnchanged"][0]);
                                aDRec.st = Convert.ToString(result.Attributes["st"][0]);
                                aDRec.primarygroupid = Convert.ToString(result.Attributes["primarygroupid"][0]);
                                aDRec.objectcategory = Convert.ToString(result.Attributes["objectcategory"][0]);
                                aDRec.userprincipalname = Convert.ToString(result.Attributes["userprincipalname"][0]);
                                aDRec.streetaddress = Convert.ToString(result.Attributes["streetaddress"][0]);
                                aDRec.samaccounttype = Convert.ToString(result.Attributes["samaccounttype"][0]);
                                aDRec.lastlogon = Convert.ToString(result.Attributes["lastlogon"][0]);
                                aDRec.whencreated = Convert.ToString(result.Attributes["whencreated"][0]);
                                aDRec.distinguishedname = Convert.ToString(result.Attributes["distinguishedname"][0]);
                                aDRec.dscorepropagationdata = Convert.ToString(result.Attributes["dscorepropagationdata"][0]);
                                aDRec.displayname = Convert.ToString(result.Attributes["displayname"][0]);
                                aDRec.sn = Convert.ToString(result.Attributes["sn"][0]);
                                aDRec.usncreated = Convert.ToString(result.Attributes["usncreated"][0]);
                                aDRec.postalcode = Convert.ToString(result.Attributes["postalcode"][0]);
                                aDRec.objectclass = null;


                            }
                            catch { }

                            aDRec.UserDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(varJson, expConverter);

                            _users.Add(aDRec);
                        }
                    };
                }
                catch (Exception ex)
                {
                    throw;
                }


                //Document must be encrypted before adding to Mongo DB -- seems like this is not the requirement

                await _adRepository.UpsertManyAsync(_users);
               

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private List<SearchResultEntry> PerformPagedSearch(LdapConnection connection, string baseDN, string filter, string[] attribs)
        {
            List<SearchResultEntry> results = new List<SearchResultEntry>();

            System.DirectoryServices.Protocols.SearchRequest request = new System.DirectoryServices.Protocols.SearchRequest(baseDN, filter, System.DirectoryServices.Protocols.SearchScope.Subtree, null);

            PageResultRequestControl prc = new PageResultRequestControl(500);

            SearchOptionsControl soc = new SearchOptionsControl(System.DirectoryServices.Protocols.SearchOption.DomainScope);

            //add the paging control
            request.Controls.Add(prc);
            request.Controls.Add(soc);

            int pages = 0;
            while (true)
            {
                pages++;
                connection.SessionOptions.ProtocolVersion = 3;


                System.DirectoryServices.Protocols.SearchResponse response = connection.SendRequest(request) as System.DirectoryServices.Protocols.SearchResponse;

                //find the returned page response control
                foreach (DirectoryControl control in response.Controls)
                {
                    if (control is PageResultResponseControl)
                    {
                        //update the cookie for next set
                        prc.Cookie = ((PageResultResponseControl)control).Cookie;
                        break;
                    }
                }

                //add them to our collection
                foreach (SearchResultEntry sre in response.Entries)
                {
                    results.Add(sre);
                }

                //our exit condition is when our cookie is empty
                if (prc.Cookie.Length == 0)
                {
                    Trace.WriteLine("Warning GetAllAdSdsp exiting in paged search wtih cookie = zero and page count =" + pages + " and user count = " + results.Count);
                    break;
                }
            }
            return results;
        }
        private ADRecord getRecFromAD(Google.Apis.Admin.Directory.directory_v1.Data.User user)
        {
            dynamic varJson = Newtonsoft.Json.JsonConvert.SerializeObject(user);
            //varHelo.Id = Guid.NewGuid();
            ADRecord gRec = new ADRecord();
            var expConverter = new ExpandoObjectConverter();

            gRec.Email = user.PrimaryEmail ?? "";
            try
            {
                gRec.Phone = Convert.ToString(user.Phones[0]);
            }
            catch { }
            gRec.FirstName = user.Name.GivenName ?? "";
            gRec.LastName = user.Name.FamilyName ?? "";
            gRec.user_origin_code = "Google";

            gRec.UserDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(varJson, expConverter);

            return gRec;
        }
    }


}