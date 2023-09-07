using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
/* =========================================================
    Item Name: Email_Configuration model-used in configuration
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{
    public class Email_Configuration
    {
        public Email_Configuration()
        {
            Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }
        public string Email_Delivery_Method { get; set; } = null!;
        public virtual Smtp_Config? Smtp_Config { get; set; } = null!;
        public virtual Imap_Config? Imap_Config { get; set; } = null!;
        public virtual Pop3_Config? Pop3_Config { get; set; } = null!;

    }
}
