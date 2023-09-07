using Google.Apis.Admin.Directory.directory_v1.Data;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;

using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;

using Newtonsoft.Json;

using Pursuit.Context;
using Pursuit.Context.ConfigFile;
using Pursuit.Helpers;
using Pursuit.Model;
using Pursuit.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime;
using System.Security.Claims;
using System.Security.Policy;
using System.Text;
using static Pursuit.Utilities.Enums;
using static System.Net.WebRequestMethods;
using Role = Pursuit.Model.Role;
using User = Pursuit.Model.User;
/* =========================================================
Item Name: API's Related to User - UsersController
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

    public class UsersController : ControllerBase
    {
        private readonly IPursuitRepository<User> _userRepository;
        private readonly IPursuitRepository<Role> _roleRepository;

        private readonly IPursuitRepository<Admin_Configuration> _adminconfigRepository;
        private readonly IPursuitRepository<EmailContentConfiguration> _emailconfigRepository;

        private static readonly FormOptions _defaultFormOptions = new FormOptions();
        private readonly ILogger<UsersController> _logger;
        private readonly IConfiguration _config;
        private readonly long _fileSizeLimit;
        private readonly string[] _permittedExtensions = { ".xls", ".xlsx", ".csv", ".txt" };


        public UsersController(IPursuitRepository<User> userRepository, IPursuitRepository<Admin_Configuration> adminconfigRepository,
            IPursuitRepository<Role> roleRepository, IPursuitRepository<EmailContentConfiguration> emailconfigRepository,
            ILogger<UsersController> logger, IConfiguration config)
        {
            _userRepository = userRepository;

            _roleRepository = roleRepository;
            _adminconfigRepository = adminconfigRepository;
            _emailconfigRepository = emailconfigRepository;

            _logger = logger;
            _config = config;
            _fileSizeLimit = long.Parse(_config["FileSizeLimit"]);
        }
        //API used to register user by themselve
        [HttpPost("registerUser")]

        public async Task<IActionResult> registerUser(User user)
        {
            //Checking if user exists with same email and phone number
            try
            {
                _logger.LogInformation("Registered Users Data");
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            var existUser = await Task.Run(() => _userRepository.FilterBy(
                    existUser => existUser.Email == user.Email || (existUser.Phone == user.Phone && existUser.Phone != null && existUser.Phone != "")
                    ).AsQueryable().FirstOrDefault());
            //If doesn't exists add user
            if (existUser == null)
            {
                //Guid generation
                user.UserGuId = Guid.NewGuid();
                //User Id generation
                user.Id = ObjectId.GenerateNewId();
                //When user register until admin approval remain in PENDING_APPROVAL status
                user.Userstatus = Convert.ToString(Enums.UserStatus.PENDING_APPROVAL);
                //Creating origin code
                user.user_origin_code = Convert.ToString(Enums.UserOrigin.SELF);
                //Encription of password and storing hash and salt to DB.
                var Password = Cryptograph.EncryptPassword(user.Password);
                user.Password = Password.Hash;
                user.Salt = Password.Salt;
                user.PasswordSetDateTime = DateTime.Now;

                //Defaulting role
                if (user.Role == null)
                {
                    AccessRule rule = new AccessRule();
                    rule.Feature = "View";
                    rule.Access = true;
                    Role role1 = new Role();
                    role1.RoleName = "Viewer";
                    role1.AccessRules.Add(rule);
                    user.Role = role1;

                }
                //Adding username

                var s1 = "";
                var s2 = user.LastName;
                if (s2.Count() >= 7)
                {
                    s1 = user.FirstName.Substring(0, 1) + user.LastName.Substring(0, 7);
                }
                else
                {
                    int i = 1;
                    while (s2.Count() <= 7)
                    {

                        s2 = s2 + i.ToString();
                        i++;
                    }
                    s1 = user.FirstName.Substring(0, 1) + s2.Substring(0, 7);

                }



                user.Username = s1;

                await _userRepository.InsertOneAsync(user);
                EmailToAdminsAsync(user);
                return Ok(user);
            }
            else
            {
                if (user.Userstatus == Convert.ToString(Enums.UserStatus.DEACTIVE))
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "Your Account Is Deactivated, Please Contact Your Administrator!" });
                }
                if (user.Userstatus == Convert.ToString(Enums.UserStatus.REJECTED))
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "Your Registration Request Is Rejected, Please Contact Your Administrator!" });
                }
                else
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "User Already Exists With This Phone Number, Please Contact Your Administrator!" });
                }
            }

        }
        private async void EmailToAdminsAsync(User user)
        {
            string role = "Admin";
            //Get admin data
            var admin = await Task.Run(() => _userRepository.FilterBy(
                   user1 => user1.Role.RoleName == role).AsQueryable());
            Notification notification = new Notification();
            notification.Id = ObjectId.GenerateNewId();
            if (user.FirstName == null || user.FirstName == "")
            {
                notification.NotifiedBy = "User";
            }
            else
            {
                notification.NotifiedBy = user.FirstName;
            }

            notification.NotificationText = "New User Registered";
            notification.NotificationStatus = "New";
            //  notification.NotificationStatus = "Unread";
            notification.DateCreated = DateTime.Now;

            foreach (var adminUser in admin)
            {
                if (adminUser.Notifications == null)
                {
                    adminUser.Notifications = new Collection<Notification> { notification };
                }
                else
                {
                    adminUser.Notifications.Add(notification);
                }

                await _userRepository.UpdateNotificationAsync(adminUser.Id.ToString(), adminUser.Notifications);
                //get email settings from admin config
                var adminConfig = await Task.Run(() => _adminconfigRepository
                 .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());

                //Sending mails to admin for approval of user
                await Task.Run(() => _userRepository.SendEmailToAdmin(user, adminUser, adminConfig));
            }

        }
        //API used to register user by Admin
        [HttpPost("addUser")]
        [Authorize]
        public async Task<IActionResult> addUser(User user)
        {
            //Checking if user exists with same email and phone number
            _logger.LogInformation("Add Users Data");
            var existUser = await Task.Run(() => _userRepository.FilterBy(
                    existUser => existUser.Email == user.Email || (existUser.Phone == user.Phone && existUser.Phone != null && existUser.Phone != "")
                    ).AsQueryable().FirstOrDefault());

            var otherusers = await Task.Run(() => _userRepository.FilterBy(
                         x => (x.Phone == user.Phone && x.Phone != null && x.Phone != "")).AsQueryable());
            if (otherusers.Count() > 0)
            {
                return Ok(new { ErrorCode = "409", ErrorMessege = "User With This Phone Number Already Exists" });

            }
            //If doesn't exists add user
            if (existUser == null)
            {
                //Guid generation
                user.UserGuId = Guid.NewGuid();
                //User Id generation
                user.Id = ObjectId.GenerateNewId();


                //When user get add by admin until user verify details status of user remains in PENDING_VERIFICATION
                user.Userstatus = Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION);


                //Creating origin code
                user.user_origin_code = Convert.ToString(Enums.UserOrigin.ADMIN);
                //Encription of password and storing hash and salt to DB.
                user.Password = await Task.Run(() => _userRepository.GeneratePassword());
                var GeneratedPass = user.Password;
                var Password = Cryptograph.EncryptPassword(user.Password);
                user.Password = Password.Hash;
                user.Salt = Password.Salt;
                user.PasswordSetDateTime = DateTime.Now;

                //Defaulting role
                if (user.Role == null)
                {
                    AccessRule rule = new AccessRule();
                    rule.Feature = "View";
                    rule.Access = true;
                    Role role1 = new Role();
                    role1.RoleName = "Viewer";
                    role1.AccessRules.Add(rule);
                    user.Role = role1;

                }
                //Adding username
                var s1 = "";
                var s2 = user.LastName;
                if (s2.Count() >= 7)
                {
                    s1 = user.FirstName.Substring(0, 1) + user.LastName.Substring(0, 7);
                }
                else
                {
                    int i = 1;
                    while (s2.Count() <= 7)
                    {

                        s2 = s2 + i.ToString();
                        i++;
                    }
                    s1 = user.FirstName.Substring(0, 1) + s2.Substring(0, 7);

                }



                user.Username = s1;
                await _userRepository.InsertOneAsync(user);
                //get email settings from admin config
                var adminConfig = await Task.Run(() => _adminconfigRepository
                   .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());

                var emailconfig = await Task.Run(() => _emailconfigRepository
                .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable());

                //Sending mail to user for verification
                await Task.Run(() => _userRepository.SendEmailUser(user, GeneratedPass, adminConfig));

                return Ok(user);
            }
            else
            {
                if (user.Userstatus == Convert.ToString(Enums.UserStatus.DEACTIVE))
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "Your Account Is Deactivated, Please Contact Your Administrator!" });
                }
                if (user.Userstatus == Convert.ToString(Enums.UserStatus.REJECTED))
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "Your Registration Request Is Rejected, Please Contact Your Administrator!" });
                }
                else
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "User Already Exists" });
                }
            }


        }
        //API for get all users data
        [HttpGet("getUserData")]

        public async Task<IActionResult> GetUserData()
        {
            _logger.LogInformation("Queried Users Data");
            try
            {
                Guid guid;
                //Getting all users from the database
                var user = await Task.Run(() => _userRepository
                .FilterBy(x => x.Id != ObjectId.Empty && x.Userstatus == Convert.ToString(Enums.UserStatus.ACTIVE)).AsQueryable());

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Failed To Fetch Users" });

            }
            return NoContent();
        }
        //API to get users detail by user id
        [HttpGet("getUsersById")]

        public async Task<IActionResult> GetUsersId(string id)
        {
            _logger.LogInformation("Queried Users Data");
            try
            {
                //Getting perticular user details
                var user = await Task.Run(() => _userRepository.FindByIdAsync(id));

                if (user == null)
                {//If user not found
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
                }
                else
                {
                    // user.RoleName = user.Role.RoleName;
                    //If user is present in db
                    return Ok(user);
                }
            }
            catch (Exception ex)

            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Fetching User" });

            }
            return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found,Please Register!" });
        }

        //API to get users detail by user status
        [HttpGet("getUsersByStatus")]
        [Authorize]
        public async Task<IActionResult> GetUsersByStatus(string status)
        {

            _logger.LogInformation("Queried Users Data");
            try
            {
                if (status == "ALL")
                {
                    var user = await Task.Run(() => _userRepository
                 .FilterBy(x => x.Id != ObjectId.Empty && !(x.Userstatus == Convert.ToString(Enums.UserStatus.DEACTIVE) || x.Userstatus == Convert.ToString(Enums.UserStatus.REJECTED))).AsQueryable());

                    return Ok(user);
                }
                else
                {

                    //Getting all users from the database as per status
                    var user = await Task.Run(() => _userRepository
                   .FilterBy(x => x.Userstatus.Equals(status)).AsQueryable());
                    // user.RoleName = user.Role.RoleName;
                    return Ok(user);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Fetching Users" });

            }
            return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found,Please Register!" });
        }
        //API to get users detail by user status
        [HttpGet("getDeactiveRejectUsers")]
        [Authorize]
        public async Task<IActionResult> getDeactiveRejectUsers()
        {

            _logger.LogInformation("Queried Users Data");
            try
            {

                var user = await Task.Run(() => _userRepository
             .FilterBy(x => x.Id != ObjectId.Empty && (x.Userstatus == Convert.ToString(Enums.UserStatus.DEACTIVE) || x.Userstatus == Convert.ToString(Enums.UserStatus.REJECTED))).AsQueryable());
                return Ok(user);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Fetching Users" });

            }
            return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
        }
        //API to get Admin notifications by admin id
        [HttpGet("getNotoficationById")]
        [Authorize]
        public async Task<IActionResult> getAdminNotoficationById(string userId, string status)
        {
            _logger.LogInformation("Queried Users Data");
            try
            {
                //Getting perticular user details
                var user = await Task.Run(() => _userRepository.FindByIdAsync(userId));
                var notification = user.Notifications;
                List<Notification> notify1 = new List<Notification>();
                //get email settings from admin config
                var adminConfig = await Task.Run(() => _adminconfigRepository
                   .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());
                int days = int.Parse(adminConfig.Notification_Expires_days);

                foreach (var notify in notification)
                {

                    if (notify.NotificationStatus == "Unread" || notify.NotificationStatus == "New")
                    {
                        DateTime lastsetdate = (DateTime)notify.DateCreated;
                        DateTime currentDateTime = DateTime.Now;

                        TimeSpan timeDifference = currentDateTime - lastsetdate;
                        int daysDifference = timeDifference.Days;
                        NotifyStatusRequest req = new NotifyStatusRequest();
                        req.UserId = userId;
                        req.NotificationId = notify.Id.ToString();
                        req.Status = "Expired";
                        if (daysDifference >= days)
                        {
                            await _userRepository.UpdateNotificationStatusAsync(req);
                        }
                        else
                            if (notify.NotificationStatus == status)
                        {


                            string input = notify.DateCreated.ToString();
                            DateTime dateTime = DateTime.Parse(input);
                            string formattedDate = dateTime.ToString("dd-MMM-yyyy");
                            notify.Date = formattedDate;


                            notify1.Add(notify);
                        }

                    }
                    else
                    if (notify.NotificationStatus == status)
                    {
                        string input = notify.DateCreated.ToString();
                        DateTime dateTime = DateTime.Parse(input);
                        string formattedDate = dateTime.ToString("dd-MMM-yyyy");
                        notify.Date = formattedDate;

                        notify1.Add(notify);
                    }

                }
                if (user == null)
                {//If user not found
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
                }
                else
                {
                    //If user is present in db
                    return Ok(notify1);
                }
            }
            catch (Exception ex)

            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Fetching Notifications" });

            }
            return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found,Please Register!" });
        }
        // API to update notification status
        [HttpPost("updateNotificationStatus")]
        [Authorize]
        public async Task<IActionResult> UpdateNotificationStatus([FromBody] List<NotifyStatusRequest> requests)
        {
            try
            {
                foreach (var request in requests)
                {
                    // Update Notification Status
                    await _userRepository.UpdateNotificationStatusAsync(request);
                }

                return Ok(new { ResponseCode = "200", ResponseMessage = "Notification Status Updated Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessage = ex.Message });
            }
        }


        [HttpPost("GetTheData")]
        public async Task<IActionResult> GetTheData(ConfigSettings configs)
        {

            return Ok(new { ResponseCode = "200", ResponseMessege = configs.DomainName });
        }

        [HttpPost("VerifyUser")]
        public async Task<IActionResult> VerifyUser([FromBody] LoginModel _user)
        {
            //Get user details by email id or phone number
            var user = await Task.Run(() => _userRepository.FilterBy(
                user => user.Email == _user.Email
                ).AsQueryable().FirstOrDefault());
            if (user == null)
            {
                return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });

            }
            if (user.Userstatus == Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION))
            { return Ok(new { ErrorCode = "200", ErrorMessege = "Your Account Is Not Verified Yet, Please Verify Your Account" }); }
            if (user.Userstatus == Convert.ToString(Enums.UserStatus.DEACTIVE))
            { return Ok(new { ErrorCode = "200", ErrorMessege = "Your Account Is Inactive, Please Contact Administrator!" }); }
            if (user.Userstatus == Convert.ToString(Enums.UserStatus.PENDING_APPROVAL))
            { return Ok(new { ErrorCode = "200", ErrorMessege = "Your Account Is Not Approved Yet, Please Wait For Approval!" }); }
            if (user.Userstatus == Convert.ToString(Enums.UserStatus.REJECTED))
            {
                return Ok(new { ErrorCode = "200", ErrorMessege = "Your Registration Request Is Rejected, Please Contact Your Administrator!" });
            }
            //verifying encrypted password with entered password by user
            bool userpass = Cryptograph.VerifyPassword(_user.Password, user.Salt, user.Password);
            if (userpass == true)
            {
                return Ok(user.Id);
            }
            else
            {
                return Ok(new { ErrorCode = "40402", ErrorMessege = "The Password You Entered Is Incorrect" });
            }
        }

        //API used for login
        [HttpPost("authenticateUser")]

        public async Task<IActionResult> UserLogin([FromBody] LoginModel _user)
        {
            string OTP = null;

            try
            {
                if (_user.Email != null)
                {
                    //Get user details by email id or phone number
                    var userdata = await Task.Run(() => _userRepository.FilterBy(
                        user => user.Email == _user.Email //&& (user.Userstatus == "Active" || user.Userstatus == "PENDING_VERIFICATION")
                        ).AsQueryable().FirstOrDefault());
                    // check user is there or not

                    if (userdata == null)
                    {

                        return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
                    }
                    else
                    {
                        if (userdata.Userstatus == Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION))
                        {
                            //get email settings
                            var adminconfig = await Task.Run(() => _adminconfigRepository
                                      .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());
                            if (adminconfig.Authentication_Settings.Activation_Mail_Expires_After != null || adminconfig.Authentication_Settings.Activation_Mail_Expires_After != "")
                            {
                                //Checking the Password Reset days
                                var days = int.Parse(adminconfig.Authentication_Settings.Activation_Mail_Expires_After);
                                DateTime lastsetdate = (DateTime)userdata.PasswordSetDateTime;
                                DateTime currentDateTime = DateTime.Now;

                                TimeSpan timeDifference = currentDateTime - lastsetdate;
                                int daysDifference = timeDifference.Days;
                                if (daysDifference >= days)
                                {
                                    return Ok(new { ErrorCode = "40405", ErrorMessege = "The Verification Mail Expired, Please Contact Admin" });

                                }
                            }
                        }
                        if (userdata.Userstatus == Convert.ToString(Enums.UserStatus.ACTIVE) || userdata.Userstatus == Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION))
                        {

                            //get pwd settings
                            var adminconfig = await Task.Run(() => _adminconfigRepository
                                      .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());
                            if (adminconfig.Authentication_Settings.Password_Settings.Pwd_Change_Enforce_Duration != null || adminconfig.Authentication_Settings.Password_Settings.Pwd_Change_Enforce_Duration != "")
                            {
                                //Checking the Password Reset days
                                var days = int.Parse(adminconfig.Authentication_Settings.Password_Settings.Pwd_Change_Enforce_Duration);
                                DateTime lastsetdate = (DateTime)userdata.PasswordSetDateTime;
                                DateTime currentDateTime = DateTime.Now;

                                TimeSpan timeDifference = currentDateTime - lastsetdate;
                                int daysDifference = timeDifference.Days;
                                if (daysDifference >= days)
                                {
                                    return Ok(new { ErrorCode = "40405", ErrorMessege = "The Password Has Been Expired, Please Reset Your Password" });

                                }
                            }

                            if (adminconfig.Two_FA_Settings.Two_FA_Enforce == true)
                            {

                                OTP = await Task.Run(() => _userRepository.loginVerification(userdata.Email, adminconfig));
                                // userdata.LoginOTP = OTP;
                            }
                            if (userdata.Userstatus == Convert.ToString(Enums.UserStatus.ACTIVE) && (userdata.Password == null || userdata.Password == ""))
                            {
                                return Ok(new { ErrorCode = "200", ErrorMessege = "You Have Used Oauth Settings Before For Login, Please Use Same Again For Login Or Please Set Password Using Forgot Password" });
                            }
                            else
                            {
                                //verifying encrypted password with entered password by user
                                bool userpass = Cryptograph.VerifyPassword(_user.Password, userdata.Salt, userdata.Password);
                                if (userpass == true)
                                {

                                    var claims = new[] {
                                 new Claim(ClaimTypes.Role, userdata.Role.RoleName.ToString()??"Viewer"),
                                 new Claim(ClaimTypes.Name, userdata.FirstName??""),
                                 new Claim(ClaimTypes.Email, userdata.Email??""),
                                 new Claim(ClaimTypes.NameIdentifier, Convert.ToString(userdata.Id))
                                  };
                                    //Get user details by email id or phone number
                                    var user = await Task.Run(() => _userRepository.FilterBy(
                                        user => user.Email == _user.Email //&& (user.Userstatus == "Active" || user.Userstatus == "PENDING_VERIFICATION")
                                        ).AsQueryable().FirstOrDefault());
                                    if (user != null)
                                    {
                                        user.LoginOTP = OTP;
                                        var tokenString = GenerateJSONWebToken(claims);
                                        return Ok(new { user, token = tokenString });
                                    }
                                }
                                else
                                {
                                    return Ok(new { ErrorCode = "40402", ErrorMessege = "The Password You Entered Is Incorrect" });
                                }
                            }
                        }
                        if (userdata.Userstatus == Convert.ToString(Enums.UserStatus.DEACTIVE))
                        { return Ok(new { ErrorCode = "200", ErrorMessege = "Your Account Is Inactive, Please Contact Administrator!" }); }
                        if (userdata.Userstatus == Convert.ToString(Enums.UserStatus.PENDING_APPROVAL))
                        { return Ok(new { ErrorCode = "200", ErrorMessege = "Your Account Is Not Approved Yet, Please Wait For Approval!" }); }
                        if (userdata.Userstatus == Convert.ToString(Enums.UserStatus.REJECTED))
                        {
                            return Ok(new { ErrorCode = "200", ErrorMessege = "Your Registration Request Is Rejected, Please Contact Your Administrator!" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Authentication" });

            }

            return Ok(new { ErrorCode = "400", ErrorMessege = "Email Id OR Phone Number Needed" });
        }

        //API used for login when user use the AD,Google or Azure authentication
        [HttpPost("authenticateUserByType")]

        public async Task<IActionResult> UserLoginwithoutPassword([FromBody] LoginModel _user)
        {

            try
            {
                if (_user.IsVerified == false)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Account Verification Failed, Please Try Again With Valid Data" });

                }
                if (_user.Email != null)
                {
                    //Get user details by email id or phone number
                    var user = await Task.Run(() => _userRepository.FilterBy(
                        user => user.Email == _user.Email //&& (user.Userstatus == "Active" || user.Userstatus == "PENDING_VERIFICATION")
                        ).AsQueryable().FirstOrDefault());
                    // check user is there or not

                    if (user == null)
                    {

                        return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
                    }
                    else
                    {

                        if (user.Userstatus == Convert.ToString(Enums.UserStatus.ACTIVE) || user.Userstatus == Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION))
                        {
                            //Delete password if user is in pending verification and make them active
                            if (user.Userstatus == Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION))
                            {
                                //encrypt entered password

                                var PasswordSetDateTime = DateTime.Now;
                                //update password,salt
                                await _userRepository.UpdatePasswordAsync(user.Id.ToString(), "", null, PasswordSetDateTime);


                                await _userRepository.UpdateStatusAsync(user.Id.ToString(), "ACTIVE");
                            }

                            var claims = new[] {
                                 new Claim(ClaimTypes.Role, user.Role.RoleName.ToString()??"Viewer"),
                                 new Claim(ClaimTypes.Name, user.FirstName??""),
                                 new Claim(ClaimTypes.Email, user.Email??""),
                                 new Claim(ClaimTypes.NameIdentifier, Convert.ToString(user.Id))
                                  };

                            if (user != null)
                            {
                                var tokenString = GenerateJSONWebToken(claims);
                                return Ok(new { user, token = tokenString });
                            }

                        }
                        if (user.Userstatus == Convert.ToString(Enums.UserStatus.DEACTIVE))
                        { return Ok(new { ErrorCode = "200", ErrorMessege = "Your Account Is Inactive, Please Contact Administrator!" }); }
                        if (user.Userstatus == Convert.ToString(Enums.UserStatus.PENDING_APPROVAL))
                        { return Ok(new { ErrorCode = "200", ErrorMessege = "Your Account Is Not Approved Yet, Please Wait For Approval!" }); }
                        if (user.Userstatus == Convert.ToString(Enums.UserStatus.REJECTED))
                        {
                            return Ok(new { ErrorCode = "200", ErrorMessege = "Your Registration Request Is Rejected, Please Contact Your Administrator!" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Authentication" });

            }

            return Ok(new { ErrorCode = "400", ErrorMessege = "Email Id Needed" });
        }

        //API for forgot password: send OTP 
        [HttpGet("forgotPassword")]

        public async Task<IActionResult> forgotPassword(string email)
        {
            //_logger.LogInformation("Queried Users Data");
            try
            {
                //Get user details by email id
                var users = await Task.Run(() => _userRepository.FilterBy(
                     user => user.Email == email).AsQueryable().FirstOrDefault());
                if (users != null)
                {
                    if (users.Userstatus == Convert.ToString(Enums.UserStatus.ACTIVE) || users.Userstatus == Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION))
                    {
                        //get email settings
                        var adminconfig = await Task.Run(() => _adminconfigRepository
                              .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());

                        //Generate OTP
                        string OTP = await Task.Run(() => _userRepository.ForgotPassword(users, adminconfig));
                        //In response we give OTP and user id
                        var obj = new
                        {
                            OTP,
                            UserId = Convert.ToString(users.Id)
                        };

                        return Ok(obj);
                    }

                    if (users.Userstatus == Convert.ToString(Enums.UserStatus.DEACTIVE))
                    { return Ok(new { ErrorCode = "200", ErrorMessege = "Your Account Is Inactive, Please Contact Administrator!" }); }
                    if (users.Userstatus == Convert.ToString(Enums.UserStatus.PENDING_APPROVAL))
                    { return Ok(new { ErrorCode = "200", ErrorMessege = "Your Account Is Not Approved Yet, Please Wait For Approval!" }); }
                    if (users.Userstatus == Convert.ToString(Enums.UserStatus.REJECTED))
                    {
                        return Ok(new { ErrorCode = "200", ErrorMessege = "Your Registration Request Is Rejected, Please Contact Your Administrator!" });
                    }

                }
                else
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Forgot Password Failed" });

            }
            return Ok();
        }
        //API for email verification: send OTP 
        [HttpGet("emailVerification")]

        public async Task<IActionResult> emailVerification(string email)
        {

            try
            {
                var otherusers = await Task.Run(() => _userRepository.FilterBy(
                          x => x.Email == email).AsQueryable());
                Console.WriteLine(otherusers.Count());
                if (otherusers.Count() > 0)
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "User Already Exists, Please Contact Your Administrator!" });

                }
                //get email settings from admin config
                var adminConfig = await Task.Run(() => _adminconfigRepository
                                   .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());

                //Generate OTP
                string OTP = await Task.Run(() => _userRepository.emailVerification(email, adminConfig));
                //In response we give OTP and user id
                var obj = new
                {
                    OTP,
                };
                return Ok(obj);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Email Verification Failed" });

            }

            return Ok();


        }
        //API to update new pawword
        [HttpPost("updateUserPassword")]

        public async Task<IActionResult> updateUserPassword([FromBody] UpdatePassRequest _req)
        {

            try
            {


                //encrypt entered password
                var Password = Cryptograph.EncryptPassword(_req.newPassword);
                var PasswordSetDateTime = DateTime.Now;
                //update password,salt
                await _userRepository.UpdatePasswordAsync(_req.userId, Password.Hash, Password.Salt, PasswordSetDateTime);

                var user = await Task.Run(() => _userRepository.FindByIdAsync(_req.userId));
                if (user.Userstatus == Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION))
                {

                    await _userRepository.UpdateStatusAsync(user.Id.ToString(), "ACTIVE");
                }

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Password Updation Failed" });

            }
            return Ok(new { ResponseCode = "200", ResponseMessege = "Password Updated Successfully" });

        }

        //API for update user details
        [HttpPost("updateUser")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(string id, User user)
        {
            try
            {
                var GeneratedPass = "";
                var users = await Task.Run(() => _userRepository.FindByIdAsync(id));
                var emailTemplates = await Task.Run(() => _emailconfigRepository.FindByIdAsync(id));
                if (users == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
                }
                if (user.Username == null || user.Username == "")
                {
                    if (users.Username == null || users.Username == "")
                    {
                        var s1 = "";
                        var s2 = user.LastName;
                        if (s2.Count() >= 7)
                        {
                            s1 = user.FirstName.Substring(0, 1) + user.LastName.Substring(0, 7);
                        }
                        else
                        {
                            int i = 1;
                            while (s2.Count() <= 7)
                            {

                                s2 = s2 + i.ToString();
                                i++;
                            }
                            s1 = user.FirstName.Substring(0, 1) + s2.Substring(0, 7);

                        }



                        user.Username = s1;
                    }
                    else
                    {
                        user.Username = users.Username;
                    }
                }
                if (users.Email != user.Email)
                {
                    var otherusers = await Task.Run(() => _userRepository.FilterBy(
                          x => x.Email == user.Email).AsQueryable());
                    if (otherusers.Count() > 0)
                    {
                        return Ok(new { ErrorCode = "409", ErrorMessege = "User With This Email Id Already Exists" });

                    }
                }

                if (users.Phone != user.Phone)
                {
                    var otherusers = await Task.Run(() => _userRepository.FilterBy(
                          x => (x.Phone == user.Phone && x.Phone != null && x.Phone != "")).AsQueryable());
                    if (otherusers.Count() > 0)
                    {
                        return Ok(new { ErrorCode = "409", ErrorMessege = "User With This Phone Number Already Exists" });

                    }
                }
                if (user.Userstatus == Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION))
                {
                    //Encription of password and storing hash and salt to DB.
                    user.Password = await Task.Run(() => _userRepository.GeneratePassword());
                    GeneratedPass = user.Password;
                    var Password = Cryptograph.EncryptPassword(user.Password);
                    var PasswordSetDateTime = DateTime.Now;
                    //update password,salt

                    await _userRepository.UpdatePasswordAsync(id, Password.Hash, Password.Salt, PasswordSetDateTime);
                }

                //updating user details for perticular user id
                await _userRepository.ReplaceOneAsync(id, user);
                var usersupdated = await Task.Run(() => _userRepository.FindByIdAsync(id));
                if (users.Userstatus != user.Userstatus)
                {
                    var adminConfig = await Task.Run(() => _adminconfigRepository
                   .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());




                    Notification notification = new Notification();
                    notification.Id = ObjectId.GenerateNewId();
                    notification.NotifiedBy = "Admin";
                    notification.NotificationText = "Your Status Have Been Changed";
                    notification.NotificationStatus = "New";
                    // notification.NotificationStatus = "Unread";
                    notification.DateCreated = DateTime.Now;
                    if (usersupdated.Notifications == null)
                    {
                        usersupdated.Notifications = new Collection<Notification> { notification };
                    }
                    else
                    {
                        usersupdated.Notifications.Add(notification);
                    }

                    await _userRepository.UpdateNotificationAsync(usersupdated.Id.ToString(), usersupdated.Notifications);
                    EmailContentConfiguration emailconfig = new EmailContentConfiguration();
                    //As per status need to send email to user
                    if (usersupdated.Userstatus == Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION))
                    {
                        emailconfig = await Task.Run(() => _emailconfigRepository
                               .FilterBy(x => x.EmailType == "Verification").AsQueryable().FirstOrDefault());
                        //Sending mail to user to know their status changes
                        await Task.Run(() => _userRepository.SendStatusEmailToUser(adminConfig, usersupdated, usersupdated.Userstatus, GeneratedPass, emailconfig));

                    }
                    if (usersupdated.Userstatus == Convert.ToString(Enums.UserStatus.ACTIVE))
                    {
                        emailconfig = await Task.Run(() => _emailconfigRepository
                               .FilterBy(x => x.EmailType == "Approval").AsQueryable().FirstOrDefault());
                        //Sending mail to user to know their status changes
                        await Task.Run(() => _userRepository.SendStatusEmailToUser(adminConfig, usersupdated, usersupdated.Userstatus, "", emailconfig));

                    }
                    if (usersupdated.Userstatus == Convert.ToString(Enums.UserStatus.DEACTIVE))
                    {
                        emailconfig = await Task.Run(() => _emailconfigRepository
                              .FilterBy(x => x.EmailType == "Deletion").AsQueryable().FirstOrDefault());
                        //Sending mail to user to know their status changes
                        await Task.Run(() => _userRepository.SendStatusEmailToUser(adminConfig, usersupdated, usersupdated.Userstatus, "", emailconfig));

                    }
                    if (usersupdated.Userstatus == Convert.ToString(Enums.UserStatus.REJECTED))
                    {
                        emailconfig = await Task.Run(() => _emailconfigRepository
                              .FilterBy(x => x.EmailType == "Rejection").AsQueryable().FirstOrDefault());
                        //Sending mail to user to know their status changes
                        await Task.Run(() => _userRepository.SendStatusEmailToUser(adminConfig, usersupdated, usersupdated.Userstatus, "", emailconfig));

                    }
                }
                return Ok(usersupdated);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Updating User" });

            }
            return Ok();
        }
        //API to update user status
        [HttpPost("updateUserStatus")]

        public async Task<IActionResult> updateUserStatus(string userId, string userStatus)
        {
            try
            {
                var users = await Task.Run(() => _userRepository.FindByIdAsync(userId));
                if (users == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
                }
                else
                {
                    //updating user status
                    await _userRepository.UpdateStatusAsync(userId, userStatus);
                    var usersupdated = await Task.Run(() => _userRepository.FindByIdAsync(userId));
                    var adminConfig = await Task.Run(() => _adminconfigRepository
                   .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());

                    EmailContentConfiguration emailconfig = new EmailContentConfiguration();

                    //As per status need to send email to user
                    if (userStatus == Convert.ToString(Enums.UserStatus.ACTIVE))
                    {
                        emailconfig = await Task.Run(() => _emailconfigRepository
                              .FilterBy(x => x.EmailType == "Approval").AsQueryable().FirstOrDefault());
                        //Sending mail to user to know their status changes
                        await Task.Run(() => _userRepository.SendStatusEmailToUser(adminConfig, usersupdated, userStatus, "", emailconfig));

                    }
                    if (userStatus == Convert.ToString(Enums.UserStatus.DEACTIVE))
                    {
                        emailconfig = await Task.Run(() => _emailconfigRepository
                              .FilterBy(x => x.EmailType == "Deletion").AsQueryable().FirstOrDefault());
                        //Sending mail to user to know their status changes
                        await Task.Run(() => _userRepository.SendStatusEmailToUser(adminConfig, usersupdated, userStatus, "", emailconfig));

                    }
                    if (userStatus == Convert.ToString(Enums.UserStatus.REJECTED))
                    {
                        emailconfig = await Task.Run(() => _emailconfigRepository
                              .FilterBy(x => x.EmailType == "Rejection").AsQueryable().FirstOrDefault());
                        //Sending mail to user to know their status changes
                        await Task.Run(() => _userRepository.SendStatusEmailToUser(adminConfig, usersupdated, userStatus, "", emailconfig));

                    }
                    if (userStatus == Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION))
                    {
                        emailconfig = await Task.Run(() => _emailconfigRepository
                              .FilterBy(x => x.EmailType == "Verification").AsQueryable().FirstOrDefault());
                        //Sending mail to user to know their status changes
                        await Task.Run(() => _userRepository.SendStatusEmailToUser(adminConfig, usersupdated, userStatus, "", emailconfig));

                    }
                    return Ok(usersupdated);
                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Updating User" });

            }
            return Ok();
        }
        //API for add user configuration details
        [HttpPost("addUserConfig")]
        [Authorize]
        public async Task<IActionResult> addUserConfig(string id, User user)
        {

            try
            {
                var users = await Task.Run(() => _userRepository.FindByIdAsync(id));
                if (users == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
                }
                if (user.Notification_Setting == null)
                {

                    if (users.Notification_Setting != null)
                    {
                        user.Notification_Setting = users.Notification_Setting;

                    }
                }
                if (user.Notification_Preference == null)
                {
                    if (users.Notification_Preference != null)
                    {
                        user.Notification_Preference = users.Notification_Preference;

                    }
                }
                if (user.Notification_Expires_days == null)
                {
                    if (users.Notification_Expires_days != null)
                    {
                        user.Notification_Expires_days = users.Notification_Expires_days;

                    }
                }

                //Updating user details with config details
                await _userRepository.AddUserConfigAsync(id, user);
                var usersupdated = await Task.Run(() => _userRepository.FindByIdAsync(id));
                return Ok(usersupdated);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Updating User Config" });


            }

        }


        //API for add or update user connection settings details
        [HttpPost("addUpdateConnSettings")]
        [Authorize]
        public async Task<IActionResult> addUpdateConnSettings(string id, string ctype, Connection con)
        {
            try
            {
                var user = await Task.Run(() => _userRepository.FindByIdAsync(id));

                if (user.Connection_Settings == null)
                {

                    var con_set = new Connection_Setting() { Id = ObjectId.GenerateNewId(), Connection_Type = ctype };
                    con_set.Connections.Add(con);
                    con.Id = ObjectId.GenerateNewId();
                    user.Connection_Settings = new Collection<Connection_Setting>() { con_set };

                    await _userRepository.AddConnSettingsAsync(id, user.Connection_Settings);


                }

                Connection_Setting Conn_Set = user.Connection_Settings.Where(c => c.Connection_Type == ctype).FirstOrDefault();
                if (Conn_Set == null)
                {

                    Conn_Set = new Connection_Setting() { Id = ObjectId.GenerateNewId(), Connection_Type = ctype };
                    con.Id = ObjectId.GenerateNewId();
                    Conn_Set.Connections = new Collection<Connection> { con };
                    await _userRepository.AddConnSettingItemAsync(id, Conn_Set);

                }
                foreach (Connection_Setting conn in user.Connection_Settings)
                {
                    foreach (Connection co in conn.Connections)
                    {
                        if (co.Connection_Name == con.Connection_Name)
                        {
                            return Ok(new { ErrorCode = "409", ErrorMessege = "Connection Name Can't Be Duplicate" });

                        }
                    }
                }

                await _userRepository.AddOrUpdateConnectionAsync(id, ctype, con);
                var updated = await Task.Run(() => _userRepository.FindByIdAsync(id));
                return Ok(updated);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Add-Update User Connections" });


            }



        }

        //Update Connection
        [HttpPost("UpdateConnection")]
        [Authorize]
        public async Task<IActionResult> UpdateConnection(string id, string ctype, Connection con)
        {
            try
            {
                var users = await Task.Run(() => _userRepository.FindByIdAsync(id));
                if (users == null)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
                }
                else
                {
                    foreach (Connection_Setting conn in users.Connection_Settings)
                    {
                        foreach (Connection co in conn.Connections)
                        {
                            if (co.Id != con.Id && co.Connection_Name == con.Connection_Name)
                            {
                                return Ok(new { ErrorCode = "409", ErrorMessege = "Connection Name Can't Be Duplicate" });

                            }
                        }
                    }

                    var Connections = users.Connection_Settings
                   .Where(y => y.Connection_Type == ctype).SelectMany(x => x.Connections).ToList();
                    await _userRepository.AddOrUpdateConnectionAsync(id, ctype, con);
                    var usersupdated = await Task.Run(() => _userRepository.FindByIdAsync(id));
                    return Ok(usersupdated);

                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Update User Connections" });
            }
        }

        [HttpPost("DeleteConnection")]
        [Authorize]
        public async Task<IActionResult> DeleteConnection(string userId, string connectionId)
        {
            try
            {
                int flag = 0;
                var user = await Task.Run(() => _userRepository.FindByIdAsync(userId));
                foreach (Connection_Setting con_set in user.Connection_Settings)
                {
                    foreach (Connection con in con_set.Connections)
                    {
                        if (con.Id == new ObjectId(connectionId))
                            flag++;
                    }
                }
                if (flag == 0)
                {
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Connection Not Found" });

                }


                await Task.Run(() => _userRepository.DeleteConnectionAsync(userId, connectionId));

                return Ok(new { ResponseCode = "200", ResponseMessege = "Connection Deleted Successfully" });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Delete User Connections" });

            }
        }

        //API to get users detail by user id
        [HttpGet("getUserConnections")]
        [Authorize]
        public async Task<IActionResult> GetUserConnections(string id)
        {
            _logger.LogInformation("Queried Users Data");
            try
            {
                //Getting perticular user details
                var user = await Task.Run(() => _userRepository.FindByIdAsync(id));
                // user.Connection_Settings.Where(x=>x.Connection_Type=="Google")
                if (user == null)
                {//If user not found
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
                }
                else
                {
                    foreach (Connection_Setting connSetting in user.Connection_Settings)
                    {
                        foreach (Connection con in connSetting.Connections)
                        {
                            con.Connection_Type = connSetting.Connection_Type;

                        }
                    }
                    //If user is present in db
                    return Ok(user.Connection_Settings);
                }
            }
            catch (Exception ex)

            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Fetching User Connections" });

            }
            return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
        }
        //API to get users detail by user id
        [HttpGet("getUserConnByType")]
        [Authorize]
        public async Task<IActionResult> getUserConnByType(string id, string type)
        {
            _logger.LogInformation("Queried Users Data");
            try
            {
                //Getting perticular user details
                var user = await Task.Run(() => _userRepository.FindByIdAsync(id));
                // user.Connection_Settings.Where(x=>x.Connection_Type=="Google")
                if (user == null)
                {//If user not found
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
                }
                else
                {
                    var connType = user.Connection_Settings.Where(x => x.Connection_Type == type);
                    return Ok(connType);
                    // return Ok(user.Connection_Settings);
                }
            }
            catch (Exception ex)

            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Fetching User Connections" });

            }
            return Ok(new { ErrorCode = "40401", ErrorMessege = "User Not Found, Please Register!" });
        }

        //Send Email 
        [HttpGet("SendEmail")]

        public async Task<IActionResult> SendEmail(String Message, String Email)
        {
            //get email settings from admin config
            var adminConfig = await Task.Run(() => _adminconfigRepository
             .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());

            //Sending mails to admin for approval of user
            await Task.Run(() => _userRepository.SendShedulerEmail(adminConfig, Message, Email));

            return Ok(Message);
        }

        //upload a user
        [HttpPost("Upsert")]
        [Authorize]
        public async Task<IActionResult> Upsert(User _user)
        {
            await _userRepository.UpsertOne(_user);
            return Ok(_user);
        }
        //Upload list of users
        [HttpPost("bulkUpsert")]
        [Authorize]
        public async Task<IActionResult> bulkUpsert(List<User> _users)
        {
            try
            {

                /*  foreach (User _user in _users)
                       _user.user_origin_code = Convert.ToString(Enums.UserOrigin.BULK)+ _user.Id;*/
                foreach (User user in _users)
                {
                    //Guid generation
                    user.UserGuId = Guid.NewGuid();
                    //User Id generation
                    user.Id = ObjectId.GenerateNewId();
                    user.user_origin_code = Convert.ToString(Enums.UserOrigin.BULK);
                    user.Userstatus = Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION);
                    //Generate password
                    user.Password = await Task.Run(() => _userRepository.GeneratePassword());
                    var GeneratedPass = user.Password;
                    var Password = Cryptograph.EncryptPassword(user.Password);
                    user.Password = Password.Hash;
                    user.Salt = Password.Salt;
                    user.PasswordSetDateTime = DateTime.Now;
                    //Defaulting role
                    AccessRule rule = new AccessRule();
                    rule.Feature = "View";
                    rule.Access = true;
                    Role role1 = new Role();
                    role1.RoleName = "Viewer";
                    role1.AccessRules.Add(rule);
                    user.Role = role1;
                    //Adding username
                    var s1 = "";
                    var s2 = user.LastName;
                    if (s2.Count() >= 7)
                    {
                        s1 = user.FirstName.Substring(0, 1) + user.LastName.Substring(0, 7);
                    }
                    else
                    {
                        int i = 1;
                        while (s2.Count() <= 7)
                        {

                            s2 = s2 + i.ToString();
                            i++;
                        }
                        s1 = user.FirstName.Substring(0, 1) + s2.Substring(0, 7);

                    }



                    user.Username = s1;
                    //get email settings from admin config
                    var adminConfig = await Task.Run(() => _adminconfigRepository
                       .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());

                    //Sending mail to user for verification
                    await Task.Run(() => _userRepository.SendEmailUser(user, GeneratedPass, adminConfig));
                }
                await _userRepository.UpsertBulk(_users);
                return Ok(_users);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Failed To Add Bulk Users" });

            }
        }
        //Upload list of users from AD,Azure and Google
        [HttpPost("bulkEAUpsert")]
        [Authorize]
        public async Task<IActionResult> bulkEAUpsert(List<User> _users)
        {
            try
            {
                /*var flag = 0;
                string emailId = "";*/
                foreach (User user in _users)
                {
                    var existUser = await Task.Run(() => _userRepository.FilterBy(
                    existUser => existUser.Email == user.Email || (existUser.Phone == user.Phone && existUser.Phone != null && existUser.Phone != "")
                    ).AsQueryable().FirstOrDefault());
                    if (existUser == null)
                    {
                        //Guid generation
                        user.UserGuId = Guid.NewGuid();
                        //User Id generation
                        user.Id = ObjectId.GenerateNewId();
                        if (user.Role == null)
                        {
                            return Ok(new { ErrorCode = "409", ErrorMessege = "Please Enter Role Details" });

                        }

                        user.user_origin_code = user.user_origin_code.ToUpper();
                        user.Userstatus = Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION);
                        //Generate password
                        user.Password = await Task.Run(() => _userRepository.GeneratePassword());
                        var GeneratedPass = user.Password;
                        var Password = Cryptograph.EncryptPassword(user.Password);
                        user.Password = Password.Hash;
                        user.Salt = Password.Salt;
                        user.PasswordSetDateTime = DateTime.Now;

                        /* //Defaulting role
                         AccessRule rule = new AccessRule();
                         rule.Feature = "View";
                         rule.Access = true;
                         Role role1 = new Role();
                         role1.RoleName = "Viewer";
                         role1.AccessRules.Add(rule);
                         user.Role = role1;*/
                        //Adding username
                        string s1 = "";
                        var s2 = user.LastName;
                        if (s2.Count() >= 7)
                        {
                            s1 = user.FirstName.Substring(0, 1) + user.LastName.Substring(0, 7);
                        }
                        else
                        {
                            int i = 1;
                            while (s2.Count() <= 7)
                            {

                                s2 = s2 + i.ToString();
                                i++;
                            }


                            s1 = user.FirstName.Substring(0, 1) + s2.Substring(0, 7);

                        }

                        user.Username = s1;
                        //get email settings from admin config
                        var adminConfig = await Task.Run(() => _adminconfigRepository
                           .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());

                        //Sending mail to user for verification
                        await Task.Run(() => _userRepository.SendEmailUser(user, GeneratedPass, adminConfig));
                    }
                    else
                    {
                        return Ok(new { ErrorCode = "409", ErrorMessege = "User with Email Id " + user.Email + " Already Exists" });

                    }
                }
                await _userRepository.UpsertBulk(_users);
                return Ok(_users);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Failed To Add Bulk Users" });

            }
        }

        [HttpGet("userPages")]
        [Authorize]
        public async Task<IActionResult> UserPages(int pageSize, int pageNo)
        {
            var users = await Task.Run(() => _userRepository.FilterBy(x => x.Id != ObjectId.Empty)
                         .Skip((pageNo - 1) * pageSize)
                         .Take(pageSize)
                         .OrderBy(x => x.Username));

            return Ok(users);

        }
        //Upload the bulk users
        [HttpPost("UploadBulkUsers")]
        [Authorize]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> UploadBulkUsers()
        {
            try
            {
                //Cecking Content type of file
                if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
                {
                    ModelState.AddModelError("File",
                        $"The request couldn't be processed (Error 1).");
                    // Log error

                    // return BadRequest(ModelState);
                    return Ok(new { ErrorCode = "409", ErrorMessege = "The request couldn't be processed (Error 1)." });

                }

                // Accumulate the form data key-value pairs in the request (formAccumulator).
                var formAccumulator = new KeyValueAccumulator();
                var trustedFileNameForDisplay = string.Empty;
                var untrustedFileNameForStorage = string.Empty;
                var streamedFileContent = Array.Empty<byte>();

                var boundary = MultipartRequestHelper.GetBoundary(
                    MediaTypeHeaderValue.Parse(Request.ContentType),
                    _defaultFormOptions.MultipartBoundaryLengthLimit);
                var reader = new MultipartReader(boundary, HttpContext.Request.Body);

                var section = await reader.ReadNextSectionAsync();
                var extension = "CSV";

                while (section != null)
                {
                    var hasContentDispositionHeader =
                        ContentDispositionHeaderValue.TryParse(
                            section.ContentDisposition, out var contentDisposition);

                    if (hasContentDispositionHeader)
                    {
                        if (MultipartRequestHelper
                            .HasFileContentDisposition(contentDisposition))
                        {
                            untrustedFileNameForStorage = contentDisposition.FileName.Value;
                            // Don't trust the file name sent by the client. To display
                            // the file name, HTML-encode the value.
                            if (untrustedFileNameForStorage.EndsWith(".xls") || untrustedFileNameForStorage.EndsWith(".xlsx"))
                                extension = "XL";
                            else if (untrustedFileNameForStorage.EndsWith(".txt") || untrustedFileNameForStorage.EndsWith(".csv"))
                                extension = "CSV";

                            trustedFileNameForDisplay = WebUtility.HtmlEncode(
                                    contentDisposition.FileName.Value);

                            streamedFileContent =
                                await FileHelpers.ProcessStreamedFile(section, contentDisposition,
                                    ModelState, _permittedExtensions, _fileSizeLimit);

                            if (!ModelState.IsValid)
                            {
                                string errorMessage = "File Error";
                                foreach (var modelStateEntry in ModelState.Values)
                                {
                                    foreach (var error in modelStateEntry.Errors)
                                    {
                                        errorMessage = error.ErrorMessage;

                                    }
                                }
                                // return BadRequest(ModelState);
                                return Ok(new { ErrorCode = "409", ErrorMessege = errorMessage });


                            }
                        }
                        else if (MultipartRequestHelper
                            .HasFormDataContentDisposition(contentDisposition))
                        {
                            // Don't limit the key name length because the 
                            // multipart headers length limit is already in effect.
                            var key = HeaderUtilities
                                .RemoveQuotes(contentDisposition.Name).Value;
                            var encoding = Cryptograph.GetEncoding(section);

                            if (encoding == null)
                            {
                                ModelState.AddModelError("File",
                                    $"The request couldn't be processed (Error 2).");
                                // Log error

                                // return BadRequest(ModelState);
                                return Ok(new { ErrorCode = "409", ErrorMessege = "The request couldn't be processed (Error 2)." });

                            }

                            using (var streamReader = new StreamReader(
                                section.Body,
                                encoding,
                                detectEncodingFromByteOrderMarks: true,
                                bufferSize: 1024,
                                leaveOpen: true))
                            {
                                // The value length limit is enforced by 
                                // MultipartBodyLengthLimit
                                var value = await streamReader.ReadToEndAsync();

                                if (string.Equals(value, "undefined",
                                    StringComparison.OrdinalIgnoreCase))
                                {
                                    value = string.Empty;
                                }

                                formAccumulator.Append(key, value);


                                if (formAccumulator.ValueCount >
                                    _defaultFormOptions.ValueCountLimit)
                                {
                                    // Form key count limit of 
                                    // _defaultFormOptions.ValueCountLimit  is exceeded.
                                    ModelState.AddModelError("File",
                                        $"The request couldn't be processed (Error 3).");
                                    // Log error

                                    //  return BadRequest(ModelState);
                                    return Ok(new { ErrorCode = "409", ErrorMessege = "The request couldn't be processed (Error 3)." });

                                }
                            }
                        }
                    }

                    // Drain any remaining section body that hasn't been consumed and
                    // read the headers for the next section.
                    section = await reader.ReadNextSectionAsync();
                }

                // Bind form data to the model
                var userFile = new BulkUser();
                var formValueProvider = new FormValueProvider(
                    BindingSource.Form,
                    new FormCollection(formAccumulator.GetResults()),
                    CultureInfo.CurrentCulture);

                var bindingSuccessful = await TryUpdateModelAsync(userFile, prefix: "",
                    valueProvider: formValueProvider);

                if (!bindingSuccessful)
                {
                    ModelState.AddModelError("File",
                        "The request couldn't be processed (Error 5).");
                    // Log error

                    // return BadRequest(ModelState);
                    return Ok(new { ErrorCode = "409", ErrorMessege = "The request couldn't be processed (Error 5)." });

                }

                // **!!!!!WARNING!!!!!**
                /*
                 The file here is saved without scanning for any virus/malwares/etc. 
                    - Sharath
                 */
                List<BulkUser> userData = new List<BulkUser>();

                if (extension == "XL")
                    userData = ReadFromExcel.readExcelFromStream(streamedFileContent, _userRepository);
                else if (extension == "CSV")
                    userData = ReadFromExcel.readCSVFromStream(streamedFileContent, _userRepository);

                if (userData==null || userData.Count <= 0)
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "File Is Empty" });
                }

                return Ok(userData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Failed To Upload Bulk Users" });

            }

            return NoContent();
        }

        private string GenerateJSONWebToken(Claim[] userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
              _config["Jwt:Issuer"],
              userInfo,
             expires: DateTime.Now.AddMinutes(120),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
