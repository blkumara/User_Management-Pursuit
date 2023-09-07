using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Pursuit.Context;
using Pursuit.Model;
using Pursuit.Utilities;
using System.Net.NetworkInformation;
/* =========================================================
    Item Name: API's Related to Admin Configuration - ConfigurationController
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

    public class ConfigurationController : ControllerBase
    {
        private readonly IPursuitRepository<Admin_Configuration> _adminconfigRepository;
        private readonly IPursuitRepository<EmailContentConfiguration> _emailconfigRepository;
        private readonly ILogger<ConfigurationController> _logger;
        private readonly IConfiguration configuration;
        public ConfigurationController(IConfiguration configuration,ILogger<ConfigurationController>? logger, IPursuitRepository<Admin_Configuration> adminconfigRepository, IPursuitRepository<EmailContentConfiguration> emailconfigRepository)
        {
            _adminconfigRepository = adminconfigRepository;
            _emailconfigRepository = emailconfigRepository;
            this.configuration = configuration;
            _logger = logger;

        }

       
        //Defaulting Admin Configuration

        [HttpPost("defaultAdminConfiguration")]
        [Authorize]
        public async Task<IActionResult> defaultAdminConfiguration(Admin_Configuration adminconfig)
        {
            string domainName = configuration.GetSection("ConfigSettings:DomainName").Value;
            adminconfig.DomainName = domainName;
            //Inserting admin configuration
            await _adminconfigRepository.InsertOneAsync(adminconfig);
            await _emailconfigRepository.addEmailTemplate();
              return Ok(adminconfig);
           

        }
        [HttpPost("addAdminConfiguration")]
        [Authorize]
        public async Task<IActionResult> addAdminConfiguration(Admin_Configuration adminconfig)
        {
            try
            {
                string domainName = configuration.GetSection("ConfigSettings:DomainName").Value;
                adminconfig.DomainName = domainName;

                //Checking Admin configuration present or not
                var adminconfigs = await Task.Run(() => _adminconfigRepository
                    .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable().FirstOrDefault());
                if (adminconfigs != null)
                {
                    //If configuration is present we just updae the document
                    string id = adminconfigs.Id.ToString();
                    var oldadminconfig = await Task.Run(() => _adminconfigRepository.FindByIdAsync(id));

                    if (adminconfig.Oauth_Settings != null)
                    {
                        if (oldadminconfig.Oauth_Settings != null)
                        {
                            string ctype = adminconfig.Oauth_Settings.FirstOrDefault().Connection_Type;
                            Oauth_Setting oauth = oldadminconfig.Oauth_Settings.Where(c => c.Connection_Type == ctype).FirstOrDefault();
                            if (oauth != null)
                            {
                                adminconfig.Oauth_Settings.FirstOrDefault().Id = oauth.Id;

                                //Updating admin config data for config id
                                await _adminconfigRepository.UpdateOauthConfigAsync(id, adminconfig.Oauth_Settings.FirstOrDefault());
                                var adminconfigupdated1 = await Task.Run(() => _adminconfigRepository.FindByIdAsync(id));
                                return Ok(adminconfigupdated1);

                            }
                            else
                            {
                                oldadminconfig.Oauth_Settings.Add(adminconfig.Oauth_Settings.FirstOrDefault());
                                adminconfig.Oauth_Settings = oldadminconfig.Oauth_Settings;
                                //Updating admin config data for config id
                                await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                            }

                        }
                        else
                        {
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                    }

                    if (adminconfig.Notification_Setting == false)
                    {
                        if (oldadminconfig.Notification_Setting != false)
                        {
                            adminconfig.Notification_Setting = oldadminconfig.Notification_Setting;
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                        else
                        {
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                    }
                    if (adminconfig.Notification_Expires_days == null || adminconfig.Notification_Expires_days == "")
                    {
                        if (oldadminconfig.Notification_Expires_days != null || oldadminconfig.Notification_Expires_days != "")
                        {
                            adminconfig.Notification_Expires_days = oldadminconfig.Notification_Expires_days;
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                        else
                        {
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                    }
                    if (adminconfig.Notification_Preference == null)
                    {
                        if (oldadminconfig.Notification_Preference != null)
                        {
                            adminconfig.Notification_Preference = oldadminconfig.Notification_Preference;
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                        else
                        {
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                    }
                    if (adminconfig.Email_Settings == null)
                    {
                        if (oldadminconfig.Email_Settings != null)
                        {
                            adminconfig.Email_Settings = oldadminconfig.Email_Settings;
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                        else
                        {
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                    }
                    if (adminconfig.Authentication_Settings == null)
                    {
                        if (oldadminconfig.Authentication_Settings != null)
                        {
                            adminconfig.Authentication_Settings = oldadminconfig.Authentication_Settings;
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                        else
                        {
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                    }
                    if (adminconfig.Oauth_Settings == null)
                    {
                        if (oldadminconfig.Oauth_Settings != null)
                        {

                            adminconfig.Oauth_Settings = oldadminconfig.Oauth_Settings;
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                        else
                        {
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                    }

                    if (adminconfig.Two_FA_Settings == null)
                    {
                        if (oldadminconfig.Two_FA_Settings != null)
                        {
                            adminconfig.Two_FA_Settings = oldadminconfig.Two_FA_Settings;
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                        else
                        {
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                    }
                    if (adminconfig.SyncupCred == null)
                    {
                        if (oldadminconfig.SyncupCred != null)
                        {
                            adminconfig.SyncupCred = oldadminconfig.SyncupCred;
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                        else
                        {
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                    }
                    if (adminconfig.ArchivalInfo == null)
                    {
                        if (oldadminconfig.ArchivalInfo != null)
                        {
                            adminconfig.ArchivalInfo = oldadminconfig.ArchivalInfo;
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                        else
                        {
                            //Updating admin config data for config id
                            await _adminconfigRepository.UpdateAdminConfigAsync(id, adminconfig);
                        }
                    }
                    var adminconfigupdated = await Task.Run(() => _adminconfigRepository.FindByIdAsync(id));
                    return Ok(adminconfigupdated);

                }
                else
                {

                    if (adminconfig.Email_Settings != null)
                    {
                        foreach (Email_Configuration ec in adminconfig.Email_Settings.Email_Configuration)
                        {

                            var IMPassword = Cryptograph.EncryptPassword(ec.Imap_Config.Imap_User_Pwd);
                            ec.Imap_Config.Imap_User_Pwd = IMPassword.Hash;
                            ec.Imap_Config.Salt = IMPassword.Salt;

                            var SMPassword = Cryptograph.EncryptPassword(ec.Smtp_Config.Smtp_User_Pwd);
                            ec.Smtp_Config.Smtp_User_Pwd = SMPassword.Hash;
                            ec.Smtp_Config.Salt = SMPassword.Salt;

                            var Pop3Password = Cryptograph.EncryptPassword(ec.Pop3_Config.Pop3_User_Pwd);
                            ec.Pop3_Config.Pop3_User_Pwd = Pop3Password.Hash;
                            ec.Pop3_Config.Salt = Pop3Password.Salt;

                        }
                    }

                    //Inserting admin configuration
                    await _adminconfigRepository.InsertOneAsync(adminconfig);
                    return Ok(adminconfig);
                }

                return Ok(new { ErrorCode = "409", ErrorMessege = "Please Check Entered Details" });

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Admin Configuration Failed." });

            }
        }
        [HttpPost("addUpdateEmailConfiguration")]
        [Authorize]
        public async Task<IActionResult> addUpdateEmailConfiguration(EmailContentConfiguration emailconfig)
        {
            try
            {
                var emailconfigs = await Task.Run(() => _emailconfigRepository
                       .FilterBy(x => x.EmailType == emailconfig.EmailType).AsQueryable().FirstOrDefault());
                if (emailconfigs != null)
                {
                    string id = emailconfigs.Id.ToString();
                    await _emailconfigRepository.ReplaceEmailConfigAsync(id, emailconfig);
                    var updatedemail = await Task.Run(() => _emailconfigRepository
                           .FilterBy(x => x.EmailType == emailconfig.EmailType).AsQueryable().FirstOrDefault());
                    return Ok(updatedemail);

                }
                else
                {
                    emailconfig.Id=ObjectId.GenerateNewId();
                    await _emailconfigRepository.InsertOneAsync(emailconfig);
                    return Ok(emailconfig);
                }
              

            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Email Template Configuration Failed" });

            }
        }

        [HttpGet("getAdminConfiguration")]

        public async Task<IActionResult> getAdminConfiguration()
        {

            try
            {
                Guid guid;
                //Getting all users from the database
                var adminconfig = await Task.Run(() => _adminconfigRepository
                .FilterBy(x => x.Id != ObjectId.Empty).AsQueryable());

                if (adminconfig.Count() <= 0)
                {
                    return Ok(new { ErrorCode = "409", ErrorMessege = "Your Application Is Not Setup Please Contact Organization Admin" });
                }
                return Ok(adminconfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                Console.WriteLine(ex.GetType);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Fetching Admin Configuration" });

            }
            return NoContent();
        }

        [HttpGet("getEmailConfiguration")]

        public async Task<IActionResult> getEmailConfiguration(string EmailType)
        {

            try
            {
                Guid guid;
                //Getting all users from the database
                var emailconfig = await Task.Run(() => _emailconfigRepository
                .FilterBy(x => x.EmailType == EmailType).AsQueryable());
                if (emailconfig.Count() > 0)
                    return Ok(emailconfig);
                else
                    return Ok(new { ErrorCode = "40401", ErrorMessege = "Email Type Not Found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Ok(new { ErrorCode = "409", ErrorMessege = "Error Occured While Fetching Email Templates" });

            }
          
        }

    }
}
