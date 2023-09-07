using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Pursuit.Context;
using Pursuit.Model;
using System.DirectoryServices;
using System.Dynamic;
using Pursuit.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.DirectoryServices.Protocols;
using Newtonsoft.Json.Converters;
using System.Net;
using System.Diagnostics;
using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
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

    public class MSController : ControllerBase
    {
        private readonly IADRepository<ADRecord> _adRepository;
        private readonly ILogger<MSController> _logger;

        private readonly IPursuitRepository<Admin_Configuration> _adminconfigRepository;
        public MSController(ServiceResolver serviceResolver,
           ILogger<MSController> logger, IConfiguration config, IPursuitRepository<Admin_Configuration> adminconfigRepository)
        {
            _adRepository = serviceResolver("MS");
            _adminconfigRepository = adminconfigRepository;
            _logger = logger;
        }

        //Uncomment in next build
        [HttpGet("msadLogin")]
        public async Task<IActionResult> msadLogin(string UserEmail)
        {

            Oauth_Setting cs = new Oauth_Setting();
            try
            {
                //get connection setup from admin config

                var adminConfig = await Task.Run(() => _adminconfigRepository
                   .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());
                cs = adminConfig.Oauth_Settings.Where(c => c.Connection_Type == "AD").FirstOrDefault();

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



                        string filter = $"(&(objectCategory=person)(objectClass=user)(mail={UserEmail}))";


                        string[] components = domainName.Split('.');
                        string dcString = "DC=" + string.Join(",DC=", components);
                        string baseDN = dcString;


                        string[] attribArray = { "*" };

                        List<SearchResultEntry> srList = PerformPagedSearch(connection, baseDN, filter, attribArray);


                        var expConverter = new ExpandoObjectConverter();
                        dynamic varJson;
                        SearchResultEntry result = srList.FirstOrDefault();
                        LoginModel user = new LoginModel();
                        if (result != null)
                        {
                            user.Email = UserEmail;
                            user.IsVerified = true;
                            return Ok(user);
                        }
                        else
                        {
                            user.Email = UserEmail;
                            user.IsVerified = false;
                            return Ok(user);
                        }


                    };
                }
                catch (Exception ex)
                {
                    throw;
                }




            }
            catch (Exception ex)
            {
                return Ok(new { ErrorCode = "409", ErrorMessege = "Microsoft AD Login Failed" });
                throw ex;
            }
        }

        [HttpPost("msAdUsersFiltered")]
        [Authorize]
        public async Task<IActionResult> filterMSADUsers([FromBody] ICollection<gFilterRequest> filterList, [FromQuery] PaginationParameters paginationParameters)
        {
            try
            {
                var users = _adRepository.MultiFilter(filterList);

                if (users == null || users.Count() <= 0)
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
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Filtering Microsoft AD Users" });
                throw ex;
            }
        }

        [HttpPost("importADUsers")]
        [Authorize]
        public async Task<IActionResult> getMSADUsers([FromBody] Connection cs)
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
                                    Role role1 = new Role();
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
                    };
                }
                catch (Exception ex)
                {
                    throw;
                }


                //Document must be encrypted before adding to Mongo DB -- seems like this is not the requirement

                await _adRepository.UpsertManyAsync(_users);
                return Ok(new { ResponseCode = "200", ResponseMessege = "Microsoft AD Data Was Imported Successfully." });

            }
            catch (Exception ex)
            {
                return Ok(new { ErrorCode = "409", ErrorMessege = "Microsoft AD Import Failed" });
                throw ex;
            }
        }

        [HttpPost("verifyMSAccount")]
        [Authorize]
        public async Task<IActionResult> GetMSAccountVerification([FromBody] Connection cs)
        {
            try
            {

                string domainName = cs.Domain_Name;
                string userId = cs.Connection_User_Id;
                string password = cs.Connection_User_Pwd;
                string hostName = cs.HostName;

                LdapConnection ldapConnection = new LdapConnection(new LdapDirectoryIdentifier(hostName));

                TimeSpan mytimeout = new TimeSpan(0, 0, 0, 1);
                try
                {
                    ldapConnection.AuthType = AuthType.Basic;
                    ldapConnection.AutoBind = false;
                    ldapConnection.Timeout = mytimeout;
                    ldapConnection.Bind(new System.Net.NetworkCredential(userId, password));

                    ldapConnection.Dispose();
                    return Ok(new { ResponseCode = "200", ResponseMessege = "Microsoft AD Connection Tested Successfully" });


                }
                catch (LdapException e)
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "Can Not Test the Microsoft AD Connection" });

                }

            }
            catch (Exception ex)
            {
                return Ok(new { ErrorCode = "409", ErrorMessege = "Microsoft AD Connection Test Failed" });
                throw ex;
            }

        }

        [HttpPost("SyncMSADUsersDaily")]
        [Authorize]
        public async Task<IActionResult> SyncMSADUsersDaily([FromBody] Connection cs)
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

                        // string[] attribArray = { "mail", "telephonenumber", "givenname", "name" };
                        string[] attribArray = { "*" };

                        List<SearchResultEntry> srList = PerformPagedSearch(connection, baseDN, filter, attribArray);

                        var expConverter = new ExpandoObjectConverter();
                        dynamic varJson;

                        if (srList.Count == 0) return null;

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
                                aDRec.user_origin_code = "AD";
                                //Adding empty role setails
                                AccessRule rule = new AccessRule();
                                rule.Feature = "View";
                                rule.Access = true;
                                Role role1 = new Role();
                                role1.RoleName = "";
                                role1.AccessRules.Add(rule);
                                aDRec.Role = role1;

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

                //return _users;

                //Document must be encrypted before adding to Mongo DB
                await _adRepository.UpsertManyAsync(_users);
                return Ok("Data Synced Up!");
            }
            catch (Exception ex)
            {
                return NoContent();
            }
        }

        private List<SearchResultEntry> PerformPagedSearch(LdapConnection connection, string baseDN, string filter, string[] attribs)
        {
            List<SearchResultEntry> results = new List<SearchResultEntry>();

            SearchRequest request = new SearchRequest(baseDN, filter, System.DirectoryServices.Protocols.SearchScope.Subtree, null);

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


                SearchResponse response = connection.SendRequest(request) as SearchResponse;

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

    }
}