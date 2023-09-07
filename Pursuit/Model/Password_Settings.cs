using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
/* =========================================================
    Item Name: Password_Settings model-used in configuration
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{
    public class Password_Settings
    {
        public Password_Settings()
        {
            Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }
        public string? Minimum_Length { get; set; }
        public virtual Pwd_Allowed_Characters? Pwd_Allowed_Characters { get; set; }
        
        public string? Pwd_Change_Enforce_Duration { get; set; }
        
    }
}
