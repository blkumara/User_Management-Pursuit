using MongoDB.Bson;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Pursuit.Model;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Log = Pursuit.Model.Log;
/* =========================================================
Item Name: Repository Interface-IPursuitRepository
Author: Ortusolis for EvolveAccess Team
Version: 1.0
Copyright 2022 - 2023 - Evolve Access
============================================================ */
namespace Pursuit.Context
{
    public interface IPursuitRepository<TDocument>
        where TDocument : IDocument  //where TDocument : IDocument.SubType(ISubDocument)
    {
        //All methods implemented in Pursuit.Context.PursuitRepository
        IQueryable<TDocument> AsQueryable();

        IEnumerable<TDocument> FilterBy(
            Expression<Func<TDocument, bool>> filterExpression);

        IEnumerable<Connection_Setting> FindConnection(
            string userId);

        IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression,
            Expression<Func<TDocument, TProjected>> projectionExpression);

        TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression);

        Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression);
        Task RemoveManyAsync(Expression<Func<TDocument, bool>> filterExpression);

        //Find the user data by using ID
        TDocument FindById(string id);

        Task<TDocument> FindByIdAsync(string id);
        //Get Logs
        List<BsonDocument> FindlogAsync();

        //Get Logs by level
        List<BsonDocument> FindloglevelAsync(string level);
        //Delete Logs
        String DeleteLogAsync();

        //Insert user whole details
        void InsertOne(TDocument document);

        Task InsertOneAsync(TDocument document);

        void InsertMany(ICollection<TDocument> documents);

        Task InsertManyAsync(ICollection<TDocument> documents);

        Task UpsertOne(TDocument document);

        Task UpsertBulk(List<TDocument> documents);

        void ReplaceOne(string userId, User user);

        Task ReplaceOneAsync(string userId, User user);

        void AddUserConfig(string userId, User user);
        Task ReplaceEmailConfigAsync(string Id, EmailContentConfiguration emailconfig);
        Task AddUserConfigAsync(string userId, User user);
        Task AddConnSettingsAsync(string userId, ICollection<Connection_Setting> con_set);
        Task AddConnSettingItemAsync(string userId, Connection_Setting con_set);
        Task AddOrUpdateConnectionAsync(string userId, string ctype, Connection con);

        Task UpdateConnectionAsync(string ctype, Connection con);

        Task DeleteConnectionAsync(string UserId, string ConnId);
        Task DeleteConnAsync(string ctype, string connectionId);

        void UpdateAdminConfig(string Id, Admin_Configuration admin);

        Task UpdateAdminConfigAsync(string Id,  Admin_Configuration admin);

        Task UpdateOauthConfigAsync(string adminId, Oauth_Setting oauth);

        void UpdateStatus(string userId, string userStatus);

        Task UpdateStatusAsync(string userId, string userStatus);


        Task UpdateNotificationStatusAsync(NotifyStatusRequest req);


        void UpdateNotification(string userId, ICollection<Notification> notifications);

        Task UpdateNotificationAsync(string userId, ICollection<Notification> notifications);

        void UpdatePassword(string userId, string password, byte[] salt, DateTime PasswordSetDateTime);

        Task UpdatePasswordAsync(string userId, string password, byte[] salt,DateTime PasswordSetDateTime);

        string ForgotPassword(User user, Admin_Configuration adminConfig);
       
        string emailVerification(string email, Admin_Configuration adminConfig);

        string loginVerification(string email, Admin_Configuration adminConfig);

        string SendEmailUser(User user, string GeneratedPass, Admin_Configuration adminConfig);

        string SendEmailToAdmin(User user, User user2, Admin_Configuration adminConfig);

        string SendStatusEmailToUser(Admin_Configuration adminConfig, User user, string status,String Password, EmailContentConfiguration emailconfig);

        Task<string> SendEmailToAdminAsync(User user, User user2, Admin_Configuration adminConfig);

        string GeneratePassword();
        string SendShedulerEmail(Admin_Configuration adminConfig, String Message,String Email);

        string GenerateOTP();

        Task addEmailTemplate();
    }
}
