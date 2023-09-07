using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Pursuit.Context;
using System.ComponentModel.DataAnnotations;
/* =========================================================
    Item Name: ADRecord model
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{

    [BsonIgnoreExtraElements]
    [BsonCollection("GWS_AD")]
    public class GUser : IAdDoc
    {

        public GUser()
        {
           // Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }


        [BsonElement("Email")]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string Email { get; set; } = null!;
        /*

                [BsonElement("Firstname")]
                public string? FirstName { get; set; } = null!;

                [BsonElement("Lastname")]
                public string? LastName { get; set; } = null!;

                [BsonElement("Phone")]
                [Phone(ErrorMessage = "Invalid Phone Number")]
                public string Phone { get; set; } = null!; */

        [BsonElement("UserDocument")]
        public BsonDocument UserDocument { get; set; } = null!;

    }
}
