using MongoDB.Bson;
using MongoDB.Driver;

using Pursuit.Model;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Net;
using Pursuit.Utilities;
using MongoDB.Driver.Linq;
using System.Web;
using Microsoft.AspNetCore.Hosting;
using Log = Pursuit.Model.Log;
using System.Security.Policy;
using Microsoft.Graph.Models.Security;
using System.Net.Mime;
using System.Net.Http;
using static System.Net.WebRequestMethods;

/* =========================================================
Item Name: Repository Class-PursuitRepository
Author: Ortusolis for EvolveAccess Team
Version: 1.0
Copyright 2022 - 2023 - Evolve Access
============================================================ */
namespace Pursuit.Context
{
    public class PursuitRepository<TDocument> : IPursuitRepository<TDocument>
    where TDocument : IDocument
    {
        private readonly IMongoCollection<TDocument> _collection;
        private readonly IMongoCollection<User> _CSCollection;
        private readonly IMongoCollection<Admin_Configuration> _AdminCollection;
        private readonly IMongoCollection<EmailContentConfiguration> _MailTemplate;

        private readonly IMongoCollection<Connection_Setting> _conCollection;

        private readonly IMongoCollection<Log> _logcollection;
      

        public PursuitRepository(IPursuitDBSettings settings)
        {
            var database = new MongoClient(settings.ConnectionString).GetDatabase(settings.DatabaseName);
            //  _collection = database.GetCollection<TDocument>("AppUsers");
            _collection = database.GetCollection<TDocument>(GetCollectionName(typeof(TDocument)));
            _CSCollection = database.GetCollection<User>("AppUsers");
            _AdminCollection = database.GetCollection<Admin_Configuration>("AdminConfig");
            _MailTemplate = database.GetCollection<EmailContentConfiguration>("MailTemplates");
            _conCollection = database.GetCollection<Connection_Setting>("ConnectionMgt");
            _logcollection = database.GetCollection<Log>("log");
            
        }

        private protected string GetCollectionName(Type documentType)
        {
            return ((BsonCollectionAttribute)documentType
                .GetCustomAttributes(typeof(BsonCollectionAttribute), true)
                .FirstOrDefault())?.CollectionName; //?? "AppUsers"; //default collection may be at end of development -gts
        }

        public virtual IQueryable<TDocument> AsQueryable()
        {
            return _collection.AsQueryable();
        }

        public virtual IEnumerable<TDocument> FilterBy(
            Expression<Func<TDocument, bool>> filterExpression)
        {
            return _collection.Find(filterExpression).ToEnumerable();
        }
        public virtual Task RemoveManyAsync(
           Expression<Func<TDocument, bool>> filterExpression)
        {
            return _collection.DeleteManyAsync(filterExpression);
        }
        public virtual IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression,
            Expression<Func<TDocument, TProjected>> projectionExpression)
        {
            return _collection.Find(filterExpression).Project(projectionExpression).ToEnumerable();
        }

