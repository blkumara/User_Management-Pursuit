using MongoDB.Bson.Serialization.Attributes;

namespace Pursuit.Model
{
    public class MessageTemplate
    {
        public string? Text { get; set; }

        [BsonElement("Tokens")]
        public Token[]? Tokens { get; set; }
    }
}
