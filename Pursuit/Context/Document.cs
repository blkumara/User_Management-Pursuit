using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
/* =========================================================
    Item Name: Document format for DB - IDocument,Document
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Context
{
    public interface IDocument
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        ObjectId Id { get; set; }
        
        DateTime DateCreated { get; }
    }

    public abstract class Document : IDocument
    {
        public ObjectId Id { get; set; }

        public DateTime DateCreated => DateTime.Now;

     }
}
