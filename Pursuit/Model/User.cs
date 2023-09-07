using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Pursuit.Context;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json.Serialization;
/* =========================================================
    Item Name: User model
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{
    [BsonIgnoreExtraElements]
    [BsonCollection("AppUsers")]
    public class User : IDocument
    {
        public User()
        {
            Connection_Settings = new HashSet<Connection_Setting>();
           // Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid UserGuId { get; set; }

        [BsonElement("Phone")]
        
        public string? Phone { get; set; } = null!;

        public string? SubjectId { get; set; }

        [BsonElement("Username")]
        public string? Username { get; set; } = null!;

        [BsonElement("Firstname")]
        public string? FirstName { get; set; } = null!;

        [BsonElement("Lastname")]
        public string? LastName { get; set; } = null!;

              
        [BsonElement("Password")]
        public string? Password { get; set; } = null!;

        [JsonIgnore]
       
        [BsonElement("Salt")]
        public byte[] Salt { get; set; } = new byte[0];
        
        [BsonElement("Email")]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string? Email { get; set; } = null!;

        [BsonElement("ProfileImage")]
        public string? ProfileImage { get; set; }

        [BsonElement("user_origin_code")]
        public string? user_origin_code { get; set; } 

        [BsonElement("Userstatus")]
        public string? Userstatus { get; set; }

        [BsonElement("RoleName")]
        public string? RoleName { get; set; } = null!;

        [BsonElement("Organization")]
        public virtual Organization? Organization { get; set; } = null!;

        [BsonElement("Connection_Settings")]
        public virtual ICollection<Connection_Setting> Connection_Settings { get; set; } = null!;
        public Boolean? Notification_Setting { get; set; }
        public string? Notification_Expires_days { get; set; } = null!;

        [BsonElement("Notification_Preference")]
        public virtual Notification_Preference? Notification_Preference { get; set; } = null!;

        [BsonElement("Notifications")]
        public virtual ICollection<Notification>? Notifications { get; set; } = null!;
        

        [BsonElement("Role")]
        public virtual Role? Role { get; set; } = null!;

        [BsonElement("LoginOTP")]
        public string? LoginOTP { get; set; } = null!;

        [BsonElement("DateCreated")]
        public DateTime DateCreated => DateTime.Now;

       [BsonElement("PasswordSetDateTime")] 
        public DateTime? PasswordSetDateTime { get; set; } = null!;
       
    }
}
