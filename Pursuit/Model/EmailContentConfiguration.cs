using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Pursuit.Context;

namespace Pursuit.Model
{
    [BsonCollection("MailTemplates")]
    public class EmailContentConfiguration : IDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }
        public string? EmailType { get; set; }
        public string? EmailSubject { get; set; }
        public string? EmailBody { get; set; }
        public string? EmailHeader { get; set; }
        public string? EmailFooter { get; set; }
        public DateTime DateCreated => DateTime.Now;
    }
}
