using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
/* =========================================================
    Item Name: Two_FA_Settings model-used in configuration
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{
    public class Two_FA_Settings
    {
        public Two_FA_Settings()
        {
            Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }
        public string? Two_FA_Provider { get; set; }
        public Boolean? Two_FA_Enforce { get; set; }
        public Boolean? Two_FA_Remember_Login_Days { get; set; }

    }
}
