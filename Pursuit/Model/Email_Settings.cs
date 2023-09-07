using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
/* =========================================================
    Item Name: Email_Settings model-used in configuration
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{
    public class Email_Settings
    {
        public Email_Settings()
        {
            Email_Configuration = new HashSet<Email_Configuration>();
            Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }
        public string Sent_Email_Address { get; set; } = null!;
        public string Email_Bcc { get; set; } = null!;
       
        public virtual ICollection<Email_Configuration> Email_Configuration { get; set; }

    }
}
