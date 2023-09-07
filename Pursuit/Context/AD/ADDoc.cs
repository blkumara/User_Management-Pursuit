using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Dynamic;

/* =========================================================
    Item Name: Document format for DB - IAdDoc,AdDoc
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Context
{
    public interface IAdDoc
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        ObjectId Id { get; set; }
        string Email { get; set; }
        string AzureId { get; set; }

        ExpandoObject UserDocument { get; set; }

    }

    [BsonIgnoreExtraElements]
    public abstract class AdDoc : IAdDoc
    {
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }

        [BsonIgnoreIfDefault]
        public string Email { get; set; }

        [BsonIgnoreIfDefault]
        public string AzureId { get; set; }

        [BsonIgnoreIfDefault]
        public ExpandoObject UserDocument { get; set; }

    }

}
