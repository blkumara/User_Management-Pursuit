using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Dynamic;

using System.Text.Json.Serialization;
using Microsoft.Kiota.Abstractions;
using Pursuit.Context.AD;

namespace Pursuit.Model
{

    [BsonIgnoreExtraElements]
    public class DeltaModel:IDeltaDoc
    {
        public DeltaModel() { }
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }

        [JsonIgnore]
        [BsonElement("Value")]
        [BsonIgnoreIfDefault]
        public ICollection<ExpandoObject>? Value { get; set; } = null!;

        public DateTime? PrevUpdateDate { get; set; } 
        public Boolean? IsNextLinkCalled { get; set; }
        public Boolean? IsRecordUsed { get; set; }
        public string? NextLink { get; set; }
        public string? DeltaLink { get; set; }

        public string? ConnectionId { get; set; }

    }
}