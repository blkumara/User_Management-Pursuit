using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Pursuit.Context;
/* =========================================================
    Item Name: Role model
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Model
{
    [BsonIgnoreExtraElements]
    [BsonCollection("Roles")]
    public class Role : IDocument
    {
        public Role()
        {
            //Id = ObjectId.GenerateNewId();
            AccessRules = new HashSet<AccessRule>();
            Users = new HashSet<User>();
            RoleName = "";
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }

        [BsonElement("RoleName")]
        public string? RoleName { get; set; } = null!;

        public ICollection<AccessRule> AccessRules { get; set; } = null!;

        public ICollection<User> Users { get; set; } = null!;

        [BsonElement("DateCreated")]
        public DateTime DateCreated => DateTime.Now;
    }

}
