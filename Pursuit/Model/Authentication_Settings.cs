using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
/* =========================================================
    Item Name: Authentication_Settings model-used in configuration
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{
    public class Authentication_Settings
    {
        public Authentication_Settings()
        {
            Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }
        public Boolean? Auth_Required { get; set; }
        public Boolean? Self_Registration { get; set; }
        public Boolean? Use_Email_Login { get; set; }
        public string? Activation_Mail_Expires_After { get; set; }
       
        public virtual Password_Settings? Password_Settings { get; set; }
       
       
        public Boolean? Session_Expires { get; set; }
        public string? Session_Expiration_Days { get; set; }

    }
}
