using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;
/* =========================================================
    Item Name: Imap_Config model-used in configuration
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{
    public class Imap_Config
    {
        public Imap_Config()
        {
            Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }
        public string Imap_Server { get; set; } = null!;
        public string Imap_Port { get; set; } = null!;
        public string Imap_Domain { get; set; } = null!;
        public string Imap_Authentication { get; set; } = null!;
        public string Imap_User_Name { get; set; } = null!;
        public string Imap_User_Pwd { get; set; } = null!;

        [JsonIgnore]

        [BsonElement("Salt")]
        public byte[] Salt { get; set; } = new byte[0];
        public Boolean Imap_Encrypt_Method { get; set; }
        public Boolean Use_Ssl_Connection { get; set; }


    }
}
