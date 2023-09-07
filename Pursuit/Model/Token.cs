using MongoDB.Bson.Serialization.Attributes;

namespace Pursuit.Model
{
    public class Token
    {
        [BsonElement("_t")]
        public string? Type { get; set; }

        [BsonElement("StartIndex")]
        public int? StartIndex { get; set; }

        [BsonElement("Text")]
        public string? Text { get; set; }
    }
}
