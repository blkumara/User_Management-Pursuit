using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Pursuit.Context;

namespace Pursuit.Model
{
    public class Notification
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }

        public string? NotifiedBy { get; set; }

        public string? NotificationText { get; set; }
        public string? NotificationStatus { get; set; }
        public string? Date { get; set; }

        [BsonElement("DateCreated")]
        public DateTime? DateCreated { get; set; } = null!;
    }
}
