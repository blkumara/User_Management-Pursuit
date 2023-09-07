using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
/* =========================================================
    Item Name: Pwd_Allowed_Characters model-used in configuration
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{
    public class Pwd_Allowed_Characters
    {
        public Pwd_Allowed_Characters()
        {
            Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }
        public Boolean Uppercase { get; set; }
        public Boolean Lowercase { get; set; }
        public Boolean SpecialCharacters { get; set; }
        public Boolean Numeric { get; set; }

    }
}
