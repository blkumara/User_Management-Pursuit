using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pursuit.Context
{
    public interface ISubDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        ObjectId Id { get; set; }

        DateTime DateCreated { get; }
    }

    public abstract class SubDocument : ISubDocument
    {
        public ObjectId Id { get; set; }

        public DateTime DateCreated => DateTime.Now;
    }
}
