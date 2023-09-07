using MongoDB.Bson.Serialization.Attributes;

namespace Pursuit.Model
{
    public class Eventid
    {
        [BsonElement("Id")]
        public Int32? Id { get; set; }

       

        [BsonElement("Name")]
        public string? Name { get; set; }
    }
}
