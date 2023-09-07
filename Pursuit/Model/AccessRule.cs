using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Pursuit.Context;
/* =========================================================
    Item Name: AccessRule model
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{
    [BsonIgnoreExtraElements]
    [BsonCollection("RoleAccesses")]
    public class AccessRule : IDocument
    {
        public AccessRule()
        {
            Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }

        [BsonElement("Feature")]
        public string Feature { get; set; } = null!;

        [BsonElement("Access")] 
        public bool Access { get; set; } = true!;

        [BsonElement("DateCreated")]
        public DateTime DateCreated => DateTime.Now;
    }
}
