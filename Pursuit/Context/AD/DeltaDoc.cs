using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using System.Text.Json.Serialization;
using System.Dynamic;

namespace Pursuit.Context.AD
{
    public interface IDeltaDoc
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
      
         ObjectId Id { get; set; }


        ICollection<ExpandoObject>? Value { get; set; }

         DateTime? PrevUpdateDate { get; set; }
         Boolean? IsNextLinkCalled { get; set; }
         string? NextLink { get; set; }
         string? DeltaLink { get; set; }

         Boolean? IsRecordUsed { get; set; }

    }
    [BsonIgnoreExtraElements]
    public class DeltaDoc: IDeltaDoc
    {
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
        public string? NextLink { get; set; }
        public string? DeltaLink { get; set; }
    
    public Boolean? IsRecordUsed { get; set; }


}
}
