using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Pursuit.Context;
using System.Dynamic;
using System.Security.Cryptography.Xml;

namespace Pursuit.Model
{

    [BsonCollection("log")]
    public class Log : IDocument
    {
        public Log()
        {
            //  Id = ObjectId.GenerateNewId();
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }

        [BsonElement("Level")]
        public string? Level { get; set; }

        [BsonElement("UtcTimeStamp")]
        public DateTime? UtcTimeStamp { get; set; }
       
        [BsonElement("RenderedMessage")]
        public string? RenderedMessage { get; set; }

        [BsonElement("MessageTemplate")]
        public MessageTemplate? MessageTemplate { get; set; }

        [BsonElement("Properties")]
        public Properties? Properties { get; set; }

        [BsonElement("Exception")]
        public Exceptions? Exception { get; set; }

        [BsonElement("DateCreated")]
        public DateTime DateCreated => DateTime.Now;
    }
}
