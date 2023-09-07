using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Pursuit.Context;
using System.Text.Json.Serialization;
/* =========================================================
    Item Name: Admin_Configuration model
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{
   
    [BsonCollection("AdminConfig")]
    public class Admin_Configuration : IDocument
    {
        public Admin_Configuration()
        {
            Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }
        public Boolean Notification_Setting { get; set; }
        public string? Notification_Expires_days { get; set; } = null!;
        public virtual Notification_Preference? Notification_Preference { get; set; }
        public virtual Email_Settings? Email_Settings { get; set; }
        public virtual Authentication_Settings? Authentication_Settings { get; set; } 
        public virtual ICollection<Oauth_Setting>? Oauth_Settings { get; set; }
        public virtual Two_FA_Settings? Two_FA_Settings { get; set; }

        [BsonElement("SyncupCred")]
        public virtual SyncupCred? SyncupCred { get; set; }

        public virtual ArchivalInfo? ArchivalInfo { get; set; }

        public string? DomainName { get; set; } = null!;
        public DateTime DateCreated => DateTime.Now;
    }
}