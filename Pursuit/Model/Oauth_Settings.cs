using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Newtonsoft.Json;
/* =========================================================
Item Name: Oauth_Settings model-used in configuration
Author: Ortusolis for EvolveAccess Team
Version: 1.0
Copyright 2022 - 2023 - Evolve Access
============================================================ */
namespace Pursuit.Model
{
    public class Oauth_Setting
    {
        public Oauth_Setting()
        {
            Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }

        public string? Connection_Type { get; set; }
        public string? Connection_Name { get; set; }

        public string? Connection_User_Id { get; set; }

        public string? Connection_User_Name { get; set; }

        public string? Connection_User_Pwd { get; set; }

        [JsonIgnore]

        [BsonElement("Salt")]
        public byte[] SaltPwd { get; set; } = new byte[0];

        public string? Domain_Name { get; set; }
        public string? Client_Secret { get; set; }
        public string? private_key { get; set; }
        public string? client_email { get; set; }
        public string? type { get; set; }
        public string? project_id { get; set; }
        public string? ApplicationId { get; set; }
        public string? TenantId { get; set; }
        public string? ApiKey { get; set; }
        public string? AuthDomain { get; set; }
        public string? ProjectId { get; set; }
        public string? StorageBucket { get; set; }
        public string? MessagingSenderId { get; set; }
        public string? AppId { get; set; }
        public string? MeasurementId { get; set; }
        public Boolean? IsActive { get; set; } = true;
        public string? Client_Id { get; set; }

        public string? HostName { get; set; }
    }
}
