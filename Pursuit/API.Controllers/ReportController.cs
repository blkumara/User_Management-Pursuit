using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Pursuit.Context;
using Pursuit.Helpers;
using Pursuit.Model;

namespace Pursuit.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]

    [ApiController]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IADRepository<ADRecord> _aRepository;
        private readonly IADRepository<ADRecord> _mRepository;
        private readonly IADRepository<ADRecord> _gRepository;
        private readonly ILogger<AzureController> _logger;

        readonly string url = "";
        public ReportController(ServiceResolver serviceResolver, ILogger<AzureController> logger)
        {
            _aRepository = serviceResolver("AZ");
            _mRepository = serviceResolver("MS");
            _gRepository = serviceResolver("GWS");
            _logger = logger;
        }

        /* Get The Distinct List Of COMPANY aND DEPARTMENT From Azure Db */
        [HttpGet("azureList")]
        public async Task<IActionResult> azureList(String listKey)
        {
            try
            {
                var list = _aRepository.DistinctList(listKey, "");

                if (list.Count() <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "List Not Found" });
                }

                var obj = new
                {

                    List = list
                };

                return Ok(obj);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }

        /* Get The Distinct List Of DEPARTMENT AND COMPANY From  MS_AD Db */
        [HttpGet("msadList")]
        public async Task<IActionResult> msadList(String listKey)
        {
            try
            {
                var list = _mRepository.DistinctList(listKey, "");


                if (list.Count() <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "List Not Found" });
                }
                var obj = new
                {

                    List = list
                };


                return Ok(obj);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get List with Details of Managers and Employees from Azure Db
        [HttpPost("azureListDetails")]
        public async Task<IActionResult> azureFilter([FromBody] ICollection<gFilterRequest> filterList)
        {
            try
            {
                //Get Department Details under Comp
                if (filterList.FirstOrDefault().Key == "COMP")
                {
                    var list = _aRepository.DistinctList("DEPT", filterList.FirstOrDefault().Value);
                    Console.WriteLine(list.Count());

                    return Ok(new { Company = filterList.FirstOrDefault().Value, Departments = list });
                }

                //Get Department Details under Comp
                if (filterList.FirstOrDefault().Key == "COMP")
                {
                    var list = _aRepository.DistinctList("DEPT", filterList.FirstOrDefault().Value);
                    Console.WriteLine(list.Count());


                    var name = list.FirstOrDefault();
                    var departments = _aRepository.MultiFilter(filterList).Select(x => (dynamic)x.UserDocument);
                    var departmentlist = departments.Select(x => new
                    {
                        Name = Convert.ToString(x.GivenName ?? ""),
                        EmailId = Convert.ToString(x.Mail ?? ""),
                        Id = Convert.ToString(x.Id ?? ""),
                        Department = Convert.ToString(x.Department ?? ""),
                        CompanyName = Convert.ToString(x.CompanyName ?? ""),
                        ManagerName = x.Manager != null ? Convert.ToString(x.Manager.GivenName ?? "") : ""
                    });
                    return Ok(departmentlist);
                }
                //Get Managers Details under DEPT
                if (filterList.FirstOrDefault().Key == "DEPT")
                {
                    var list = _aRepository.DistinctList("MANG", filterList.FirstOrDefault().Value);

                    Console.WriteLine(list.Count());

                    var name = list.FirstOrDefault();
                    var managers = _aRepository.MultiFilter(filterList).Select(x => (dynamic)x.UserDocument);
                    var manager = managers.Select(x => new
                    {
                        Name = Convert.ToString(x.GivenName ?? ""),
                        EmailId = Convert.ToString(x.Mail ?? ""),
                        Id = Convert.ToString(x.Id ?? ""),
                        Department = Convert.ToString(x.Department ?? ""),
                        CompanyName = Convert.ToString(x.CompanyName ?? ""),
                        ManagerName = x.Manager != null ? Convert.ToString(x.Manager.GivenName ?? "") : ""
                    });
                    return Ok(manager);
                }

                //Get Employees Details under Manager
                else
                {

                    var emp = _aRepository.MultiFilter(filterList).Select(x => (dynamic)x.UserDocument);
                    Console.WriteLine(emp.Count());
                    var employees = emp.Select(x => new
                    {
                        Name = Convert.ToString(x.GivenName ?? ""),
                        EmailId = Convert.ToString(x.Mail ?? ""),
                        Id = Convert.ToString(x.Id ?? ""),
                        Department = Convert.ToString(x.Department ?? ""),
                        CompanyName = Convert.ToString(x.CompanyName ?? ""),
                        ManagerName = x.Manager != null ? Convert.ToString(x.Manager.GivenName ?? "") : ""
                    });
                    return Ok(employees);
                }


                return Ok();
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get List with Details of Managers and Employees from Azure
        [HttpPost("msadListDetails")]
        public async Task<IActionResult> msadListDetails([FromBody] ICollection<gFilterRequest> filterList)
        {
            try
            {
                //Get Department Details under Comp
                if (filterList.FirstOrDefault().Key == "COMP")
                {
                    var list = _mRepository.DistinctList("DEPT", filterList.FirstOrDefault().Value);
                    Console.WriteLine(list.Count());

                    return Ok(new { Company = filterList.FirstOrDefault().Value, Departments = list });
                }

                //Get Department Details under Comp
                if (filterList.FirstOrDefault().Key == "COMP")
                {
                    var list = _mRepository.DistinctList("DEPT", filterList.FirstOrDefault().Value);
                    Console.WriteLine(list.Count());


                    var name = list.FirstOrDefault();
                    var departments = _mRepository.MultiFilter(filterList);
                    var departmentlist = from x in departments
                                         where Convert.ToString(x.distinguishedname) == name
                                         select new
                                         {
                                             DName = Convert.ToString(x.distinguishedname),
                                             memberOf = Convert.ToString(x.memberof),
                                             EmployeeID = Convert.ToString(x.employeeid),
                                             Department = Convert.ToString(x.department),
                                             CompanyName = Convert.ToString(x.company),
                                             Name = Convert.ToString(x.FirstName),
                                             EmailId = Convert.ToString(x.Email)
                                         };
                    return Ok(departmentlist);
                }
                //Get Managers Details under DEPT
                if (filterList.FirstOrDefault().Key == "DEPT")
                {
                    var list = _mRepository.DistinctList("MANG", filterList.FirstOrDefault().Value);

                    Console.WriteLine(list.Count());

                    var name = list.FirstOrDefault();
                    var managers = _mRepository.MultiFilter(filterList);
                    var manager = from x in managers
                                         where Convert.ToString(x.distinguishedname) == name
                                         select new
                                         {
                                             DName = Convert.ToString(x.distinguishedname),
                                             memberOf = Convert.ToString(x.memberof),
                                             EmployeeID = Convert.ToString(x.employeeid),
                                             Department = Convert.ToString(x.department),
                                             CompanyName = Convert.ToString(x.company),
                                             Name = Convert.ToString(x.FirstName),
                                             EmailId = Convert.ToString(x.Email)
                                         };
                    return Ok(manager);
                }

                //Get Employees Details under Manager
                else
                {

                    var emp = _mRepository.MultiFilter(filterList);
                    Console.WriteLine(emp.Count());
                    var employees = from x in emp

                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };
                    return Ok(employees);
                }


                return Ok();
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }

        //Get Employee List With start date and end date for Particular Department from MSAD
        [HttpPost("msadDateRangeEmp")]
        public async Task<IActionResult> msadDateRangeEmp([FromBody] DateRangeRequest req)
        {
            try
            {
                var emp = _mRepository.DateRangeEmpByDept(req);

                if (emp.Count() == 0 || emp.Count() == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been This Date Range" });

                }
                var employees = from x in emp
                                where Convert.ToString(x.department) == req.Key
                                select new
                                {
                                    DName = Convert.ToString(x.distinguishedname),
                                    memberOf = Convert.ToString(x.memberof),
                                    EmployeeID = Convert.ToString(x.employeeid),
                                    Department = Convert.ToString(x.department),
                                    CompanyName = Convert.ToString(x.company),
                                    Name = Convert.ToString(x.FirstName),
                                    EmailId = Convert.ToString(x.Email)
                                };
                return Ok(employees);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }

        //Get Employee count for each department from MSAD
        [HttpGet("msadDeptEmpCount")]
        public async Task<IActionResult> msadDeptEmpCount()
        {
            try
            {
                var emp = _mRepository.DeptEmpCount();

                Console.WriteLine(emp.Count());
                //  var test = emp.Select(v => BsonSerializer.Deserialize<Property>(v)).ToList();
                var dotNetObjList = emp.ConvertAll(BsonTypeMapper.MapToDotNetValue);

                return Ok(dotNetObjList);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }

        /* Get The Employee List Password Create And LastSet In Range Between Start Date And End Date From MS_AD*/
        [HttpPost("msadPwdCrdAndLastSet")]
        public async Task<IActionResult> msadPwdLastSet([FromBody] DateRangeRequest req)
        {
            try
            {
                //Get The List of Password Created Employees  in B/W The Dates 
                if (req.Key == "PWDCRD")
                {
                    var emp = _mRepository.DateRangeEmpByDept(req);

                    if (emp.Count() == 0 || emp.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Created Password" });

                    }

                    var employees = from x in emp
                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };
                    return Ok(employees);

                }
                //Get The List of Password LastSet Employees  in B/W The Dates 
                if (req.Key == "PWDLSET")
                {
                    var emps = _mRepository.pwdLastSet(req);
                    if (emps.Count() == 0 || emps.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Set Password" });

                    }

                    Console.Write(emps.Count());
                    var employees = from x in emps
                                        //    where Convert.ToString(x.Properties.manager[0]) == "CN=Aguirre\\, Cora,OU=NV,OU=USA,OU=Users,OU=DBBOTS USERS,DC=dbbots,DC=com"
                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };
                    Console.WriteLine(employees.Count());
                    return Ok(employees);
                }
                return Ok();

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new
                {
                    ErrorCode = "409",
                    ErrorMessege = ex.Message
                });

            }
        }

        /* Get The Count Of Pasword Create , Password Last Set,LogOn And LogOff From MS_AD Db */
        [HttpPost("msadPwdAndLogCount")]
        public async Task<IActionResult> msadPwdAndLogCount([FromBody] DateRangeRequest req)
        {
            try
            {
                //Get The Count Regarding Password Parameters
                if (req.Key == "PWD")
                {
                    var empPassCreated = _mRepository.DateRangeEmpByDept(req).Select(x => (dynamic)JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(x.UserDocument)));
                    var empPassUpdated = _mRepository.pwdLastSet(req).Select(x => (dynamic)JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(x.UserDocument)));

                    if (empPassCreated.Count() == 0 || empPassCreated.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Not Password Created" });

                    }
                    if (empPassCreated.Count() == 0 || empPassCreated.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Not Password Updated" });

                    }


                    var obj = new
                    {
                        items = new[]
                        {
                            new{name="passcreated",Value=empPassCreated.Count()},
                            new{name="passLastSet",Value=empPassUpdated.Count()}
                        }
                    };


                    return Ok(obj);
                }
                //Get The Count Regarding LogIn Parameters
                if (req.Key == "LOG")
                {
                    var empLogOn = _mRepository.lastLogOn(req).Select(x => (dynamic)JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(x.UserDocument)));
                    var empLogOff = _mRepository.lastLogOff(req).Select(x => (dynamic)JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(x.UserDocument)));
                    var empLogFail = _mRepository.badPwdCount(req).Select(x => (dynamic)JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(x.UserDocument)));
                    if (empLogOn.Count() == 0 || empLogOn.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Not LogOn " });

                    }
                    if (empLogOff.Count() == 0 || empLogOff.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Not LogOff " });

                    }

                    var obj = new
                    {
                        items = new[] {
                        new {name = "LogOn" , Value = empLogOn.Count()},
                        new {name = "LogOff" , Value=empLogOff.Count()},
                        new {name = "LoginFail", Value=empLogFail.Count()}
                    }
                    };

                    return Ok(obj);
                }
                if (req.Key == "ALL")
                {
                    var empPassCreated = _mRepository.DateRangeEmpByDept(req).Select(x => (dynamic)JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(x.UserDocument)));
                    var empPassUpdated = _mRepository.pwdLastSet(req).Select(x => (dynamic)JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(x.UserDocument)));
                    var empLogOn = _mRepository.lastLogOn(req).Select(x => (dynamic)JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(x.UserDocument)));
                    var empLogOff = _mRepository.lastLogOff(req).Select(x => (dynamic)JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(x.UserDocument)));
                    var empLogFail = _mRepository.badPwdCount(req).Select(x => (dynamic)JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(x.UserDocument)));

                    var obj = new
                    {

                        items = new[] {

                        new{name="passCreated",Value=empPassCreated.Count()},
                        new{name="passLastSet",Value=empPassUpdated.Count()},
                        new {name = "LogOn" , Value = empLogOn.Count()},
                        new {name = "LogOff" , Value=empLogOff.Count()},
                        new {name = "LoginFail", Value=empLogFail.Count()}
                    }
                    };
                    return Ok(obj);
                }

                return Ok();
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new
                {
                    ErrorCode = "409",
                    ErrorMessege = ex.Message
                });

            }


        }

        /* Get The Employee List LogIn  And LogOff In Range Between Start Date And End Date From MS_AD Db */
        [HttpPost("msadLastLogOnAndOffAndLogFail")]

        public async Task<IActionResult> msadLastLogOnAndOff([FromBody] DateRangeRequest req)
        {
            try
            {
                //Get The List of LogOn  Employees  in B/W The Dates 
                if (req.Key == "LOGON")
                {
                    var emp = _mRepository.lastLogOn(req);
                    if (emp.Count() == 0 || emp.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Logged In For This Date Range" });
                    }
                    var employees = from x in emp
                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };
                    return Ok(employees);

                }
                //Get The List of LogOff Created Employees  in B/W The Dates 
                if (req.Key == "LOGOFF")
                {
                    var emps = _mRepository.lastLogOff(req);
                    if (emps.Count() == 0 || emps.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Logged Off For This Date Range" });
                    }
                    //Console.Write(emps.Count());
                    var employees = from x in emps

                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };
                    return Ok(employees);
                }
                if (req.Key == "LOGFAIL")
                {
                    var emps = _mRepository.logFail(req);
                    if (emps.Count() == 0 || emps.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Logged Fail For This Date Range" });
                    }
                    //Console.Write(emps.Count());
                    var employees = from x in emps

                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };
                    return Ok(employees);
                }
                return Ok();
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new
                {
                    ErrorCode = "409",
                    ErrorMessege = ex.Message
                });

            }
        }
        [HttpGet("msadallList")]
        public async Task<IActionResult> msadallList()
        {
            try
            {
                IList<string> list1, list2, list3 = null;
                var output = new List<Dictionary<string, object>>();
                list1 = _mRepository.DistinctList("COMP", "");
                var headCount = _mRepository.CompHeadCount();
                var dotNetObjList = headCount.ConvertAll(BsonTypeMapper.MapToDotNetValue);
                foreach (var item in list1)
                {
                    var departments = new List<Dictionary<string, object>>();
                    list2 = _mRepository.DistinctList("DEPT", item);


                    foreach (var item2 in list2)
                    {
                        list3 = _mRepository.DistinctList("MANG", item2);
                        foreach (var item3 in list3)
                        {
                            var departmentDict = new Dictionary<string, object>
                            {
                                    {"name", item2},
                                    {"mang", item3}
                            };
                            departments.Add(departmentDict);
                        }
                    }

                    // Add current company to output list
                    var companyDict = new Dictionary<string, object> {
                       {"company", item},
                       {"headCount", dotNetObjList},
                        {"departments", departments}
                       };
                    output.Add(companyDict);

                }



                // Convert output to JSON
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(output, Formatting.Indented);

                return Ok(json);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }

        //Get The Employees Count In Under Manager
        [HttpGet("msadMGREmpCount")]
        public async Task<IActionResult> msadMGREmpCount()
        {
            try
            {
                var emp = _mRepository.MGREmpCount();

                Console.WriteLine(emp.Count());
                //  var test = emp.Select(v => BsonSerializer.Deserialize<Property>(v)).ToList();
                var dotNetObjList = emp.ConvertAll(BsonTypeMapper.MapToDotNetValue);

                return Ok(dotNetObjList);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }

        //Get The Company Head Count 
        [HttpGet("msadCompHeadCount")]
        public async Task<IActionResult> msadCompHeadCount()
        {
            try
            {
                var emp = _mRepository.CompHeadCount();

                Console.WriteLine(emp.Count());
                //  var test = emp.Select(v => BsonSerializer.Deserialize<Property>(v)).ToList();
                var dotNetObjList = emp.ConvertAll(BsonTypeMapper.MapToDotNetValue);

                return Ok(dotNetObjList);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get The All The Details About Active Users in MS_AD
        [HttpGet("msadActiveUsers")]
        public async Task<IActionResult> msadActiveUsers()
        {
            try
            {
                var result = _mRepository.msadListCount();
                var dotNetObjList = result.ConvertAll(BsonTypeMapper.MapToDotNetValue);
                if (dotNetObjList.Count >= 0)
                {
                    return Ok(dotNetObjList);
                }
                else
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }

        //Get The All msadUserAttribute Change in Ms_Ad
        [HttpPost("msadUserAttributeChange")]
        public async Task<IActionResult> msadUserAttributeChange([FromBody] DateRangeRequest req)
        {
            try
            {
                //Get The Count Of Last Password Set 
                if (req.Key == "PWDLST")
                {
                    var result = _mRepository.msadpwdLastSet(req);
                    var dotNetObjList = result.ConvertAll(BsonTypeMapper.MapToDotNetValue);
                    if (dotNetObjList.Count > 0)
                    {
                        return Ok(dotNetObjList);
                    }
                    else
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Password Set This Date Range" });
                    }
                }
                //Get The Count Of Last Log ON  
                if (req.Key == "LSTLOGON")
                {
                    var result = _mRepository.msadlastLogOn(req);
                    var dotNetObjList = result.ConvertAll(BsonTypeMapper.MapToDotNetValue);
                    if (dotNetObjList.Count > 0)
                    {
                        return Ok(dotNetObjList);
                    }
                    else
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Last Log On This Date Range" });
                    }
                }
                //Get The Count Of Last Log Off  
                if (req.Key == "LSTLOGOFF")
                {
                    var result = _mRepository.msadlastLogOff(req);
                    var dotNetObjList = result.ConvertAll(BsonTypeMapper.MapToDotNetValue);
                    if (dotNetObjList.Count > 0)
                    {
                        return Ok(dotNetObjList);
                    }
                    else
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Last Log Off This Date Range" });
                    }
                }
                //Get The Count Of Log Fail
                if (req.Key == "LOGFAIL")
                {
                    var result = _mRepository.msadbadPwd(req);
                    var dotNetObjList = result.ConvertAll(BsonTypeMapper.MapToDotNetValue);
                    if (dotNetObjList.Count > 0)
                    {
                        return Ok(dotNetObjList);
                    }
                    else
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Log Fail This Date Range" });
                    }
                }
                //Getting count of pwd last set and last log on between dates
                if (req.Key == "ALL")
                {
                    var result = _mRepository.msadAllAttribute(req);
                    //Conversion of List<BsonDocument> to Lis<Object>
                    var dotNetObjList = result.ConvertAll(BsonTypeMapper.MapToDotNetValue);

                    if (dotNetObjList.Count > 0)
                    {
                        return Ok(dotNetObjList);
                    }
                    else
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Not Found" });
                    }
                }



                return Ok();
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }

        //Attribute change list of users with details under Manager
        [HttpPost("msadAttributeChangebyMang")]
        public async Task<IActionResult> msadAttributeChangebyMang([FromBody] DateRangeRequest req)
        {
            try
            {
                //Get The Count Of Last Password Set 
                if (req.Key == "PWDLST")
                {
                    var emps = _mRepository.pwdLastSet(req);
                    if (emps.Count() <= 0 || emps.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Set Password" });

                    }


                    var employees = from x in emps
                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };

                    return Ok(employees);
                }


                //Get The Count Of Last Log ON  
                if (req.Key == "LSTLOGON")
                {
                    var emp = _mRepository.lastLogOn(req);
                    if (emp.Count() <= 0 || emp.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Logged In For This Date Range" });
                    }
                    var employees = from x in emp
                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };
                    return Ok(employees);
                }
                //Get The Count Of Last Log Off  
                if (req.Key == "LSTLOGOFF")
                {
                    var emps = _mRepository.lastLogOff(req);
                    if (emps.Count() <= 0 || emps.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Logged Off For This Date Range" });
                    }
                    //Console.Write(emps.Count());
                    var employees = from x in emps

                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };
                    return Ok(employees);
                }
                //Get The Count Of Log Fail
                if (req.Key == "LOGFAIL")
                {
                    var emps = _mRepository.logFail(req);
                    if (emps.Count() <= 0 || emps.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Logged Fail For This Date Range" });
                    }
                    //Console.Write(emps.Count());
                    var employees = from x in emps

                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };
                    return Ok(employees);
                }



                return Ok();
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Attribute change list of users with details under Manager
        [HttpPost("msadAttributeChangeGetEmpList")]
        public async Task<IActionResult> msadAttributeChangeGetEmpList([FromBody] DateRangeRequest req)
        {
            try
            {
                if(req.Key == ""|| req.Key == null)
                {
                    return Ok(new { ErrorCode = "40405", ErrorMessege = "Key Should Not Be Null" });
                }
                //Get The Count Of Last Password Set 
                if (req.Key == "PWDLST")
                {
                    var emps = _mRepository.pwdLastSet(req);
                   
                    if (emps.Count() <= 0 || emps.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Set Password" });

                    }


                    var employees = from x in emps
                                   
                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };

                    return Ok(employees);
                }


                //Get The Count Of Last Log ON  
                if (req.Key == "LSTLOGON")
                {
                    var emp = _mRepository.lastLogOn(req);
                   
                    if (emp.Count() <= 0 || emp.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Logged In For This Date Range" });
                    }
                    var employees = from x in emp
                                   
                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };
                    return Ok(employees);
                }
                //Get The Count Of Last Log Off  
                if (req.Key == "LSTLOGOFF")
                {
                    var emps = _mRepository.lastLogOff(req); 
                    if (emps.Count() <= 0 || emps.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Logged Off For This Date Range" });
                    }
                    //Console.Write(emps.Count());
                    var employees = from x in emps
                                 
                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };
                    return Ok(employees);
                }
                //Get The Count Of Log Fail
                if (req.Key == "LOGFAIL")
                {
                    var emps = _mRepository.logFail(req);
                    
                    if (emps.Count() <= 0 || emps.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Logged Fail For This Date Range" });
                    }
                    //Console.Write(emps.Count());
                    var employees = from x in emps
                                   
                                    select new
                                    {
                                        DName = Convert.ToString(x.distinguishedname),
                                        memberOf = Convert.ToString(x.memberof),
                                        EmployeeID = Convert.ToString(x.employeeid),
                                        Department = Convert.ToString(x.department),
                                        CompanyName = Convert.ToString(x.company),
                                        Name = Convert.ToString(x.FirstName),
                                        EmailId = Convert.ToString(x.Email)
                                    };
                    return Ok(employees);
                }



                return Ok();
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
       

        [HttpGet("msadtrial")]
        public async Task<IActionResult> msadtrial()
        {
            try
            {
                // Define the original JSON object
                string json = @"
                    {
                      ""items"": [
                            {
                              ""name"": ""PWDLST"",
                              ""value"": [
                                  {
                                     ""_id"": [
                                    ""Ortusolis""
                                    ],
                                    ""count"": 1
                             }
                            ]
                         },
                         {
                             ""name"": ""LogOn"",
                            ""value"": [
                                 {
                                     ""_id"": [
                                         ""Dbbots, Inc.""
                                      ],
                                    ""count"": 2
                                }
                            ]
                        },
                        {
                            ""name"": ""LogOff"",
                            ""value"": [
                                {
                                   ""_id"": [
                                     ""Dbbots, Inc.""
                                     ],
                                     ""count"": 1
                                }
                             ]
                          },
                          {
                              ""name"": ""LogFail"",
                              ""value"": []
                         }
                    ]
                }";


                // Parse the original JSON object into a JObject
                JObject original = JObject.Parse(json);

                // Group the data by company name
                var grouped = original["items"]
                    .SelectMany(item => item["value"]
                        .Select(value => new
                        {
                            Company = (string)value["_id"][0],
                            Name = (string)item["name"],
                            Count = (int)value["count"]
                        }))
                    .GroupBy(item => item.Company);

                // Create the new JSON object
                var newJson = new JObject(
                    new JProperty("items",
                        new JArray(grouped.Select(companyGroup =>
                            new JObject(
                                new JProperty("name", "Company"),
                                new JProperty("value",
                                    new JArray(new JObject(
                                        new JProperty("_id", new JArray(companyGroup.Key)),
                                        new JProperty("PWDLSTcount", companyGroup.Sum(item => item.Name == "PWDLST" ? item.Count : 0)),
                                        new JProperty("LogOnCount", companyGroup.Sum(item => item.Name == "LogOn" ? item.Count : 0)),
                                        new JProperty("LogOffCount", companyGroup.Sum(item => item.Name == "LogOff" ? item.Count : 0)),
                                        new JProperty("LogFailCount", companyGroup.Sum(item => item.Name == "LogFail" ? item.Count : 0))
                                    )))
                            ))
                        ))
                    );

                // Serialize the new JSON object back to a string
                string newJsonString = newJson.ToString();

                return Ok(newJsonString);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get The All msadUserAttribute Change in Ms_Ad
        [HttpPost("msadAttributeChangeComp")]
        public async Task<IActionResult> msadAttributeChangeComp([FromBody] DateRangeRequest req)
        {
            try
            {


                var msadpwdLastSetresult = _mRepository.msadpwdLastSet(req);
                var msadpwdLastSet = msadpwdLastSetresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadlastLogOnresult = _mRepository.msadlastLogOn(req);
                var msadlastLogOn = msadlastLogOnresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadlastLogOffresult = _mRepository.msadlastLogOff(req);
                var msadlastLogOff = msadlastLogOffresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadbadPwdresult = _mRepository.msadbadPwd(req);
                var msadbadPwd = msadbadPwdresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var Alllist = msadpwdLastSet.Concat(msadlastLogOn).Concat(msadlastLogOff).Concat(msadbadPwd).ToList();
                if (Alllist.Count<=0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                var obj = new
                    {
                        items = new[] {
                            new { name = "PWDLST", value = msadpwdLastSet.ToList() },
                            new { name = "LogOn", value = msadlastLogOn.ToList() },
                            new { name = "LogOff", value = msadlastLogOff.ToList() },
                            new { name = "LogFail", value = msadbadPwd.ToList() }
                        }
                    };
                if (obj == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
              //  return Ok(obj);
                // Parse the original object into a JObject
                JObject original = JObject.FromObject(obj);

                // Group the data by company name
                var grouped = original["items"]
                    .SelectMany(item => item["value"]
                        .Select(value => new
                        {
                            Company = (string)value["_id"],
                            Name = (string)item["name"],
                            Count = (int)value["count"]
                        }))
                    .GroupBy(item => item.Company);

                // Create the new JSON object
                var newJson = new JObject(
                    new JProperty("items",
                        new JArray(grouped.Select(companyGroup =>
                            new JObject(
                                new JProperty("name", "Company"),
                                new JProperty("value",
                                    new JArray(new JObject(
                                        new JProperty("_id", new JArray(companyGroup.Key)),
                                        new JProperty("PWDLSTcount", companyGroup.Sum(item => item.Name == "PWDLST" ? item.Count : 0)),
                                        new JProperty("LogOnCount", companyGroup.Sum(item => item.Name == "LogOn" ? item.Count : 0)),
                                        new JProperty("LogOffCount", companyGroup.Sum(item => item.Name == "LogOff" ? item.Count : 0)),
                                        new JProperty("LogFailCount", companyGroup.Sum(item => item.Name == "LogFail" ? item.Count : 0))
                                    )))
                            ))
                        ))
                    );

                // Serialize the new JSON object back to a string
                string newJsonString = newJson.ToString();

                return Ok(newJsonString);

            }




            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get The All msadUserAttribute Change for perticular company in Ms_Ad
        [HttpPost("msadAttributeChangeOneComp")]
        public async Task<IActionResult> msadAttributeChangeOneComp([FromBody] DateRangeRequest req)
        {
            try
            {


                var msadpwdLastSetresult = _mRepository.msadpwdLastSetSingleData(req);
                var msadpwdLastSet = msadpwdLastSetresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadlastLogOnresult = _mRepository.msadlastLogOnSingleData(req);
                var msadlastLogOn = msadlastLogOnresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadlastLogOffresult = _mRepository.msadlastLogOffSingleData(req);
                var msadlastLogOff = msadlastLogOffresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadbadPwdresult = _mRepository.msadbadPwdSingleData(req);
                var msadbadPwd = msadbadPwdresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var Alllist = msadpwdLastSet.Concat(msadlastLogOn).Concat(msadlastLogOff).Concat(msadbadPwd).ToList();
                if (Alllist.Count <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                var obj = new
                {
                    items = new[] {
                            new { name = "PWDLST", value = msadpwdLastSet.ToList() },
                            new { name = "LogOn", value = msadlastLogOn.ToList() },
                            new { name = "LogOff", value = msadlastLogOff.ToList() },
                            new { name = "LogFail", value = msadbadPwd.ToList() }
                        }
                };
                if (obj == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                // Parse the original object into a JObject
                JObject original = JObject.FromObject(obj);

                // Group the data by company name
                var grouped = original["items"]
                    .SelectMany(item => item["value"]
                        .Select(value => new
                        {
                            Company = (string)value["_id"],
                            Name = (string)item["name"],
                            Count = (int)value["count"]
                        }))
                    .GroupBy(item => item.Company);

                // Create the new JSON object
                var newJson = new JObject(
                    new JProperty("items",
                        new JArray(grouped.Select(companyGroup =>
                            new JObject(
                                new JProperty("name", "Company"),
                                new JProperty("value",
                                    new JArray(new JObject(
                                        new JProperty("_id", new JArray(companyGroup.Key)),
                                        new JProperty("PWDLSTcount", companyGroup.Sum(item => item.Name == "PWDLST" ? item.Count : 0)),
                                        new JProperty("LogOnCount", companyGroup.Sum(item => item.Name == "LogOn" ? item.Count : 0)),
                                        new JProperty("LogOffCount", companyGroup.Sum(item => item.Name == "LogOff" ? item.Count : 0)),
                                        new JProperty("LogFailCount", companyGroup.Sum(item => item.Name == "LogFail" ? item.Count : 0))
                                    )))
                            ))
                        ))
                    );

                // Serialize the new JSON object back to a string
                string newJsonString = newJson.ToString();

                return Ok(newJsonString);

            }




            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get The All msadUserAttribute Change in Ms_Ad
        [HttpPost("msadAttributeChangeDept")]
        public async Task<IActionResult> msadAttributeChangeDept([FromBody] DateRangeRequest req)
        {
            try
            {


                var msadpwdLastSetresult = _mRepository.msadpwdLastSet(req);
                var msadpwdLastSet = msadpwdLastSetresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadlastLogOnresult = _mRepository.msadlastLogOn(req);
                var msadlastLogOn = msadlastLogOnresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadlastLogOffresult = _mRepository.msadlastLogOff(req);
                var msadlastLogOff = msadlastLogOffresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadbadPwdresult = _mRepository.msadbadPwd(req);
                var msadbadPwd = msadbadPwdresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var Alllist = msadpwdLastSet.Concat(msadlastLogOn).Concat(msadlastLogOff).Concat(msadbadPwd).ToList();
                if (Alllist.Count <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                var obj = new
                {
                    items = new[] {
                            new { name = "PWDLST", value = msadpwdLastSet.ToList() },
                            new { name = "LogOn", value = msadlastLogOn.ToList() },
                            new { name = "LogOff", value = msadlastLogOff.ToList() },
                            new { name = "LogFail", value = msadbadPwd.ToList() }
                        }
                };
                if (obj == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }

                // Parse the original object into a JObject
                JObject original = JObject.FromObject(obj);

                // Group the data by company name
                var grouped = original["items"]
                    .SelectMany(item => item["value"]
                        .Select(value => new
                        {
                            Department = (string)value["_id"],
                            Name = (string)item["name"],
                            Count = (int)value["count"]
                        }))
                    .GroupBy(item => item.Department);

                // Create the new JSON object
                var newJson = new JObject(
                    new JProperty("items",
                        new JArray(grouped.Select(departmentGroup =>
                            new JObject(
                                new JProperty("name", "Department"),
                                new JProperty("value",
                                    new JArray(new JObject(
                                        new JProperty("_id", new JArray(departmentGroup.Key)),
                                        
                                        new JProperty("PWDLSTcount", departmentGroup.Sum(item => item.Name == "PWDLST" ? item.Count : 0)),
                                        new JProperty("LogOnCount", departmentGroup.Sum(item => item.Name == "LogOn" ? item.Count : 0)),
                                        new JProperty("LogOffCount", departmentGroup.Sum(item => item.Name == "LogOff" ? item.Count : 0)),
                                        new JProperty("LogFailCount", departmentGroup.Sum(item => item.Name == "LogFail" ? item.Count : 0))
                                    )))
                            ))
                        ))
                    );

                // Serialize the new JSON object back to a string
                string newJsonString = newJson.ToString();

                return Ok(newJsonString);

            }




            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get The All msadUserAttribute Change in Ms_Ad
        [HttpPost("msadAttributeChangeOneDept")]
        public async Task<IActionResult> msadAttributeChangeOneDept([FromBody] DateRangeRequest req)
        {
            try
            {


                var msadpwdLastSetresult = _mRepository.msadpwdLastSetSingleData(req);
                var msadpwdLastSet = msadpwdLastSetresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadlastLogOnresult = _mRepository.msadlastLogOnSingleData(req);
                var msadlastLogOn = msadlastLogOnresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadlastLogOffresult = _mRepository.msadlastLogOffSingleData(req);
                var msadlastLogOff = msadlastLogOffresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadbadPwdresult = _mRepository.msadbadPwdSingleData(req);
                var msadbadPwd = msadbadPwdresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var Alllist = msadpwdLastSet.Concat(msadlastLogOn).Concat(msadlastLogOff).Concat(msadbadPwd).ToList();
                if (Alllist.Count <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                var obj = new
                {
                    items = new[] {
                            new { name = "PWDLST", value = msadpwdLastSet.ToList() },
                            new { name = "LogOn", value = msadlastLogOn.ToList() },
                            new { name = "LogOff", value = msadlastLogOff.ToList() },
                            new { name = "LogFail", value = msadbadPwd.ToList() }
                        }
                };
                if (obj == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }

                // Parse the original object into a JObject
                JObject original = JObject.FromObject(obj);

                // Group the data by company name
                var grouped = original["items"]
                    .SelectMany(item => item["value"]
                        .Select(value => new
                        {
                            Department = (string)value["_id"],
                            Name = (string)item["name"],
                            Count = (int)value["count"]
                        }))
                    .GroupBy(item => item.Department);

                // Create the new JSON object
                var newJson = new JObject(
                    new JProperty("items",
                        new JArray(grouped.Select(departmentGroup =>
                            new JObject(
                                new JProperty("name", "Department"),
                                new JProperty("value",
                                    new JArray(new JObject(
                                        new JProperty("_id", new JArray(departmentGroup.Key)),

                                        new JProperty("PWDLSTcount", departmentGroup.Sum(item => item.Name == "PWDLST" ? item.Count : 0)),
                                        new JProperty("LogOnCount", departmentGroup.Sum(item => item.Name == "LogOn" ? item.Count : 0)),
                                        new JProperty("LogOffCount", departmentGroup.Sum(item => item.Name == "LogOff" ? item.Count : 0)),
                                        new JProperty("LogFailCount", departmentGroup.Sum(item => item.Name == "LogFail" ? item.Count : 0))
                                    )))
                            ))
                        ))
                    );

                // Serialize the new JSON object back to a string
                string newJsonString = newJson.ToString();

                return Ok(newJsonString);

            }




            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }

        //Get The All msadUserAttribute Change for manager from Ms_Ad 
        [HttpPost("msadAttributeChangeMang")]
        public async Task<IActionResult> msadAttributeChangeMang([FromBody] DateRangeRequest req)
        {
            try
            {


                var msadpwdLastSetresult = _mRepository.msadpwdLastSet(req);
                var msadpwdLastSet = msadpwdLastSetresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadlastLogOnresult = _mRepository.msadlastLogOn(req);
                var msadlastLogOn = msadlastLogOnresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadlastLogOffresult = _mRepository.msadlastLogOff(req);
                var msadlastLogOff = msadlastLogOffresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadbadPwdresult = _mRepository.msadbadPwd(req);
                var msadbadPwd = msadbadPwdresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var Alllist = msadpwdLastSet.Concat(msadlastLogOn).Concat(msadlastLogOff).Concat(msadbadPwd).ToList();
                if (Alllist.Count <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                var obj = new
                {
                    items = new[] {
                            new { name = "PWDLST", value = msadpwdLastSet.ToList() },
                            new { name = "LogOn", value = msadlastLogOn.ToList() },
                            new { name = "LogOff", value = msadlastLogOff.ToList() },
                            new { name = "LogFail", value = msadbadPwd.ToList() }
                        }
                };
                if (obj == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }

                // Parse the original object into a JObject
                JObject original = JObject.FromObject(obj);

                // Group the data by company name
                var grouped = original["items"]
                .SelectMany(item => item["value"]
                    .Select(value => new
                    {
                        Manager = (string)value["_id"],
                        Company = (string)value["companyName"][0],
                        Department = (string)value["departmentName"][0],
                        ManagerName = (string)value["managerName"],
                        ManagerEmail = (string)value["managerEmail"],
                        Name = (string)item["name"],
                        Count = (int)value["count"]
                    }))
                .GroupBy(item => item.Manager);

                // Create the new JSON object
                var newJson = new JObject(
                    new JProperty("items",
                        new JArray(grouped.Select(managerGroup =>
                            new JObject(
                                new JProperty("name", "Manager"),
                                new JProperty("value",
                                    new JArray(new JObject(
                                        new JProperty("_id", new JArray(managerGroup.Key)),
                                         new JProperty("Company", new JArray(managerGroup.First().Company)),
                                         new JProperty("Department", new JArray(managerGroup.First().Department)),
                                         new JProperty("ManagerName", managerGroup.First().ManagerName),
                                         new JProperty("ManagerEmail", managerGroup.First().ManagerEmail),
                                        new JProperty("PWDLSTcount", managerGroup.Sum(item => item.Name == "PWDLST" ? item.Count : 0)),
                                        new JProperty("LogOnCount", managerGroup.Sum(item => item.Name == "LogOn" ? item.Count : 0)),
                                        new JProperty("LogOffCount", managerGroup.Sum(item => item.Name == "LogOff" ? item.Count : 0)),
                                        new JProperty("LogFailCount", managerGroup.Sum(item => item.Name == "LogFail" ? item.Count : 0))
                                    )))
                            ))
                        ))
                    );
                // Serialize the new JSON object back to a string
                string newJsonString = newJson.ToString();

                return Ok(newJsonString);

            }




            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get The All msadUserAttribute Change for manager from Ms_Ad 
        [HttpPost("msadAttributeChangeOneMang")]
        public async Task<IActionResult> msadAttributeChangeOneMang([FromBody] DateRangeRequest req)
        {
            try
            {


                var msadpwdLastSetresult = _mRepository.msadpwdLastSetSingleData(req);
                var msadpwdLastSet = msadpwdLastSetresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadlastLogOnresult = _mRepository.msadlastLogOnSingleData(req);
                var msadlastLogOn = msadlastLogOnresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadlastLogOffresult = _mRepository.msadlastLogOffSingleData(req);
                var msadlastLogOff = msadlastLogOffresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadbadPwdresult = _mRepository.msadbadPwdSingleData(req);
                var msadbadPwd = msadbadPwdresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var Alllist = msadpwdLastSet.Concat(msadlastLogOn).Concat(msadlastLogOff).Concat(msadbadPwd).ToList();
                if (Alllist.Count <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                var obj = new
                {
                    items = new[] {
                            new { name = "PWDLST", value = msadpwdLastSet.ToList() },
                            new { name = "LogOn", value = msadlastLogOn.ToList() },
                            new { name = "LogOff", value = msadlastLogOff.ToList() },
                            new { name = "LogFail", value = msadbadPwd.ToList() }
                        }
                };
                if (obj == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }

                // Parse the original object into a JObject
                JObject original = JObject.FromObject(obj);

                // Group the data by company name
                var grouped = original["items"]
                .SelectMany(item => item["value"]
                    .Select(value => new
                    {
                        Manager = (string)value["_id"],
                        Company = (string)value["companyName"][0],
                        Department = (string)value["departmentName"][0],
                        ManagerName = (string)value["managerName"],
                        ManagerEmail = (string)value["managerEmail"],
                        Name = (string)item["name"],
                        Count = (int)value["count"]
                    }))
                .GroupBy(item => item.Manager);

                // Create the new JSON object
                var newJson = new JObject(
                    new JProperty("items",
                        new JArray(grouped.Select(managerGroup =>
                            new JObject(
                                new JProperty("name", "Manager"),
                                new JProperty("value",
                                    new JArray(new JObject(
                                        new JProperty("_id", new JArray(managerGroup.Key)),
                                         new JProperty("Company", new JArray(managerGroup.First().Company)),
                                         new JProperty("Department", new JArray(managerGroup.First().Department)),
                                         new JProperty("ManagerName", managerGroup.First().ManagerName),
                                         new JProperty("ManagerEmail", managerGroup.First().ManagerEmail),
                                        new JProperty("PWDLSTcount", managerGroup.Sum(item => item.Name == "PWDLST" ? item.Count : 0)),
                                        new JProperty("LogOnCount", managerGroup.Sum(item => item.Name == "LogOn" ? item.Count : 0)),
                                        new JProperty("LogOffCount", managerGroup.Sum(item => item.Name == "LogOff" ? item.Count : 0)),
                                        new JProperty("LogFailCount", managerGroup.Sum(item => item.Name == "LogFail" ? item.Count : 0))
                                    )))
                            ))
                        ))
                    );
                // Serialize the new JSON object back to a string
                string newJsonString = newJson.ToString();

                return Ok(newJsonString);

            }




            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get all Employees List from MSAD
        [HttpGet("msadGetAllEmployees")]
        public async Task<IActionResult> msadGetAllEmployees()
        {
            try
            {
                var emp = _mRepository.msadgetallemp();
                Console.WriteLine(emp.Count());
                if (emp.Count() == 0 || emp.Count() == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Not Found" });

                }
                 

                var employees = from x in emp

                                select new
                                {
                                    DName = Convert.ToString(x.distinguishedname),
                                    memberOf = Convert.ToString(x.memberof)
                                    ,
                                    EmployeeID = Convert.ToString(x.employeeid),
                                    Department = Convert.ToString(x.department)
                                    ,
                                    CompanyName = Convert.ToString(x.company),
                                    ManagerName = Convert.ToString(x.manager),
                                   
                                    Name = Convert.ToString(x.FirstName),
                                    EmailId = Convert.ToString(x.Email)
                                };
                return Ok(employees);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get The All msadUserAttribute Change for manager from Ms_Ad 
        [HttpPost("GWSAttributeChange")]
        public async Task<IActionResult> GWSAttributeChange([FromBody] DateRangeRequest req)
        {
            try
            {

                var gwsCreatedCount = _gRepository.gwsUserCreated(req)?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();
                var gwsLastLogonCount= _gRepository.gwslastLogOn(req)?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();


                var Alllist = gwsCreatedCount.Concat(gwsLastLogonCount).ToList();
                if (Alllist.Count <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                var obj = new
                {
                    items = new[] {
                            new { name = "UserCreated", value = gwsCreatedCount.ToList() },
                            new { name = "LogOn", value = gwsLastLogonCount.ToList() }
                          
                        }
                };
                if (obj == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }

                // Parse the original object into a JObject
                JObject original = JObject.FromObject(obj);

                // Group the data by company name
                var grouped = original["items"]
                .SelectMany(item => item["value"]
                    .Select(value => new
                    {
                        User = (string)value["_id"],
                        Name = (string)item["name"],
                        Count = (int)value["count"]
                    }))
                .GroupBy(item => item.User);

                // Create the new JSON object
                var newJson = new JObject(
                    new JProperty("items",
                        new JArray(grouped.Select(userGroup =>
                            new JObject(
                               
                                new JProperty("value",
                                    new JArray(new JObject(
                                        new JProperty("_id", userGroup.Key),
                                           new JProperty("Createdcount", userGroup.Sum(item => item.Name == "UserCreated" ? item.Count : 0)),
                                        new JProperty("LogOnCount", userGroup.Sum(item => item.Name == "LogOn" ? item.Count : 0))
                                    )))
                            ))
                        ))
                    );
                // Serialize the new JSON object back to a string
                string newJsonString = newJson.ToString();

                return Ok(newJsonString);

            }




            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get all Employees List from MSAD
        [HttpGet("GWSGetAllEmployees")]
        public async Task<IActionResult> GWSGetAllEmployees()
        {
            try
            {
                var emp = _gRepository.gwsgetallemp();
                Console.WriteLine(emp.Count());
                if (emp.Count() == 0 || emp.Count() == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Not Found" });

                }


                var employees = from x in emp

                                select new
                                {
                                    FirstName = Convert.ToString(x.FirstName ?? ""),
                                    LastName = Convert.ToString(x.LastName ?? ""),
                                    EmailId = Convert.ToString(x.Email)
                                };
                return Ok(employees);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        [HttpPost("GWSAttributeChangeGetEmpList")]
        public async Task<IActionResult> GWSAttributeChangeGetEmpList([FromBody] DateRangeRequest req)
        {
            try
            {
                if (req.Key == "" || req.Key == null)
                {
                    return Ok(new { ErrorCode = "40405", ErrorMessege = "Key Should Not Be Null" });
                }
                if (req.Key == "CRET")
                {
                    var emps = _gRepository.createdTime(req);

                    if (emps.Count() <= 0 || emps.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Create Password" });

                    }


                    var employees = from x in emps

                                    select new
                                    {
                                        FirstName = Convert.ToString(x.FirstName ?? ""),
                                        LastName = Convert.ToString(x.LastName ?? ""),
                                        EmailId = Convert.ToString(x.Email)
                                    };

                    return Ok(employees);
                }
                //Get The Count Of Last Password Set 
                if (req.Key == "LOGON")
                {
                    var emps = _gRepository.lastLogOn(req);

                    if (emps.Count() <= 0 || emps.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Set Password" });

                    }


                    var employees = from x in emps

                                    select new
                                    {
                                        FirstName = Convert.ToString(x.FirstName ?? ""),
                                        LastName = Convert.ToString(x.LastName ?? ""),
                                        EmailId = Convert.ToString(x.Email)

                                    };

                    return Ok(employees);
                }

                return Ok();
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get The All The Details About Active Users in MS_AD
        [HttpGet("GWSActiveUsers")]
        public async Task<IActionResult> GWSActiveUsers()
        {
            try
            {
                var result = _gRepository.gwsListCount();
                var dotNetObjList = result.ConvertAll(BsonTypeMapper.MapToDotNetValue);
                if (dotNetObjList.Count >= 0)
                {
                    return Ok(dotNetObjList);
                }
                else
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }

        //Get The All msadUserAttribute Change for manager from Azure 
        [HttpPost("AzureAttributeChangeMang")]
        public async Task<IActionResult> AzureAttributeChangeMang([FromBody] DateRangeRequest req)
        {
            try
            {

                var azureCreatedCount = _aRepository.azureUserCreated(req)?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();
                var azureLastLogonCount = _aRepository.azurepwdLastSet(req)?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();


                var Alllist = azureCreatedCount.Concat(azureLastLogonCount).ToList();
                if (Alllist.Count <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                var obj = new
                {
                    items = new[] {
                            new { name = "UserCreated", value = azureCreatedCount.ToList() },
                            new { name = "PWDLST", value = azureLastLogonCount.ToList() }

                        }
                };
                if (obj == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
               

                 // Parse the original object into a JObject
                 JObject original = JObject.FromObject(obj);

                 // Group the data by company name
                 var grouped = original["items"]
                 .SelectMany(item => item["value"]
                     .Select(value => new
                     {
                         Manager = (string)value["_id"],
                         Company = (string)value["companyName"],
                         Department = (string)value["departmentName"],
                         ManagerName = (string)value["managerName"],
                         ManagerEmail = (string)value["managerEmail"],
                         Name = (string)item["name"],
                         Count = (int)value["count"]
                     }))
                 .GroupBy(item => item.Manager);

                 // Create the new JSON object
                 var newJson = new JObject(
                     new JProperty("items",
                         new JArray(grouped.Select(managerGroup =>
                             new JObject(
                                 new JProperty("name", "Manager"),
                                 new JProperty("value",
                                     new JArray(new JObject(
                                         new JProperty("_id", managerGroup.Key),
                                          new JProperty("Company", managerGroup.First().Company),
                                          new JProperty("Department", managerGroup.First().Department),
                                          new JProperty("ManagerName", managerGroup.First().ManagerName),
                                          new JProperty("ManagerEmail", managerGroup.First().ManagerEmail),
                                         new JProperty("Createdcount", managerGroup.Sum(item => item.Name == "UserCreated" ? item.Count : 0)),
                                         new JProperty("PWDLSTcount", managerGroup.Sum(item => item.Name == "PWDLST" ? item.Count : 0))
                                     )))
                             ))
                         ))
                     );
                 // Serialize the new JSON object back to a string
                 string newJsonString = newJson.ToString();

                 return Ok(newJsonString);

            }




            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get The All msadUserAttribute Change for comp from Azure 
        [HttpPost("AzureAttributeChangeComp")]
        public async Task<IActionResult> AzureAttributeChangeComp([FromBody] DateRangeRequest req)
        {
            try
            {

                var azureCreatedCount = _aRepository.azureUserCreated(req)?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();
                var azureLastLogonCount = _aRepository.azurepwdLastSet(req)?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();


                var Alllist = azureCreatedCount.Concat(azureLastLogonCount).ToList();
                if (Alllist.Count <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                var obj = new
                {
                    items = new[] {
                            new { name = "UserCreated", value = azureCreatedCount.ToList() },
                            new { name = "PWDLST", value = azureLastLogonCount.ToList() }

                        }
                };
                if (obj == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }


                // Parse the original object into a JObject
                JObject original = JObject.FromObject(obj);

                // Group the data by company name
                var grouped = original["items"]
                .SelectMany(item => item["value"]
                    .Select(value => new
                    {
                        Company = (string)value["_id"],
                        Name = (string)item["name"],
                        Count = (int)value["count"]
                    }))
                .GroupBy(item => item.Company);

                // Create the new JSON object
                var newJson = new JObject(
                    new JProperty("items",
                        new JArray(grouped.Select(companyGroup =>
                            new JObject(
                                new JProperty("name", "Company"),
                                new JProperty("value",
                                    new JArray(new JObject(
                                        new JProperty("_id", companyGroup.Key),
                                        new JProperty("Createdcount", companyGroup.Sum(item => item.Name == "UserCreated" ? item.Count : 0)),
                                        new JProperty("PWDLSTcount", companyGroup.Sum(item => item.Name == "PWDLST" ? item.Count : 0))
                                    )))
                            ))
                        ))
                    );
                // Serialize the new JSON object back to a string
                string newJsonString = newJson.ToString();

                return Ok(newJsonString);

            }




            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get The All msadUserAttribute Change for dept from Azure 
        [HttpPost("AzureAttributeChangeDept")]
        public async Task<IActionResult> AzureAttributeChangeDept([FromBody] DateRangeRequest req)
        {
            try
            {

                var azureCreatedCount = _aRepository.azureUserCreated(req)?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();
                var azureLastLogonCount = _aRepository.azurepwdLastSet(req)?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();


                var Alllist = azureCreatedCount.Concat(azureLastLogonCount).ToList();
                if (Alllist.Count <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                var obj = new
                {
                    items = new[] {
                            new { name = "UserCreated", value = azureCreatedCount.ToList() },
                            new { name = "PWDLST", value = azureLastLogonCount.ToList() }

                        }
                };
                if (obj == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }


                // Parse the original object into a JObject
                JObject original = JObject.FromObject(obj);

                // Group the data by company name
                var grouped = original["items"]
                .SelectMany(item => item["value"]
                    .Select(value => new
                    {
                        Company = (string)value["_id"],
                        Name = (string)item["name"],
                        Count = (int)value["count"]
                    }))
                .GroupBy(item => item.Company);

                // Create the new JSON object
                var newJson = new JObject(
                    new JProperty("items",
                        new JArray(grouped.Select(departmentGroup =>
                            new JObject(
                                new JProperty("name", "Department"),
                                new JProperty("value",
                                    new JArray(new JObject(
                                        new JProperty("_id", departmentGroup.Key),
                                        new JProperty("Createdcount", departmentGroup.Sum(item => item.Name == "UserCreated" ? item.Count : 0)),
                                        new JProperty("PWDLSTcount", departmentGroup.Sum(item => item.Name == "PWDLST" ? item.Count : 0))
                                    )))
                            ))
                        ))
                    );
                // Serialize the new JSON object back to a string
                string newJsonString = newJson.ToString();

                return Ok(newJsonString);

            }




            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get The All The Details About Active Users in Azure
        [HttpGet("AzureActiveUsers")]
        public async Task<IActionResult> azureActiveUsers()
        {
            try
            {
                var result = _aRepository.azureListCount();
                var dotNetObjList = result.ConvertAll(BsonTypeMapper.MapToDotNetValue);
                if (dotNetObjList.Count >= 0)
                {
                    return Ok(dotNetObjList);
                }
                else
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        //Get all Employees List from Azure
        [HttpGet("AzureGetAllEmployees")]
        public async Task<IActionResult> AzureGetAllEmployees()
        {
            try
            {
                
                var emp = _aRepository.azuregetallemp().Select(x => (dynamic)x.UserDocument);
                Console.WriteLine(emp.Count());
                if (emp.Count() == 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessage = "Users Not Found" });
                }

                var employees = emp.Select(x => new
                {
                    Name = Convert.ToString(x.GivenName ?? ""),
                    EmailId = Convert.ToString(x.Mail ?? ""),
                    Id = Convert.ToString(x.Id ?? ""),
                    Department = Convert.ToString(x.Department ?? ""),
                    CompanyName = Convert.ToString(x.CompanyName ?? ""),
                    ManagerName = x.Manager != null ? Convert.ToString(x.Manager.GivenName ?? "") : ""
                });

                return Ok(employees);


            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        [HttpPost("azureAttributeChangeGetEmpList")]
        public async Task<IActionResult> azureAttributeChangeGetEmpList([FromBody] DateRangeRequest req)
        {
            try
            {
                if (req.Key == "" || req.Key == null)
                {
                    return Ok(new { ErrorCode = "40405", ErrorMessege = "Key Should Not Be Null" });
                }
                if (req.Key == "CRET")
                {
                    var emps = _aRepository.createdTime(req).Select(x => (dynamic)x.UserDocument); 

                    if (emps.Count() <= 0 || emps.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Create Password" });
                    }


                    var employees = emps.Select(x => new
                    {
                        Name = Convert.ToString(x.GivenName ?? ""),
                        EmailId = Convert.ToString(x.Mail ?? ""),
                        Id = Convert.ToString(x.Id ?? ""),
                        Department = Convert.ToString(x.Department ?? ""),
                        CompanyName = Convert.ToString(x.CompanyName ?? ""),
                        ManagerName = x.Manager != null ? Convert.ToString(x.Manager.GivenName ?? "") : ""
                    });

                    return Ok(employees);
                }
                //Get The Count Of Last Password Set 
                if (req.Key == "PWDLST")
                {
                    var emps = _aRepository.pwdLastSet(req).Select(x => (dynamic)x.UserDocument);

                    if (emps.Count() <= 0 || emps.Count() == null)
                    {
                        return Ok(new { ErrorCode = "40401", ErrorMessege = "Users Have Not Been Set Password" });

                    }


                    var employees = emps.Select(x => new
                    {
                        Name = Convert.ToString(x.GivenName ?? ""),
                        EmailId = Convert.ToString(x.Mail ?? ""),
                        Id = Convert.ToString(x.Id ?? ""),
                        Department = Convert.ToString(x.Department ?? ""),
                        CompanyName = Convert.ToString(x.CompanyName ?? ""),
                        ManagerName = x.Manager != null ? Convert.ToString(x.Manager.GivenName ?? "") : ""
                    });

                    return Ok(employees);
                }

                return Ok();
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        [HttpPost("azureAttributeChangeOneComp")]
        public async Task<IActionResult> azureAttributeChangeOneComp([FromBody] DateRangeRequest req)
        {
            try
            {


                var msadpwdLastSetresult = _aRepository.msadpwdLastSetSingleData(req);
                var msadpwdLastSet = msadpwdLastSetresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

               var msadCreateresult = _aRepository.msadCreateSingleData(req);
                var msadCreate = msadCreateresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                /*var msadlastLogOffresult = _mRepository.msadlastLogOffSingleData(req);
                var msadlastLogOff = msadlastLogOffresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadbadPwdresult = _mRepository.msadbadPwdSingleData(req);
                var msadbadPwd = msadbadPwdresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();*/

                //var Alllist = msadpwdLastSet.Concat(msadlastLogOn).Concat(msadlastLogOff).Concat(msadbadPwd).ToList();
                if (msadpwdLastSet.Count <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                var obj = new
                {
                    items = new[] {
                            new { name = "PWDLST", value = msadpwdLastSet.ToList() },
                           new { name = "CRET", value = msadCreate.ToList() },
                            /* new { name = "LogOff", value = msadlastLogOff.ToList() },
                            new { name = "LogFail", value = msadbadPwd.ToList() }*/
                        }
                };
                if (obj == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                // Parse the original object into a JObject
                JObject original = JObject.FromObject(obj);

                // Group the data by company name
                var grouped = original["items"]
                    .SelectMany(item => item["value"]
                        .Select(value => new
                        {
                            Company = (string)value["_id"],
                            Name = (string)item["name"],
                            Count = (int)value["count"]
                        }))
                    .GroupBy(item => item.Company);

                // Create the new JSON object
                var newJson = new JObject(
                    new JProperty("items",
                        new JArray(grouped.Select(companyGroup =>
                            new JObject(
                                new JProperty("name", "Company"),
                                new JProperty("value",
                                    new JArray(new JObject(
                                        new JProperty("_id", new JArray(companyGroup.Key)),
                                        new JProperty("PWDLSTcount", companyGroup.Sum(item => item.Name == "PWDLST" ? item.Count :0)),
                                      new JProperty("CREATEcount", companyGroup.Sum(item => item.Name == "CRET" ? item.Count : 0))
                                        /*  new JProperty("LogOffCount", companyGroup.Sum(item => item.Name == "LogOff" ? item.Count : 0)),
                                        new JProperty("LogFailCount", companyGroup.Sum(item => item.Name == "LogFail" ? item.Count : 0))*/
                                    )))
                            ))
                        ))
                    );

                // Serialize the new JSON object back to a string
                string newJsonString = newJson.ToString();

                return Ok(newJsonString);

            }




            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }
        [HttpPost("azureAttributeChangeOneDept")]
        public async Task<IActionResult> azureAttributeChangeOneDept([FromBody] DateRangeRequest req)
        {
            try
            {


                var msadpwdLastSetresult = _aRepository.msadpwdLastSetSingleData(req);
                var msadpwdLastSet = msadpwdLastSetresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadCreateresult = _aRepository.msadCreateSingleData(req);
                var msadCreate = msadCreateresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                /*var msadlastLogOffresult = _mRepository.msadlastLogOffSingleData(req);
                var msadlastLogOff = msadlastLogOffresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadbadPwdresult = _mRepository.msadbadPwdSingleData(req);
                var msadbadPwd = msadbadPwdresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();*/

                //var Alllist = msadpwdLastSet.Concat(msadlastLogOn).Concat(msadlastLogOff).Concat(msadbadPwd).ToList();
                if (msadpwdLastSet.Count <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                var obj = new
                {
                    items = new[] {
                            new { name = "PWDLST", value = msadpwdLastSet.ToList() },
                           new { name = "CRET", value = msadCreate.ToList() },
                            /* new { name = "LogOff", value = msadlastLogOff.ToList() },
                            new { name = "LogFail", value = msadbadPwd.ToList() }*/
                        }
                };
                if (obj == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                // Parse the original object into a JObject
                JObject original = JObject.FromObject(obj);

                // Group the data by company name
                var grouped = original["items"]
                    .SelectMany(item => item["value"]
                        .Select(value => new
                        {
                            Company = (string)value["_id"],
                            Name = (string)item["name"],
                            Count = (int)value["count"]
                        }))
                    .GroupBy(item => item.Company);

                // Create the new JSON object
                var newJson = new JObject(
                    new JProperty("items",
                        new JArray(grouped.Select(departmentGroup =>
                            new JObject(
                                new JProperty("name", "Department"),
                                new JProperty("value",
                                    new JArray(new JObject(
                                        new JProperty("_id", new JArray(departmentGroup.Key)),
                                        new JProperty("PWDLSTcount", departmentGroup.Sum(item => item.Name == "PWDLST" ? item.Count : 0)),
                                      new JProperty("CREATEcount", departmentGroup.Sum(item => item.Name == "CRET" ? item.Count : 0))
                                    /*  new JProperty("LogOffCount", companyGroup.Sum(item => item.Name == "LogOff" ? item.Count : 0)),
                                    new JProperty("LogFailCount", companyGroup.Sum(item => item.Name == "LogFail" ? item.Count : 0))*/
                                    )))
                            ))
                        ))
                    );

                // Serialize the new JSON object back to a string
                string newJsonString = newJson.ToString();

                return Ok(newJsonString);

            }




            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }

        [HttpPost("azureAttributeChangeOneMang")]
        public async Task<IActionResult> azureAttributeChangeOneMang([FromBody] DateRangeRequest req)
        {
            try
            {


                var msadpwdLastSetresult = _aRepository.msadpwdLastSetSingleData(req);
                var msadpwdLastSet = msadpwdLastSetresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadCreateresult = _aRepository.msadCreateSingleData(req);
                var msadCreate = msadCreateresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                /*var msadlastLogOffresult = _mRepository.msadlastLogOffSingleData(req);
                var msadlastLogOff = msadlastLogOffresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();

                var msadbadPwdresult = _mRepository.msadbadPwdSingleData(req);
                var msadbadPwd = msadbadPwdresult?.ConvertAll(BsonTypeMapper.MapToDotNetValue) ?? new List<object>();*/

                //var Alllist = msadpwdLastSet.Concat(msadlastLogOn).Concat(msadlastLogOff).Concat(msadbadPwd).ToList();
                if (msadpwdLastSet.Count <= 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                var obj = new
                {
                    items = new[] {
                            new { name = "PWDLST", value = msadpwdLastSet.ToList() },
                           new { name = "CRET", value = msadCreate.ToList() },
                            /* new { name = "LogOff", value = msadlastLogOff.ToList() },
                            new { name = "LogFail", value = msadbadPwd.ToList() }*/
                        }
                };
                if (obj == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Data Not Found" });
                }
                // Parse the original object into a JObject
                JObject original = JObject.FromObject(obj);

                // Group the data by company name
                var grouped = original["items"]
                    .SelectMany(item => item["value"]
                        .Select(value => new
                        {
                            Company = (string)value["_id"],
                            Name = (string)item["name"],
                            Count = (int)value["count"]
                        }))
                    .GroupBy(item => item.Company);

                // Create the new JSON object
                var newJson = new JObject(
                    new JProperty("items",
                        new JArray(grouped.Select(managerGroup =>
                            new JObject(
                                new JProperty("name", "Manager"),
                                new JProperty("value",
                                    new JArray(new JObject(
                                        new JProperty("_id", new JArray(managerGroup.Key)),
                                        new JProperty("PWDLSTcount", managerGroup.Sum(item => item.Name == "PWDLST" ? item.Count : 0)),
                                      new JProperty("CREATEcount", managerGroup.Sum(item => item.Name == "CRET" ? item.Count : 0))
                                    /*  new JProperty("LogOffCount", companyGroup.Sum(item => item.Name == "LogOff" ? item.Count : 0)),
                                    new JProperty("LogFailCount", companyGroup.Sum(item => item.Name == "LogFail" ? item.Count : 0))*/
                                    )))
                            ))
                        ))
                    );

                // Serialize the new JSON object back to a string
                string newJsonString = newJson.ToString();

                return Ok(newJsonString);

            }




            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = ex.Message });

            }
        }

    }

}