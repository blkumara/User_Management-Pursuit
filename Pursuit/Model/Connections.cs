using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Pursuit.Model
{
    public class Connection
    {
        public Connection()
        {
          //  Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }
        public string? UserId { get; set; }
        public string? UserRole { get; set; }
        public string? Connection_Type{ get; set; }
        public string? Connection_Name { get; set; }
        public string? Connection_Url { get; set; }
        public string? Connection_User_Id { get; set; }

        public string? Connection_User_Name { get; set; }

        public string? Connection_User_Pwd { get; set; }

        [JsonIgnore]

        [BsonElement("Salt")]
        public byte[] SaltPwd { get; set; } = new byte[0];

        public string? Connection_User_Group { get; set; }
        public string? Domain_Name { get; set; }

        public string? HostName { get; set; }
        public string? Dbn_Name { get; set; }
        public string? Client_Id { get; set; }

        [JsonIgnore]

        [BsonElement("SaltClientId")]
        public byte[] SaltClientId { get; set; } = new byte[0];

        public string? Client_Secret { get; set; }
        public string? private_key { get; set; }
        public string? client_email { get; set; }

        public string? Auth_Token { get; set; }

        public Boolean? Verified { get; set; }

        public string? type { get; set; }
        public string? project_id { get; set; }
        public string? ApplicationId { get; set; }
        public string? TenantId { get; set; }

        public string? TenantName { get; set; }

        public string? Date { get; set; }

        [BsonElement("DateCreated")]
        public DateTime DateCreated { get; set; }
        
    }
}