        public virtual TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression)
        {
            return _collection.Find(filterExpression).FirstOrDefault();
        }

        public virtual Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return Task.Run(() => _collection.Find(filterExpression).FirstOrDefaultAsync());

        }
        //Get Logs by level
        public virtual List<BsonDocument> FindloglevelAsync(string level)
        {

            var filter = Builders<Log>.Filter.Eq(x=>x.Level,level);
            var projection = Builders<Log>.Projection
                .Include(e => e.Id)
                .Include(e => e.Level)
                .Include(e => e.UtcTimeStamp)
                .Include(e => e.RenderedMessage)
                .Include(e => e.Exception)
                .Include(e => e.MessageTemplate)
                .Include(e => e.Properties);
            var sort = Builders<Log>.Sort.Descending("_id");
            var cursor = _logcollection.Find(filter).Project(projection).Sort(sort).Limit(1000).ToList();
            return cursor;

        }
        //Get Logs
        public virtual List<BsonDocument> FindlogAsync()
        {

            var filter = Builders<Log>.Filter.Empty;
            var projection = Builders<Log>.Projection
                .Include(e => e.Id)
                .Include(e => e.Level)
                .Include(e => e.UtcTimeStamp)
                .Include(e => e.RenderedMessage)
                .Include(e=>e.Exception)
                .Include(e=>e.MessageTemplate)
                .Include(e=>e.Properties);
            var sort = Builders<Log>.Sort.Descending("_id");
            var cursor = _logcollection.Find(filter).Project(projection).Sort(sort).ToList();
            return cursor;
                
        }
        //Delete Logs
        public String DeleteLogAsync()
        {

            try
            {

                var filter = Builders<Log>.Filter.Empty;
                _logcollection.DeleteMany(filter);
                return "Deleted";

            }
            catch (Exception ex)
            {
                throw;
            }

        }
        //Find the data by id
        public virtual TDocument FindById(string id)
        {
            var objectId = new ObjectId(id);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            return _collection.Find(filter).SingleOrDefault();
        }

        public virtual Task<TDocument> FindByIdAsync(string id)
        {
            return Task.Run(() =>
            {
                var objectId = new ObjectId(id);
                var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
                return _collection.Find(filter).SingleOrDefaultAsync();
            });
        }

        //Insert Data
        public virtual void InsertOne(TDocument document)
        {

            _collection.InsertOne(document);
        }

        public virtual Task InsertOneAsync(TDocument document)
        {
            return Task.Run(() => _collection.InsertOneAsync(document));
        }

        public void InsertMany(ICollection<TDocument> documents)
        {
            _collection.InsertMany(documents);
        }

        public virtual async Task InsertManyAsync(ICollection<TDocument> documents)
        {
            await _collection.InsertManyAsync(documents);
        }
        //Update data by id
        public void ReplaceOne(string userId, User user)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDocument>.Update
                            .Set("Phone", user.Phone).Set("Email", user.Email).Set("Username", user.Username).Set("ProfileImage", user.ProfileImage)
                            .Set("FirstName", user.FirstName).Set("LastName", user.LastName).Set("Userstatus", user.Userstatus).Set("Role", user.Role);
            _collection.UpdateOne(filter, update);


        }

        public virtual async Task ReplaceOneAsync(string userId, User user)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);

            var update = Builders<TDocument>.Update
                            .Set("Phone", user.Phone).Set("Email", user.Email).Set("Username", user.Username).Set("ProfileImage", user.ProfileImage)
                            .Set("FirstName", user.FirstName).Set("LastName", user.LastName).Set("Userstatus", user.Userstatus).Set("Role", user.Role);
            await _collection.UpdateOneAsync(filter, update);

        }
        public virtual async Task ReplaceEmailConfigAsync(string Id, EmailContentConfiguration emailconfig)
        {
            var objectId = new ObjectId(Id);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);

            var update = Builders<TDocument>.Update
                            .Set("EmailSubject", emailconfig.EmailSubject).Set("EmailBody", emailconfig.EmailBody)
                            .Set("EmailFooter", emailconfig.EmailFooter).Set("EmailHeader", emailconfig.EmailHeader);
            await _collection.UpdateOneAsync(filter, update);

        }
        //Add user config data by id
        public void AddUserConfig(string userId, User user)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDocument>.Update
                          .Set("Notification_Setting", user.Notification_Setting)
                          .Set("Notification_Preference", user.Notification_Preference)
                          .Set("Notification_Expires_days", user.Notification_Expires_days);
            _collection.UpdateOne(filter, update);


        }

        public virtual async Task AddUserConfigAsync(string userId, User user)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDocument>.Update
                           .Set("Notification_Setting", user.Notification_Setting)
                           .Set("Notification_Preference", user.Notification_Preference)

                          .Set("Notification_Expires_days", user.Notification_Expires_days); 
            await _collection.UpdateOneAsync(filter, update);

        }
        //Add or Update admin notifications by id
        public virtual async Task UpdateNotificationAsync(string userId, ICollection<Notification> notifications)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDocument>.Update
                          .Set("Notifications", notifications);
            _collection.UpdateOne(filter, update);
        }
        //Add or Update user connection settings data by id
        public virtual async Task AddConnSettingsAsync(string userId, ICollection<Connection_Setting> con_set)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDocument>.Update
                          .Set("Connection_Settings", con_set);
            _collection.UpdateOne(filter, update);
        }


        public virtual async Task AddConnSettingItemAsync(string userId, Connection_Setting con_set)
        {
            var objectId = new ObjectId(userId);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDocument>.Update.Push("Connection_Settings", con_set);
            _collection.UpdateOne(filter, update);
        }


        public virtual IEnumerable<Connection_Setting> FindConnection(
         string userId)
        {
            Console.WriteLine("a");
            var filter =
            

            Builders<Connection_Setting>.Filter.ElemMatch<Connection_Setting>(
   "connections",
   Builders<Connection_Setting>.Filter.Eq("userId", userId));
            return _conCollection.Find(filter).ToEnumerable();
        }

        public virtual async Task AddOrUpdateConnectionAsync(string userId, string ctype, Connection con)
        {

            var objectId = new ObjectId(userId);

            if (!con.Id.Equals(new ObjectId()))
            {
                try
                {
                    var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Where(x => x.Id == objectId)
                ,
                Builders<User>.Filter
                    .ElemMatch(z => z.Connection_Settings, a => a.Connection_Type == ctype && a.Connections.Any(i => i.Id == con.Id))
                    );

                    var update = Builders<User>.Update.Set("Connection_Settings.$[i].Connections.$[j]", con);
                    var arrayFilters1 = new List<ArrayFilterDefinition> { new JsonArrayFilterDefinition<User>("{'i.Connection_Type':'"+ ctype +"'}"),
                     new JsonArrayFilterDefinition<User>("{'j._id': '"+ con.Id.ToString() +"' }") };
                    var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters1, IsUpsert = true };

                    _CSCollection.UpdateOne(filter, update, updateOptions);

                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            else
            {
                con.Id = ObjectId.GenerateNewId();
                var filt = Builders<User>.Filter.And(
               Builders<User>.Filter.Where(x => x.Id == objectId),
               Builders<User>.Filter
                   .ElemMatch(z => z.Connection_Settings, a => a.Connection_Type == ctype)
               );
                var upd = Builders<User>.Update.Push("Connection_Settings.$.Connections", con);
                await _CSCollection.UpdateOneAsync(filt, upd);
            }

        }

        public virtual async Task UpdateConnectionAsync(string ctype, Connection con)
        {

            if (!con.Id.Equals(new ObjectId()))
            {
                try
                {
                    var filter = Builders<Connection_Setting>.Filter.Where(a => a.Connection_Type == ctype && a.Connections.Any(i => i.Id == con.Id));
                    

                    var update = Builders<Connection_Setting>.Update.Set("Connections.$[j]", con);
                    var arrayFilters1 = new List<ArrayFilterDefinition> { 
                     new JsonArrayFilterDefinition<Connection_Setting>("{'j._id': '"+ con.Id.ToString() +"' }") };
                    var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters1, IsUpsert = true };

                    _conCollection.UpdateOne(filter, update, updateOptions);

                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            else
            {
                con.Id = ObjectId.GenerateNewId();
                var filt = Builders<Connection_Setting>.Filter.Where(a => a.Connection_Type == ctype);

                var update = Builders<Connection_Setting>.Update.Push("Connections", con);
                await _conCollection.UpdateOneAsync(filt, update);
            }

        }
        public virtual async Task DeleteConnectionAsync(string userId, string connectionId)
        {

            try
            {

                var filter = Builders<User>.Filter.Where(x => x.Id == new ObjectId(userId));

                var update = Builders<User>.Update.PullFilter("Connection_Settings.$[].Connections",
                        Builders<Connection>.Filter.Eq("_id", connectionId));

                await _CSCollection.UpdateOneAsync(filter, update);

            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public virtual async Task DeleteConnAsync(string ctype, string connectionId)
        {

            try
            {

                var filter = Builders<Connection_Setting>.Filter.Where(a => a.Connection_Type == ctype);

                var update = Builders<Connection_Setting>.Update.PullFilter("Connections",
                        Builders<Connection>.Filter.Eq("_id", connectionId));

                await _conCollection.UpdateOneAsync(filter, update);

            }
            catch (Exception ex)
            {
                throw;
            }

        }



        //update admin config data by id
        public void UpdateAdminConfig(string Id, Admin_Configuration admin)
        {
            var objectId = new ObjectId(Id);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDocument>.Update
                          .Set("Notification_Expires_days", admin.Notification_Expires_days)
                          .Set("Notification_Setting", admin.Notification_Setting)
                          .Set("Notification_Preference", admin.Notification_Preference)
                          .Set("Email_Settings", admin.Email_Settings)
                          .Set("Authentication_Settings", admin.Authentication_Settings)
                          .Set("Oauth_Settings", admin.Oauth_Settings)
                          .Set("Two_FA_Settings", admin.Two_FA_Settings)
                          .Set("SyncupCred", admin.SyncupCred);
            _collection.UpdateOne(filter, update);


        }

        public virtual async Task UpdateAdminConfigAsync(string Id, Admin_Configuration admin)
        {
            var objectId = new ObjectId(Id);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDocument>.Update
                          .Set("Notification_Expires_days", admin.Notification_Expires_days)
                          .Set("Notification_Setting", admin.Notification_Setting)
                          .Set("Notification_Preference", admin.Notification_Preference)
                          .Set("Email_Settings", admin.Email_Settings)
                          .Set("Authentication_Settings", admin.Authentication_Settings)
                          .Set("Oauth_Settings", admin.Oauth_Settings)
                          .Set("Two_FA_Settings", admin.Two_FA_Settings)
                          .Set("SyncupCred", admin.SyncupCred);
            await _collection.UpdateOneAsync(filter, update);

        }

        public virtual async Task UpdateOauthConfigAsync(string adminId, Oauth_Setting oauth)
        {
            var objectId = new ObjectId(adminId);
            var filter = Builders<Admin_Configuration>.Filter.And(
               Builders<Admin_Configuration>.Filter.Where(x => x.Id == objectId)
               ,
               Builders<Admin_Configuration>.Filter
                   .ElemMatch(z => z.Oauth_Settings, a => a.Connection_Type == oauth.Connection_Type)
                   );
            var update = Builders<Admin_Configuration>.Update.Set("Oauth_Settings.$[i]", oauth);
            var arrayFilters1 = new List<ArrayFilterDefinition> {
                new JsonArrayFilterDefinition<Admin_Configuration>("{'i._id':'" + oauth.Id.ToString() + "'}") };
            var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters1, IsUpsert = true };

            await _AdminCollection.UpdateOneAsync(filter, update, updateOptions);

        }

        //To update the user status for particular id

        public void UpdateStatus(string userId, string userStatus)
        {
            var objectId = new ObjectId(userId);

            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDocument>.Update
                         .Set("Userstatus", userStatus);
            _collection.UpdateOne(filter, update);

        }

        public virtual async Task UpdateStatusAsync(string userId, string userStatus)
        {
            var objectId = new ObjectId(userId);

            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDocument>.Update
                         .Set("Userstatus", userStatus);
            await _collection.UpdateOneAsync(filter, update);
        }
        //To update the user password for particular id
        public void UpdatePassword(string userId, string password, byte[] salt, DateTime PasswordSetDateTime)
        {
            var objectId = new ObjectId(userId);

            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDocument>.Update
                         .Set("Password", password).Set("Salt", salt).Set("PasswordSetDateTime", PasswordSetDateTime);
            _collection.UpdateOne(filter, update);

        }

        public virtual async Task UpdatePasswordAsync(string userId, string password, byte[] salt, DateTime PasswordSetDateTime)
        {
            var objectId = new ObjectId(userId);

            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            var update = Builders<TDocument>.Update
                         .Set("Password", password).Set("Salt", salt).Set("PasswordSetDateTime", PasswordSetDateTime);
            await _collection.UpdateOneAsync(filter, update);
        }
        //UpdateNotificationStatus
        public virtual async Task UpdateNotificationStatusAsync(NotifyStatusRequest req)
        {
           
            var objectId = new ObjectId(req.UserId);
            var notificationId = new ObjectId(req.NotificationId);
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Where(x=>x.Id == objectId),
                Builders<User>.Filter.ElemMatch(y=>y.Notifications, z=> z.Id== notificationId)

                );
            var update = Builders<User>.Update
                         .Set("Notifications.$[i].NotificationStatus", req.Status);
            var arrayFilters1 = new List<ArrayFilterDefinition> {
                new JsonArrayFilterDefinition<User>("{'i._id':'" + req.NotificationId + "'}") };
            var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters1, IsUpsert = true };

            await _CSCollection.UpdateOneAsync(filter, update, updateOptions);
        }

        //forgot password sending email
        public string ForgotPassword(User user, Admin_Configuration adminConfig)
        {

            string strNewPassword = GenerateOTP().ToString();


            if (user != null)
            {

                string username = user.FirstName;
                string useremail = user.Email;
                string subject = "Password Recovery";
                string EmailHeader = "Hi {0},";
                string EmailBody = "<br /><br />Your OTP is {1}.<br /><br />";
                string EmailFooter = "Thanks And Regards You.<br />Evolve Access Support Team";
                string domain = adminConfig.DomainName;
                string imagePath = "https://" + domain + "/assets/emailtempimages/ea_black.png";
                string imageVerificationPath = "https://" + domain + "/assets/emailtempimages/mail_verification.png";

                string format = string.Format(EmailHeader + EmailBody + EmailFooter, username, strNewPassword);
                string body = $@"<!DOCTYPE html>
                            <html>
                            <head>
                                <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                                <style>
                                    .row:after {{
                                        content: """";
                                        display: table;
                                        clear: both;
                                    }}
                                </style>
                            </head>
                            <body>
                                <div class="""" style=""background-color: #808080;height: 80px; box-sizing: border-box;"">
                                    <h1 style=""color: white; padding-left: 3%;padding-top: 2%;"">Password Recovery
                                        <img src=""{imagePath}"" alt="""" width=""50px""    height=""30%"" style=""float: right;margin-right: 2%;"">
                                    </h1>
                                </div>
                                <div class=""row"">
                                    <div class=""column"" style=""float: left;box-sizing: border-box;
                                        width: 33.33%;
                                        padding: 5px;
                                        height: 100%;"">
                                        <img src=""{imageVerificationPath}"" alt=""Registration Logo"" width=""70%"" height=""100px"">
                                    </div>
                                    <div class=""column"" style=""float: left;box-shadow: 0 8px 8px 0 rgba(0,0,0,0.2);
                                        transition: 0.3s;box-sizing: border-box;
                                        width: 66.66%;
                                        padding: 10px;
                                        height: 100%;"">
           
                                        <p>{format}</p>
            
                                    </div>
                                </div>
                            </body>
                            </html>";


                string response = SendEmail(adminConfig, useremail, subject, body);

            }

            return strNewPassword;
        }

        //sending otp to mail for verification
        public string emailVerification(string email, Admin_Configuration adminConfig)
        {
            //Generation otp
            string strNewPassword = GenerateOTP().ToString();
            //Sending otp to mail
            string subject = "Email Verification";
            string EmailHeader = "Hi,";
            string EmailBody = "<br /><br />Your OTP for login verification is {0}.<br /><br />";
            string EmailFooter = "Thanks And Regards.<br />Evolve Access Support Team";
            string domain = adminConfig.DomainName;
            string imagePath = "https://" + domain + "/assets/emailtempimages/ea_black.png";
            string imageVerificationPath = "https://" + domain + "/assets/emailtempimages/mail_verification.png";

            string format = string.Format(EmailHeader + EmailBody + EmailFooter, strNewPassword);
            string body = $@"<!DOCTYPE html>
                            <html>
                            <head>
                                <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                                <style>
                                    .row:after {{
                                        content: """";
                                        display: table;
                                        clear: both;
                                    }}
                                </style>
                            </head>
                            <body>
                                <div class="""" style=""background-color: #808080;height: 80px; box-sizing: border-box;"">
                                    <h1 style=""color: white; padding-left: 3%;padding-top: 2%;"">Email Verification
                                        <img src=""{imagePath}"" alt="""" width=""50px""    height=""30%"" style=""float: right;margin-right: 2%;"">
                                    </h1>
                                </div>
                                <div class=""row"">
                                    <div class=""column"" style=""float: left;box-sizing: border-box;
                                        width: 33.33%;
                                        padding: 5px;
                                        height: 100%;"">
                                        <img src=""{imageVerificationPath}"" alt=""Registration Logo"" width=""70%"" height=""100px"">
                                    </div>
                                    <div class=""column"" style=""float: left;box-shadow: 0 8px 8px 0 rgba(0,0,0,0.2);
                                        transition: 0.3s;box-sizing: border-box;
                                        width: 66.66%;
                                        padding: 10px;
                                        height: 100%;"">
           
                                        <p>{format}</p>
            
                                    </div>
                                </div>
                            </body>
                            </html>";


            string response = SendEmail(adminConfig, email, subject, body);
            return strNewPassword;

        }
        //sending otp to mail for verification
        public string loginVerification(string email, Admin_Configuration adminConfig)
        {
            //Generation otp
            string strNewPassword = GenerateOTP().ToString();
            //Sending otp to mail
            string subject = "Login Verification";
            string EmailHeader = "Hi,";
            string EmailBody = "<br />Your OTP for login verification is {0}.<br />";
            string EmailFooter = "Thanks And Regards.<br />Evolve Access Support Team";
            string domain = adminConfig.DomainName;
            string imagePath = "https://" + domain + "/assets/emailtempimages/ea_black.png";
            string imageVerificationPath = "https://" + domain + "/assets/emailtempimages/mail_verification.png";
            //Login Verification Email Body
            string body = string.Format($@"<!doctype html>
                                            <html lang=""en"">
                                        <head>
                            <meta charset=""utf-8"">
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                            <title>Bootstrap demo</title>
                        </head>
                        <body>
                            <div style=""background-color: #808080; height: 80px;"">
                                <h1 style=""color: white; padding-left: 3%;padding-top: 2%;"">Login Verification
                                    <img src=""{imagePath}"" alt="""" width=""50px"" height=""30%"" style=""float: right; margin-right: 2%;"">
                                </h1>
                            </div>


                             <div class=""row"">
                                    <div class=""column"" style=""float: left;box-sizing: border-box;
                                        width: 33.33%;
                                        padding: 5px;
                                        height: 100%;"">
                                        <img src=""{imageVerificationPath}"" alt=""Registration Logo"" width=""70%"" height=""100px"">
                                    </div>
                                    <div class=""column"" style=""float: left;box-shadow: 0 8px 8px 0 rgba(0,0,0,0.2);
                                        transition: 0.3s;box-sizing: border-box;
                                        width: 66.66%;
                                        padding: 10px;
                                        height: 100%;"">

                                        <p>{EmailHeader}</p>
                                        <p>{EmailBody}</p>
                                        <p>{EmailFooter}</p>
                                    </div>
                                </div>
                            </div>
                        </body>
                        </html>", strNewPassword);
            string response = SendEmail(adminConfig, email, subject, body);

            return strNewPassword;
        }
        //Generate 6 digit random OTP
        public string GenerateOTP()
        {
            string PasswordLength = "6";
            string NewPassword = "";

            string allowedChars = "";
            allowedChars = "1,2,3,4,5,6,7,8,9,0";
            char[] sep = { ',' };
            string[] arr = allowedChars.Split(sep);
            string IDString = "";
            string temp = "";
            Random rand = new Random();
            for (int i = 0; i < Convert.ToInt32(PasswordLength); i++)
            {
                temp = arr[rand.Next(0, arr.Length)];
                IDString += temp;
                NewPassword = IDString;
            }
            return NewPassword;
        }

        //when user register then mail trigger to admin
        public string SendEmailToAdmin(User user, User user2, Admin_Configuration adminConfig)
        {
            if (user != null)
            {

                string username = user.FirstName;

                string lastname = user.LastName;
                string phone = user.Phone;
                string email = user.Email;
                string user2mail = user2.Email;
                string user2name = user2.FirstName;
                string Subject = "New User Registration";
                string EmailHeader = "Hi {0},";
                string EmailBody = "<br />New User has been registered please verify and approve the details of user.<br/><br/>User Details are :<br /><br /> First Name: {1},<br /><br />Last Name: {2},<br /><br />Phone Number: {3},<br /><br />Email ID: {4}<br /><br />";
                string EmailFooter = "Thanks And Regards.<br />Evolve Access Support Team";
                string domain = adminConfig.DomainName;
                string imagePath = "https://" + domain + "/assets/emailtempimages/ea_black.png";
                string imageVerificationPath = "https://" + domain + "/assets/emailtempimages/mail_verification.png";

                //User Registration Email Body 
                string Body = string.Format($@"<!doctype html>
                                            <html lang=""en"">
                                            <head>
                                <meta charset=""utf-8"">
                                <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                                <title>Bootstrap demo</title>
                            </head>
                            <body>

                                <div style=""background-color: #808080; height: 80px;box-sizing: border-box;"">
                                    <h1 style=""color: white; padding-left: 3%;padding-top: 2%;"">New User Registration
                                        <img src=""{imagePath}"" alt="""" width=""50px"" height=""30%"" style=""float: right; margin-right: 2%;"">
                                    </h1>
                                </div>
                                <div style=""display: flex;"">

                                    <div class=""card"" style=""padding: 5%; padding-top: 5%; height: 500px; width: 500px;"">
                                        <div style=""box-shadow: 0 8px 8px 0 rgba(0,0,0,0.2); transition: 0.3s; background: white; padding: 20%; border: 1px solid;"">
                                            <p>{EmailHeader}</p>
                                            <p>{EmailBody}</p>
                                            <p>{EmailFooter}</p>
                                        </div>
                                    </div>
                                </div>
                            </body>
                            </html>", user2name, username, lastname, phone, email);

                string response = SendEmail(adminConfig, user2mail, Subject, Body );
            }

            return "Successfull";

        }

        public Task<string> SendEmailToAdminAsync(User user, User user2, Admin_Configuration adminConfig)
        {
            throw new NotImplementedException();
        }
        //when admin add any user email trigger to user
        public string SendEmailUser(User user, string GeneratedPass, Admin_Configuration adminConfig)
        {
            if (user != null)
            {

                string username = user.FirstName;
                // string role = user.Role.RoleName;
                string id = user.Id.ToString();
                string email = user.Email;
                string password = GeneratedPass;
                string phone = user.Phone;
                string domain = adminConfig.DomainName;
                string EmailHeader = "Hi {0},";
                string EmailBody = "<br />Admin  registered your details please set your new password, Your details are:<br /><br /> First Name: {1},<br /><br />Email ID: {2},<br /><br />Phone Number: {3},<br /><br />Password: {4}<br /><br />";
                string EmailFooter = "Please use this link to login https://"+ domain + "/#/auth/login <br /><br />Thanks And Regards.<br />Evolve Access Support Team";
               
                string imagePath = "https://" + domain + "/assets/emailtempimages/ea_black.png";
                string imageVerificationPath = "https://" + domain + "/assets/emailtempimages/mail_verification.png";

                string Subject = "New User Registration Verification";
               
                //Registration verification Mail Body 
                string Body = string.Format($@"<!doctype html>
                                            <html lang=""en"">
                                            <head>
                                <meta charset=""utf-8"">
                                <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                                <title>Bootstrap demo</title>
                            </head>
                            <body>
                                <div style=""background-color: #808080; height: 80px;"">
                                    <h1 style=""color: white; padding-left: 3%;padding-top: 2%;"">New User Registration Verification
                                        <img src=""{imagePath}"" alt="""" width=""50px"" height=""30%"" style=""float: right; margin-right: 2%;"">
                                    </h1>
                                </div>
                                <div style=""display: flex;"">

                                    <div class=""card"" style=""padding: 7%; padding-top: 5%; height: 500px; width: 500px;"">
                                        <div style=""box-shadow: 0 8px 8px 0 rgba(0,0,0,0.2); transition: 0.3s; background: white; padding: 20%; border: 1px solid;"">
                                            <p>{EmailHeader}</p>
                                            <p>{EmailBody}</p>
                                            <p>{EmailFooter}</p>
                                        </div>
                                    </div>
                                </div>
                            </body>
                            </html>", username, username, email, phone, password);
                string response = SendEmail(adminConfig, email, Subject, Body);
            }

            return "Successfull";
        }
       
        //when admin update user status then mail trigger to user
        public string SendStatusEmailToUser(Admin_Configuration adminConfig, User user, string status, String Password, EmailContentConfiguration emailconfig)
        {
            if (user != null)
            {

                string username = user.FirstName;

                string usermail = user.Email;
              
                string Subject = "Status Change";
                string Body =null;
                string domain = adminConfig.DomainName;
                string imagePath = "https://" + domain + "/assets/emailtempimages/ea_black.png";
                string imageVerificationPath = "https://" + domain + "/assets/emailtempimages/mail_verification.png";
                string loginurl = "https://" + domain+"/#/auth/login";
               
               //Account Verification Email
                if (status == Convert.ToString(Enums.UserStatus.PENDING_VERIFICATION))
                {
                    Body = string.Format($@"<!doctype html>
                                            <html lang=""en"">
                                            <head>
                                <meta charset=""utf-8"">
                                <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                                <title>Bootstrap demo</title>
                            </head>
                            <body>
                                <div style=""background-color: #808080; height: 80px;"">
                                    <h1 style=""color: white; padding-left: 3%; padding-top: 2%;"">Account Pending Verification
                                        <img src=""{imagePath}"" alt="""" width=""50px"" height=""30%"" style=""float: right; margin-right: 2%;"">
                                    </h1>
                                </div>
                                <div style=""display: flex;"">
                                    <div class=""card"" style=""padding: 7%; padding-top: 5%; height: 500px; width: 500px;"">
                                        <div style=""box-shadow: 0 8px 8px 0 rgba(0,0,0,0.2); transition: 0.3s; background: white; padding: 20%; border: 1px solid;"">
                                            <p>{emailconfig.EmailHeader}</p>
                                            <p>{emailconfig.EmailBody}</p>
                                            <p>{emailconfig.EmailFooter}</p>
                                        </div>
                                    </div>
                                </div>
                            </body>
                            </html>", username, usermail, Password, loginurl);
                }
                //Account Activation Email
                if (status == Convert.ToString(Enums.UserStatus.ACTIVE))
                {
                    Body = string.Format($@"<!doctype html>
                                            <html lang=""en"">
                                            <head>
                                <meta charset=""utf-8"">
                                <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                                <title>Bootstrap demo</title>
                            </head>
                            <body>
                                <div style=""background-color: #808080; height: 80px;"">
                                    <h1 style=""color: white; padding-left: 3%; padding-top: 2%;"">Account Activation
                                        <img src=""{imagePath}"" alt="""" width=""50px"" height=""30%"" style=""float: right; margin-right: 2%;"">
                                    </h1>
                                </div>
                                <div style=""display: flex;"">
                                    <div class=""card"" style=""padding: 7%; padding-top: 5%; height: 500px; width: 500px;"">
                                        <div style=""box-shadow: 0 8px 8px 0 rgba(0,0,0,0.2); transition: 0.3s; background: white; padding: 20%; border: 1px solid;"">
                                            <p>{emailconfig.EmailHeader}</p>
                                            <p>{emailconfig.EmailBody}</p>
                                            <p>{emailconfig.EmailFooter}</p>
                                        </div>
                                    </div>
                                </div>
                            </body>
                            </html>", username, loginurl);
                }
                //Account Deactivation Mail
                if (status == Convert.ToString(Enums.UserStatus.DEACTIVE))
                {
                 
                      Body = string.Format($@"<!doctype html>
                                 <html lang=""en"">
                                 <head>
                                <meta charset=""utf-8"">
                                <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                                <title>Bootstrap demo</title>
                            </head>
                            <body>
                                <div style=""background-color: #808080; height: 80px;"">
                                    <h1 style=""color: white; padding-left: 3%; padding-top: 2%;"">Account Deactivation
                                        <img src=""{imagePath}"" alt="""" width=""50px"" height=""30%"" style=""float: right; margin-right: 2%;"">
                                    </h1>
                                </div>
                                <div style=""display: flex;"">
                                    <div class=""card"" style=""padding: 7%; padding-top: 5%; height: 500px; width: 500px;"">
                                        <div style=""box-shadow: 0 8px 8px 0 rgba(0,0,0,0.2); transition: 0.3s; background: white; padding: 20%; border: 1px solid;"">
                                            <p>{emailconfig.EmailHeader}</p>
                                            <p>{emailconfig.EmailBody}</p>
                                            <p>{emailconfig.EmailFooter}</p>
                                        </div>
                                    </div>
                                </div>
                            </body>
                            </html>", username);
                }

                //Account Rejetion Mail
                if (status == Convert.ToString(Enums.UserStatus.REJECTED))
                {
                    Body = string.Format($@"<!doctype html>
                            <html lang=""en"">
                            <head>
                            <meta charset=""utf-8"">
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                            <title>Bootstrap demo</title>
                        </head>
                        <body>
                            <div style=""background-color: #808080; height: 80px;"">
                                <h1 style=""color: white; padding-left: 3%; padding-top: 2%;"">Application Rejection
                                    <img src=""{imagePath}"" alt="""" width=""50px"" height=""30%"" style=""float: right; margin-right: 2%;"">
                                </h1>
                            </div>
                            <div style=""display: flex;"">
                                <div class=""card"" style=""padding: 7%; padding-top: 5%; height: 500px; width: 500px;"">
                                    <div style=""box-shadow: 0 8px 8px 0 rgba(0,0,0,0.2); transition: 0.3s; background: white; padding: 20%; border: 1px solid;"">
                                        <p>{emailconfig.EmailHeader}</p>
                                        <p>{emailconfig.EmailBody}</p>
                                        <p>{emailconfig.EmailFooter}</p>
                                    </div>
                                </div>
                            </div>
                        </body>
                        </html>", username);
                }
                string response = SendEmail(adminConfig,usermail, Subject, Body);
            }

            return "Successfull";

        }

        //email format taken and trigger the pertucular mail
        public string SendEmail(Admin_Configuration adminConfig,string to, string subject, string body)
        {
            try
            {
                if (adminConfig.Email_Settings != null)
                {
                    if (adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Email_Delivery_Method =="smtp")
                    {
                        MailMessage mm = new MailMessage(adminConfig.Email_Settings.Sent_Email_Address, to);
                        mm.Subject = subject;
                        mm.Body=body;
                        
                        mm.IsBodyHtml = true;
                        SmtpClient smtp = new SmtpClient();
                        smtp.Host = adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Smtp_Config.Smtp_Domain;
                        smtp.EnableSsl = true;
                       
                        NetworkCredential NetworkCred = new NetworkCredential();
                        NetworkCred.UserName = adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Smtp_Config.Smtp_User_Name;
                        NetworkCred.Password = adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Smtp_Config.Smtp_User_Pwd;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = NetworkCred;
                        smtp.Port = Int16.Parse(adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Smtp_Config.Smtp_Port);
                        smtp.Send(mm);

                        return "success";
                    }
                    if (adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Email_Delivery_Method == "pop3")
                    {
                        MailMessage mm = new MailMessage(adminConfig.Email_Settings.Sent_Email_Address, to);
                        mm.Subject = subject;
                        mm.Body = body;

                        mm.IsBodyHtml = true;
                        SmtpClient smtp = new SmtpClient();
                        smtp.Host = adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Pop3_Config.Pop3_Domain;
                        smtp.EnableSsl = true;
                       
                        NetworkCredential NetworkCred = new NetworkCredential();
                        NetworkCred.UserName = adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Pop3_Config.Pop3_User_Name;
                        NetworkCred.Password = adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Pop3_Config.Pop3_User_Pwd;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = NetworkCred;
                        smtp.Port = Int16.Parse(adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Pop3_Config.Pop3_Port);
                        smtp.Send(mm);
                        Console.WriteLine("success");
                        return "success";
                    }
                    if (adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Email_Delivery_Method == "imap")
                    {
                        MailMessage mm = new MailMessage(adminConfig.Email_Settings.Sent_Email_Address, to);
                        mm.Subject = subject;
                        mm.Body = body;
                        mm.IsBodyHtml = true;
                        SmtpClient smtp = new SmtpClient();
                        smtp.Host = adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Imap_Config.Imap_Domain;
                        smtp.EnableSsl = true;
                       
                        NetworkCredential NetworkCred = new NetworkCredential();
                        NetworkCred.UserName = adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Imap_Config.Imap_User_Name;
                        NetworkCred.Password = adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Imap_Config.Imap_User_Pwd;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = NetworkCred;
                        smtp.Port = Int16.Parse(adminConfig.Email_Settings.Email_Configuration.FirstOrDefault().Imap_Config.Imap_Port);
                        smtp.Send(mm);

                        return "success";
                    }
                }
                else
                {
                   // System.Net.ServicePointManager.Expect100Continue = false;
                    MailMessage mm = new MailMessage("support@pursuit.ortusolis.in", to);
                    mm.Subject = subject;
                    mm.Body = body;

                    mm.IsBodyHtml = true;
                    SmtpClient smtp = new SmtpClient();
                    smtp.Host = "smtp.hostinger.com";
                    smtp.EnableSsl = true;
                    
                    NetworkCredential NetworkCred = new NetworkCredential();
                    NetworkCred.UserName = "support@pursuit.ortusolis.in";
                    NetworkCred.Password = "Pursuit@o25#";
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = NetworkCred;
                    smtp.Port = 587;
                    smtp.Send(mm);
                    return "success";
                }
            }
            catch (Exception ex)
            {
                /* MailMessage mm = new MailMessage("support@pursuit.ortusolis.in", to);
                 mm.Subject = subject;
                 mm.Body = body;
                 mm.IsBodyHtml = true;
                 SmtpClient smtp = new SmtpClient();
                 smtp.Host = "smtp.hostinger.com";
                 smtp.EnableSsl = true;
                 smtp.Timeout = 5000;
                 NetworkCredential NetworkCred = new NetworkCredential();
                 NetworkCred.UserName = "support@pursuit.ortusolis.in";
                 NetworkCred.Password = "Pursuit@o25#";
                 smtp.UseDefaultCredentials = false;
                 smtp.Credentials = NetworkCred;
                 smtp.Port = 587;
                 smtp.Send(mm);
                 return "success";*/
                return ex.Message;
            }
           

            return "success";
        }
        //Update user by taking id
        public Task UpsertOne(TDocument document)
        {
            return Task.Run(() =>
            _collection.ReplaceOneAsync(
                filter: new BsonDocument("Id", document.Id),
                options: new ReplaceOptions { IsUpsert = true },
                replacement: document));
        }
        //update bulk users by taking their id's
        public Task UpsertBulk(List<TDocument> documents)
        {
            return Task.Run(() =>
            {
                var bulkOps = new List<WriteModel<TDocument>>();
                foreach (var rec in documents)
                {
                    var upsertRec = new ReplaceOneModel<TDocument>(
                        Builders<TDocument>.Filter.Where(x => x.Id == rec.Id),
                        rec)
                    { IsUpsert = true };

                    bulkOps.Add(upsertRec);
                }
                _collection.BulkWriteAsync(bulkOps);
            });
        }
        //Generating Random password
        public string GeneratePassword()
        {
            string PasswordLength = "6";

            string allowedChars = "";

            allowedChars = "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,";

            allowedChars += "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z,";

            allowedChars += "1,2,3,4,5,6,7,8,9,0";

            char[] sep = { ',' };

            string[] arr = allowedChars.Split(sep);

            string passwordString = "";

            string temp = "";

            Random rand = new Random();

            for (int i = 0; i < Convert.ToInt32(PasswordLength); i++)

            {

                temp = arr[rand.Next(0, arr.Length)];

                passwordString += temp;

            }

            return passwordString;
        }

        public void UpdateNotification(string userId, ICollection<Notification> notifications)
        {
            throw new NotImplementedException();
        }
       
        public string SendShedulerEmail(Admin_Configuration adminConfig, string Message, string Email)
        {
            
            string subject = "Scheduler And Archival";
            string body = Message;
            string response = SendEmail(adminConfig, Email,subject, body);
            return response;
        }


        public virtual async Task addEmailTemplate()
        {
            var email1 = new EmailContentConfiguration
            {
                EmailType = "Approval",
                EmailSubject = "Your Account Is Activated",
                EmailBody = "Your account is activated, you can now login with your credentials.",
                EmailHeader = "Hi {0},",
                EmailFooter = "Please use this link to login {1}<br /><br />Thanks and Regards<br />Evolve Access Support Team"
            };

            var email2 = new EmailContentConfiguration
            {
                EmailType = "Rejection",
                EmailSubject = "Your registration application is rejected",
                EmailBody = "Your registration application is rejected, please contact support.team@evolveaccess.com for more information.",
                EmailHeader = "Hi {0},",
                EmailFooter = "Thanks and Regards<br />Evolve Access Support Team"
            };

            var email3 = new EmailContentConfiguration
            {
                EmailType = "Verification",
                EmailSubject = "Admin Activated Your Account",
                EmailBody = "Admin activated your account, please verify your account with below credentials.<br /><br />Email ID: {1}<br /><br />Password: {2}<br /><br />",
                EmailHeader = "Hi {0},",
                EmailFooter = "Please use this link to login {3} <br /><br />Thanks and Regards<br />Evolve Access Support Team"
            };
            var email4= new EmailContentConfiguration
            {
                EmailType = "Deletion",
                EmailSubject = "Your Evolve Access account is Inactivated",
                EmailBody = "Your Evolve Access account is De-activated, please contact support.team@evolveaccess.com for more information.",
                EmailHeader = "Hi {0},",
                EmailFooter = "Thanks and Regards<br />Evolve Access Support Team"
            };
            var emailList = new List<EmailContentConfiguration>
                            {
                                email1, email2, email3, email4
                            };


            var distinctEmails = emailList.GroupBy(e => e.EmailType).Select(g => g.First()).ToArray();

            await _MailTemplate.InsertManyAsync(distinctEmails);

            //return _MailTemplate.InsertManyAsync(distinctEmails);
          //  return _MailTemplate.InsertManyAsync(new[] { email1, email2, email3, email4 });
        }
    }
}
